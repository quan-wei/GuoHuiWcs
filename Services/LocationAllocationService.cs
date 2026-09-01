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
    public string? TaskName { get; set; }
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
        string barMsg = "";

        if (string.IsNullOrWhiteSpace(request.StartPoint))
            return Fail("起点不能为空");

        //var pallNo = GeneratePallNo();

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
            else
            {
                barMsg += $"物料编码： {code} 未能在WMS中找到物料信息，上架会排除";
            }
        }

        // 插入 PallMater 记录
        var pallMater = new PallMater
        {
            PallNo = GeneratePallNo(),
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
            allocationResult = TryAllocate(level1, pallMater.PallNo, totalWeight);

        if (allocationResult == null && request.AllowUpperLevels)
        {
            foreach (var tier in UpperTiers)
            {
                var loc = FindUpperLevel(tier, null, totalWeight);
                if (loc != null)
                {
                    allocationResult = TryAllocate(loc, pallMater.PallNo, totalWeight);
                    break;
                }
            }
        }

        if (allocationResult == null)
            return Fail("无可用库位：一层已满，且上层库位均不满足重量限制");

        if (allocationResult.Success)
        {
            _db.Insertable(pallMater).ExecuteCommand();
            allocationResult.TaskName = RecordInboundQueue("IN", pallMater.PallNo, request.StartPoint, allocationResult.LocationCode!);
            _logger.LogInformation("PallMater created: {PallNo}, weight: {Weight}", pallMater.PallNo, totalWeight);
        }

        if (!string.IsNullOrWhiteSpace(barMsg))
        {
            allocationResult.Message += barMsg;
        }


        return allocationResult;
    }

    public async Task<AllocationResult> AllocateToSpecific(string locationCode, AllocationRequest request)
    {
        string barMsg = "";

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
                else
                {
                    barMsg += $"物料编码： {code} 未能在WMS中找到物料信息，上架会排除";
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
            allocationResult.TaskName = RecordInboundQueue("IN", pallNo, request.StartPoint, loc.LocationCode);
            _logger.LogInformation("PallMater created: {PallNo}, weight: {Weight}, from: {Start}, to: {End}",
                pallNo, totalWeight, request.StartPoint, locationCode);

            if (!string.IsNullOrWhiteSpace(barMsg))
            {
                allocationResult.Message += barMsg;
            }

            return allocationResult;
        }
        else
        {
            //var startPosition = _db.Queryable<Location>().First(l => l.Reserve5 == request.StartPoint);

            //if (startPosition == null || string.IsNullOrWhiteSpace(startPosition.LocationCode))
            //{
            //    return Fail("未找到起始库位信息");
            //}

            decimal totalWeight = 0;
            _logger.LogInformation("Outlocate {PallNo}, weight: {Weight}, from: {Start}, to: {End}",
                pallNo, totalWeight, request.StartPoint, locationCode);

            var allocationResult = TryAllocate(loc, pallNo!, totalWeight);
            if (allocationResult.Success)
                allocationResult.TaskName = $"OUT-{pallNo}";
            allocationResult.TaskName = RecordInboundQueue("OUT", pallNo, request.StartPoint, locationCode);
            return allocationResult;
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
        RollbackDestination(locationCode, pallNo);

        _db.Deleteable<Queues>()
            .Where(q => q.PallNo == pallNo && q.Type == "入库")
            .ExecuteCommand();

        _logger.LogWarning("AGV任务失败，已回滚库位 {Location} 和托盘 {PallNo}", locationCode, pallNo);
    }

    /// <summary>
    /// 回滚终点库位（预留/占用 → 空闲）并删除托盘记录，不动 queues 里的任务记录。
    /// </summary>
    private void RollbackDestination(string? locationCode, string? pallNo)
    {
        if (!string.IsNullOrEmpty(locationCode))
        {
            _db.Updateable<Location>()
                .SetColumns(l => new Location
                {
                    Status = 0,
                    PallNo = null,
                    TotalWeight = null,
                    UpdateTime = DateTime.Now
                })
                .Where(l => l.LocationCode == locationCode && l.Status != 0)
                .ExecuteCommand();
        }

        if (!string.IsNullOrEmpty(pallNo))
        {
            _db.Deleteable<PallMater>()
                .Where(p => p.PallNo == pallNo)
                .ExecuteCommand();
        }
    }

    /// <summary>
    /// 按 ProcessDeliveryAsync 出库队列的模板记录一条入库队列任务，供下游 AGV 调度消费。
    /// 返回队列的 TaskName，用作创建 AGV 任务时的 taskCode，便于回调反馈匹配本地任务。
    /// </summary>
    private string RecordInboundQueue(string taskType, string pallNo, string? startPointReserve5, string locationCode)
    {
        var startLoc = string.IsNullOrWhiteSpace(startPointReserve5)
            ? null
            : _db.Queryable<Location>()
                .Where(l => l.Reserve5 == startPointReserve5)
                .First();

        var queue = new Queues
        {
            TaskName = $"{taskType}-{pallNo}",
            PallNo = pallNo,
            Type = taskType == "IN" ? "入库" : "出库",
            GetLocation = startLoc?.LocationCode ?? startPointReserve5 ?? "",
            PutLocation = locationCode,
            Status = "0",
            CreateTime = DateTime.Now
        };

        _db.Insertable(queue).ExecuteCommand();
        _logger.LogInformation("Inbound queue created: {TaskName}, from: {From}, to: {To}",
            queue.TaskName, queue.GetLocation, queue.PutLocation);

        return queue.TaskName!;
    }

    /// <summary>
    /// 处理 AGV 回调：按 taskCode 匹配 queues 里的任务，根据 method 推进任务状态并同步库位状态。
    /// 返回给 AGV 的 message。
    /// </summary>
    public string HandleAgvCallback(string method, string taskCode, string wbCode)
    {
        var queue = _db.Queryable<Queues>()
            .Where(q => q.TaskName == taskCode)
            .First();

        if (queue == null)
        {
            _logger.LogWarning("AGV回调 未找到任务: TaskCode={TaskCode}, WbCode={WbCode}", taskCode, wbCode);
            return "task not found";
        }

        switch (method.ToLowerInvariant())
        {
            case "start":
                queue.Status = "1";
                _db.Updateable(queue).UpdateColumns(q => q.Status).ExecuteCommand();
                ReleaseLocationByCode(queue.GetLocation);
                _logger.LogInformation("AGV任务开始: TaskCode={TaskCode}, 起点释放={GetLocation}", taskCode, queue.GetLocation);
                break;

            case "begin":
                queue.Status = "2";
                _db.Updateable(queue).UpdateColumns(q => q.Status).ExecuteCommand();
                ReleaseLocationByCode(queue.GetLocation);
                _logger.LogInformation("AGV任务执行中: TaskCode={TaskCode}, 起点释放={GetLocation}", taskCode, queue.GetLocation);
                break;

            case "end":
                queue.Status = "3";
                _db.Updateable(queue).UpdateColumns(q => q.Status).ExecuteCommand();
                OccupyLocationByCode(queue.PutLocation);
                _logger.LogInformation("AGV任务完成: TaskCode={TaskCode}, 终点占用={PutLocation}", taskCode, queue.PutLocation);
                break;

            case "cancel":
                queue.Status = "4";
                queue.Reserver3 = "任务取消";
                _db.Updateable(queue).UpdateColumns(q => new { q.Status, q.Reserver3 }).ExecuteCommand();

                // 任务取消：货未送达，回滚终点库位预留并清除托盘记录；队列保留 Status=4 作为取消凭证
                RollbackDestination(queue.PutLocation, queue.PallNo);
                _logger.LogInformation("AGV任务取消: TaskCode={TaskCode}, 已回滚终点 {PutLocation} 和托盘 {PallNo}",
                    taskCode, queue.PutLocation, queue.PallNo);
                break;

            default:
                _logger.LogWarning("AGV回调未知method: {Method}, TaskCode={TaskCode}", method, taskCode);
                break;
        }

        return "success";
    }

    private void ReleaseLocationByCode(string? locationCode)
    {
        if (string.IsNullOrEmpty(locationCode)) return;
        var rows = _db.Updateable<Location>()
            .SetColumns(l => new Location
            {
                Status = 0,
                PallNo = null,
                TotalWeight = null,
                UpdateTime = DateTime.Now
            })
            .Where(l => l.LocationCode == locationCode && l.Status == 1)
            .ExecuteCommand();
        _logger.LogInformation("释放库位 {LocationCode}, 影响行数={Rows}", locationCode, rows);
    }

    private void OccupyLocationByCode(string? locationCode)
    {
        if (string.IsNullOrEmpty(locationCode)) return;
        var rows = _db.Updateable<Location>()
            .SetColumns(l => new Location
            {
                Status = 1,
                UpdateTime = DateTime.Now
            })
            .Where(l => l.LocationCode == locationCode && l.Status == 2)
            .ExecuteCommand();
        _logger.LogInformation("占用库位 {LocationCode}, 影响行数={Rows}", locationCode, rows);
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
        // Status != 0：已占用(1)和预留中(2)的托盘重量都要计入货架对限重
        var total = _db.Queryable<Location>()
            .Where(l => shelfCodes.Contains(l.ShelfCode))
            .Where(l => l.LocationType == tier)
            .Where(l => l.Status != 0)
            .Where(l => !SqlFunc.IsNullOrEmpty(l.PallNo))
            .Sum(l => l.TotalWeight ?? 0m);

        return total;
    }

    private AllocationResult TryAllocate(Location location, string pallNo, decimal? weightKg)
    {
        // Status 2 = 预留：分配时先占位，AGV 送达（end 回调）后才置为 1 正式占用
        var rows = _db.Updateable<Location>()
            .SetColumns(l => new Location
            {
                Status = 2,
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
