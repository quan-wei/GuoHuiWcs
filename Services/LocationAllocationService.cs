using Guohui_Wcs.Models;
using Models;
using SqlSugar;
using System.Linq.Expressions;

namespace Guohui_Wcs.Services;

public class AllocationRequest
{
    public List<string>? MaterNo { get; set; }
    public string? StartPoint { get; set; }
    public string? EndPoint { get; set; }
    public string? TaskType { get; set; }

    public bool AllowUpperLevels { get; set; } = true;
}

public class LockRequest
{
    public string? Reason { get; set; }
}

public class AllocationResult
{
    public bool Success { get; set; }
    public string? LocationCode { get; set; }
    public string? Message { get; set; }
    public string? LocationType { get; set; }
    public decimal? WeightKg { get; set; }
    public string? PallNo { get; set; }
}

public class GroupLoadInfo
{
    public List<string> GroupShelfs { get; set; } = new();
    public decimal CurrentWeightKg { get; set; }
    public decimal LimitWeightKg { get; set; } = 2500m;
    public decimal RemainingWeightKg => Math.Max(0, LimitWeightKg - CurrentWeightKg);
    public Dictionary<string, decimal> TierLoads { get; set; } = new();
}

public static class AllocationRules
{
    public const decimal UpperLevelPairWeightLimit = 2500m;
}

public class LocationAllocationService
{
    private readonly ILogger<LocationAllocationService> _logger;
    private readonly SqlSugarScope _db;
    private readonly ApiClientService _apiClient;

    private static readonly string[] UpperTiers = { "二层货架", "三层货架", "四层货架" };

    public LocationAllocationService(
        ILogger<LocationAllocationService> logger,
        SqlSugarScope db,
        ApiClientService apiClient)
    {
        _logger = logger;
        _db = db;
        _apiClient = apiClient;
    }

    public async Task<AllocationResult> Allocate(AllocationRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.StartPoint))
            return Fail("起点不能为空");

        var pallNo = GeneratePallNo();

        if (request.MaterNo == null || request.MaterNo.Count == 0)
            return Fail("物料号不能为空");

        decimal totalWeight = 0;
        var syncedBarcodes = new List<Barcode>();

        foreach (var code in request.MaterNo)
        {
            var result = await _apiClient.SyncBardossierToDbAsync(code);
            if (result != null)
            {
                syncedBarcodes.Add(result);
                totalWeight += result.AuxQty ?? 0;
            }
        }

        // 插入 PallMater 记录
        var pallMater = new PallMater
        {
            PallNo = pallNo,
            Weight = totalWeight,
            CreateTime = DateTime.Now
        };

        for (int i = 0; i < syncedBarcodes.Count && i < 15; i++)
        {
            var bc = syncedBarcodes[i];
            var index = i + 1;

            var subTitleProp = typeof(PallMater).GetProperty($"SubTitle{index}");
            var weighProp = typeof(PallMater).GetProperty($"Weigh{index}");

            if (subTitleProp != null && weighProp != null)
            {
                subTitleProp.SetValue(pallMater, bc.Number);
                weighProp.SetValue(pallMater, bc.Qty);
            }
        }



        if (totalWeight <= 0)
        {
            return Fail("重量异常");
        }

        AllocationResult? allocationResult = null;

        var level1 = FindFreeLocation(l => l.LocationType == "一层货架", null);
        if (level1 != null)
            allocationResult = TryAllocate(level1, pallNo, totalWeight);

        if (allocationResult == null && request.AllowUpperLevels)
        {
            foreach (var tier in UpperTiers)
            {
                var loc = FindUpperLevel(tier, null, totalWeight);
                if (loc != null)
                {
                    allocationResult = TryAllocate(loc, pallNo, totalWeight);
                    break;
                }
            }
        }

        if (allocationResult == null)
            return Fail("无可用库位：一层已满，且上层库位均不满足重量限制");

        if (allocationResult.Success)
        {
            _db.Insertable(pallMater).ExecuteCommand();
            _logger.LogInformation("PallMater created: {PallNo}, weight: {Weight}", pallNo, totalWeight);
        }

        return allocationResult;
    }

   public async Task<AllocationResult> AllocateToSpecific(string locationCode, AllocationRequest request)
   {
        var pallNo = GeneratePallNo();

        var loc = _db.Queryable<Location>()
            .First(l => l.Reserve5 == locationCode);

        if (loc == null)
            return Fail("终点库位不存在");
        if (loc.Status != 0 || !string.IsNullOrEmpty(loc.PallNo))
            return Fail("终点库位已被占用");
        if (loc.EnableFlag == false)
            return Fail("终点库位已禁用");

        // G 开头：出库操作，不校验物料号，重量为 0
        bool isGLocation = locationCode.StartsWith("G", StringComparison.OrdinalIgnoreCase);

        if (!isGLocation)
        {
            if (request.MaterNo == null || request.MaterNo.Count == 0)
                return Fail("物料号不能为空");

            decimal totalWeight = 0;
            var syncedBarcodes = new List<Barcode>();

            foreach (var code in request.MaterNo)
            {
                var wmsResult = await _apiClient.SyncBardossierToDbAsync(code);
                if (wmsResult != null)
                {
                    syncedBarcodes.Add(wmsResult);
                    totalWeight += wmsResult.Qty;
                }
            }

            if (totalWeight == 0)
                return Fail("WMS 未返回任何物料重量，请检查物料码");

            var pallMater = new PallMater
            {
                PallNo = pallNo,
                Weight = totalWeight,
                LocationCode = loc.LocationCode,
                ShelfCode = request.StartPoint,
                CreateTime = DateTime.Now
            };

            for (int i = 0; i < syncedBarcodes.Count && i < 15; i++)
            {
                var bc = syncedBarcodes[i];
                var index = i + 1;

                var subTitleProp = typeof(PallMater).GetProperty($"SubTitle{index}");
                var weighProp = typeof(PallMater).GetProperty($"Weigh{index}");

                if (subTitleProp != null && weighProp != null)
                {
                    subTitleProp.SetValue(pallMater, bc.Number);
                    weighProp.SetValue(pallMater, bc.Qty);
                }
            }

            if (!CanPlace(loc, totalWeight))
                return Fail("所在货架对已超重限制");

            // 与 Allocate 保持一致：库位分配成功后才落库 PallMater，避免失败路径留下孤儿记录
            var allocationResult = TryAllocate(loc, pallNo, totalWeight);
            if (!allocationResult.Success)
                return allocationResult;

            _db.Insertable(pallMater).ExecuteCommand();
            _logger.LogInformation("PallMater created: {PallNo}, weight: {Weight}, from: {Start}, to: {End}",
                pallNo, totalWeight, request.StartPoint, locationCode);

            return allocationResult;
        }
        else
        {
            decimal totalWeight = 0;
            _logger.LogInformation("Outlocate {PallNo}, weight: {Weight}, from: {Start}, to: {End}",
                pallNo, totalWeight, request.StartPoint, locationCode);
            return TryAllocate(loc, pallNo, totalWeight);
        }
    }
    public AllocationResult Release(string locationCode)
    {
        var rows = _db.Updateable<Location>()
            .SetColumns(l => new Location
            {
                Status = 0,
                PallNo = null,
                TotalWeight = null,
                UpdateTime = DateTime.Now
            })
            .Where(l => l.Reserve5 == locationCode && l.Status != 0)
            .ExecuteCommand();

        return rows > 0
            ? new AllocationResult { Success = true, Message = "库位已释放" }
            : Fail("库位释放失败，可能已是空闲状态");
    }

    public void RollbackAllocation(string locationCode, string pallNo)
    {
        _db.Updateable<Location>()
            .SetColumns(l => new Location
            {
                Status = 0,
                PallNo = null,
                TotalWeight = null,
                UpdateTime = DateTime.Now
            })
            .Where(l => l.LocationCode == locationCode && l.Status == 1)
            .ExecuteCommand();

        _db.Deleteable<PallMater>()
            .Where(p => p.PallNo == pallNo)
            .ExecuteCommand();

        _logger.LogWarning("AGV任务失败，已回滚库位 {Location} 和托盘 {PallNo}", locationCode, pallNo);
    }

    public GroupLoadInfo GetGroupLoad(string shelfCode)
    {
        var group = GetGroupShelfs(shelfCode);
        var tierLoads = new Dictionary<string, decimal>();
        decimal total = 0;

        foreach (var tier in UpperTiers)
        {
            var w = GetGroupCurrentWeight(group, tier);
            tierLoads[tier] = w;
            total += w;
        }

        return new GroupLoadInfo
        {
            GroupShelfs = group,
            CurrentWeightKg = total,
            TierLoads = tierLoads
        };
    }

    private Location? FindFreeLocation(Expression<Func<Location, bool>> typePredicate, string? targetZone)
    {
        var query = _db.Queryable<Location>()
            .Where(l => l.Status == 0)
            .Where(l => SqlFunc.IsNullOrEmpty(l.PallNo))
            .Where(l => l.EnableFlag == true)
            .Where(typePredicate);

        if (!string.IsNullOrWhiteSpace(targetZone))
            query = query.Where(l => l.LocationCode.StartsWith(targetZone));

        return query.OrderBy(l => l.LocationCode).First();
    }

    private Location? FindUpperLevel(string tier, string? targetZone, decimal? weightKg)
    {
        var query = _db.Queryable<Location>()
            .Where(l => l.Status == 0)
            .Where(l => SqlFunc.IsNullOrEmpty(l.PallNo))
            .Where(l => l.EnableFlag == true)
            .Where(l => l.LocationType == tier);

        if (!string.IsNullOrWhiteSpace(targetZone))
            query = query.Where(l => l.LocationCode.StartsWith(targetZone));

        var candidates = query.OrderBy(l => l.LocationCode).ToList();

        foreach (var loc in candidates)
        {
            if (CanPlaceOnTier(loc, tier, weightKg))
                return loc;
        }
        return null;
    }

    private bool CanPlace(Location location, decimal weightKg)
    {
        if (location.LocationType == "地面库位" || location.LocationType == "一层货架")
            return true;

        return CanPlaceOnTier(location, location.LocationType, weightKg);
    }

    private bool CanPlaceOnTier(Location location, string tier, decimal? weightKg)
    {
        var group = GetGroupShelfs(location.ShelfCode);
        var currentWeight = GetGroupCurrentWeight(group, tier);
        return (currentWeight + weightKg) <= AllocationRules.UpperLevelPairWeightLimit;
    }

    private decimal GetGroupCurrentWeight(List<string> shelfCodes, string tier)
    {
        var total = _db.Queryable<Location>()
            .Where(l => shelfCodes.Contains(l.ShelfCode))
            .Where(l => l.LocationType == tier)
            .Where(l => l.Status == 1)
            .Where(l => !SqlFunc.IsNullOrEmpty(l.PallNo))
            .Sum(l => l.TotalWeight ?? 0m);

        return total;
    }

    private AllocationResult TryAllocate(Location location, string pallNo, decimal? weightKg)
    {
        var rows = _db.Updateable<Location>()
            .SetColumns(l => new Location
            {
                Status = 1,
                PallNo = pallNo,
                TotalWeight = weightKg,
                UpdateTime = DateTime.Now
            })
            .Where(l => l.Id == location.Id && l.Status == 0 && SqlFunc.IsNullOrEmpty(l.PallNo))
            .ExecuteCommand();

        if (rows == 0)
            return Fail("库位在更新时被其他操作占用");

        _logger.LogInformation("托盘 {PallNo} ({WeightKg}kg) 已分配至 {Location} ({Type})",
            pallNo, weightKg, location.LocationCode, location.LocationType);

        return new AllocationResult
        {
            Success = true,
            LocationCode = location.LocationCode,
            LocationType = location.LocationType,
            WeightKg = weightKg,
            PallNo = pallNo,
            Message = "分配成功"
        };
    }

    public static List<string> GetGroupShelfs(string shelfCode)
    {
        if (!long.TryParse(shelfCode, out var shelfNum))
            return new List<string> { shelfCode };

        var groupStart = shelfNum % 2 == 1 ? shelfNum : shelfNum - 1;
        return new List<string>
        {
            groupStart.ToString(),
            (groupStart + 1).ToString()
        };
    }

    private string GeneratePallNo()
    {
        var today = DateTime.Now.ToString("yyyyMMdd");
        var sequence = IncrementSequence(today);
        return $"PALL{today}{sequence:D4}";
    }

    /// <summary>
    /// 原子递增当日序号并返回新值，避免"先查后改"在并发下生成重复的 PallNo。
    /// </summary>
    private int IncrementSequence(string today)
    {
        const string updateSql = """
            UPDATE serialsequence
            SET CurrentSequence = ISNULL(CurrentSequence, 0) + 1
            OUTPUT inserted.CurrentSequence
            WHERE SerialDate = @date
            """;

        var updated = _db.Ado.SqlQuery<int>(updateSql, new SugarParameter("@date", today));
        if (updated.Count > 0)
            return updated[0];

        try
        {
            _db.Insertable(new SerialSequence
            {
                SerialDate = today,
                CurrentSequence = 1
            }).ExecuteCommand();
            return 1;
        }
        catch
        {
            // 当日记录已被并发请求插入，主键冲突，重走递增取号
            var retried = _db.Ado.SqlQuery<int>(updateSql, new SugarParameter("@date", today));
            if (retried.Count > 0)
                return retried[0];
            throw;
        }
    }
    public AllocationResult LockLocation(string locationCode)
    {
        var loc = _db.Queryable<Location>()
            .Where(l => l.Reserve5 == locationCode)
            .First();

        if (loc == null)
            return Fail("库位不存在");

        var rows = _db.Updateable<Location>()
            .SetColumns(l => new Location
            {
                Status = 1,
                UpdateTime = DateTime.Now
            })
            .Where(l => l.Reserve5 == locationCode)
            .ExecuteCommand();

        return rows > 0
            ? new AllocationResult { Success = true, Message = "库位已锁定" }
            : Fail("库位锁定失败");
    }

    public Location? GetLocationByReserve5(string reserve5)
    {
        return _db.Queryable<Location>()
            .Where(l => l.Reserve5 == reserve5)
            .First();
    }

    public LocationDetailResult? GetLocationDetailByBarcode(string barcode)
    {
        var loc = _db.Queryable<Location>()
            .Where(l => l.Reserve5 == barcode)
            .First();

        if (loc == null)
            return null;

        var detail = new LocationDetailResult
        {
            LocationCode = loc.LocationCode,
            LocationType = loc.LocationType,
            ShelfCode = loc.ShelfCode,
            Status = loc.Status,
            StatusText = loc.Status == 0 ? "空闲" : "有货",
            PallNo = loc.PallNo,
            TotalWeight = loc.TotalWeight,
            LimitWeight = loc.LimitWeightt,
            Reserve5 = loc.Reserve5,
            EnableFlag = loc.EnableFlag
        };

        if (string.IsNullOrEmpty(loc.PallNo))
            return detail;

        var pallMater = _db.Queryable<PallMater>()
            .Where(p => p.PallNo == loc.PallNo)
            .First();

        if (pallMater == null)
            return detail;

        var slots = new (string?, decimal?)[]
        {
            (pallMater.SubTitle1, pallMater.Weigh1),
            (pallMater.SubTitle2, pallMater.Weigh2),
            (pallMater.SubTitle3, pallMater.Weigh3),
            (pallMater.SubTitle4, pallMater.Weigh4),
            (pallMater.SubTitle5, pallMater.Weigh5),
            (pallMater.SubTitle6, pallMater.Weigh6),
            (pallMater.SubTitle7, pallMater.Weigh7),
            (pallMater.SubTitle8, pallMater.Weigh8),
            (pallMater.SubTitle9, pallMater.Weigh9),
            (pallMater.SubTitle10, pallMater.Weigh10),
            (pallMater.SubTitle11, pallMater.Weigh11),
            (pallMater.SubTitle12, pallMater.Weigh12),
            (pallMater.SubTitle13, pallMater.Weigh13),
            (pallMater.SubTitle14, pallMater.Weigh14),
            (pallMater.SubTitle15, pallMater.Weigh15),
        };

        var products = new List<LocationProductInfo>();
        foreach (var (subTitle, weight) in slots)
        {
            if (string.IsNullOrEmpty(subTitle)) continue;

            var barcodeInfo = _db.Queryable<Barcode>()
                .Where(b => b.Number == subTitle)
                .First();

            products.Add(new LocationProductInfo
            {
                Barcode = subTitle,
                Weight = weight,
                MaterialNo = barcodeInfo?.MaterialNo,
                MaterialName = barcodeInfo?.MaterialName,
                MaterialModel = barcodeInfo?.MaterialModel,
                Qty = barcodeInfo?.AuxQty
            });
        }

        detail.Products = products;
        return detail;
    }

    public List<QueryByNoItem> QueryLocationsByMaterial(string code)
    {
        const string sql = """
            SELECT
                LocationCode,
                Reserve5,
                LocationType,
                LimitWeightt,
                TotalWeight,
                PallNo,
                PallWeight,
                BarcodeNumber,
                CustomerName,
                BarType,
                Qty,
                AuxQty,
                WarehouseName,
                MaterialNo,
                MaterialName,
                SubTitleIndex,
                SubTitleValue,
                CorrespondingWeight
            FROM [dbo].[querybyno]
            WHERE SubTitleValue LIKE @pattern + '%'
            ORDER BY PallNo, SubTitleIndex
            """;

        return _db.Ado.SqlQuery<QueryByNoItem>(sql, new SugarParameter("@pattern", code));
    }

    private static AllocationResult Fail(string message) =>
        new AllocationResult { Success = false, Message = message };
}

public class LocationDetailResult
{
    public bool Success { get; set; } = true;
    public string? LocationCode { get; set; }
    public string? LocationType { get; set; }
    public string? ShelfCode { get; set; }
    public byte Status { get; set; }
    public string StatusText { get; set; } = "";
    public string? PallNo { get; set; }
    public decimal? TotalWeight { get; set; }
    public decimal? LimitWeight { get; set; }
    public string? Reserve5 { get; set; }
    public bool? EnableFlag { get; set; }
    public List<LocationProductInfo>? Products { get; set; }
}

public class LocationProductInfo
{
    public string? Barcode { get; set; }
    public decimal? Weight { get; set; }
    public string? MaterialNo { get; set; }
    public string? MaterialName { get; set; }
    public string? MaterialModel { get; set; }
    public decimal? Qty { get; set; }
}

public class QueryByNoItem
{
    public string? LocationCode { get; set; }
    public string? Reserve5 { get; set; }
    public string? LocationType { get; set; }
    public decimal? LimitWeightt { get; set; }
    public decimal? TotalWeight { get; set; }
    public string? PallNo { get; set; }
    public decimal? PallWeight { get; set; }
    public string? BarcodeNumber { get; set; }
    public string? CustomerName { get; set; }
    public string? BarType { get; set; }
    public decimal? Qty { get; set; }
    public decimal? AuxQty { get; set; }
    public string? WarehouseName { get; set; }
    public string? MaterialNo { get; set; }
    public string? MaterialName { get; set; }
    public int? SubTitleIndex { get; set; }
    public string? SubTitleValue { get; set; }
    public decimal? CorrespondingWeight { get; set; }
}
