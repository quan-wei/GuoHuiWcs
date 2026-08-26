using GuoHui_Data.DaoEntity;
using Guohui_Wcs.Helper.AgvOrderHleper;
using Guohui_Wcs.Models.Kingdee;
using Guohui_Wcs.Services;
using Microsoft.AspNetCore.Mvc;
using Models;
using SqlSugar;

namespace Guohui_Wcs.Controllers;

[ApiController]
[Route("api/[controller]")]
public class LocationController : ControllerBase
{
    public class locks
    {
        public string? Reason {  get; set; }
    }

    public sealed class PallMaterCodeItem
    {
        public string? PallNo { get; set; }
        public string? LocationCode { get; set; }
        public string? ShelfCode { get; set; }
        public string? MaterialCode { get; set; }
        public decimal? Weight { get; set; }
    }

    private readonly LocationAllocationService _allocationService;
    private readonly KingdeeApiService _kingdeeApi;
    private readonly DeliveryOrderService _deliveryService;
    private readonly ILogger<LocationController> _logger;
    AGVOrderHelper aGVOrder= new AGVOrderHelper("191.167.10.5:8181");

    public LocationController(LocationAllocationService allocationService, KingdeeApiService kingdeeApi, DeliveryOrderService deliveryService, ILogger<LocationController> logger)
    {
        _allocationService = allocationService;
        _kingdeeApi = kingdeeApi;
        _deliveryService = deliveryService;
        _logger = logger;
    }

    [HttpPost("allocate")]
    public async Task<IActionResult> Allocate([FromBody] AllocationRequest request)
    {
        var result = await _allocationService.Allocate(request);
        if (!result.Success)
            return BadRequest(result);
        var point =Model_Data.Db.Queryable<Location>().Where(l => l.Reserve5 == request.StartPoint).First();

        var target = new List<string> { point.LocationCode!, result.LocationCode! };
        var agvResult = aGVOrder.CreateTask(target);

        if (agvResult == null || agvResult.Code != "0")
        {
            _allocationService.RollbackAllocation(result.LocationCode!, result.PallNo!);
            var errMsg = agvResult?.Message ?? "AGV系统无响应";
            _logger.LogError("AGV任务创建失败: {Message}, 已回滚库位 {Location}", errMsg, result.LocationCode);
            return BadRequest(new { Success = false, Message = $"AGV搬运任务创建失败: {errMsg}" });
        }

        _allocationService.Release(point.Reserve5!);
        return Ok(result);
    }

    [HttpPost("allocate/{locationCode}")]
    public async Task<IActionResult> AllocateToSpecific(string locationCode, [FromBody] AllocationRequest request)
    {
        var result =await  _allocationService.AllocateToSpecific(locationCode, request);
        if (!result.Success)
            return BadRequest(result);

        var startPoint = Model_Data.Db.Queryable<Location>().Where(l => l.Reserve5 == request.StartPoint).First();
        var target = new List<string> { startPoint.LocationCode!, result.LocationCode! };
        var agvResult = aGVOrder.CreateTask(target);

        if (agvResult == null || agvResult.Code != "0")
        {
            _allocationService.RollbackAllocation(result.LocationCode!, result.PallNo!);
            var errMsg = agvResult?.Message ?? "AGV系统无响应";
            _logger.LogError("AGV任务创建失败: {Message}, 已回滚库位 {Location}", errMsg, result.LocationCode);
            return BadRequest(new { Success = false, Message = $"AGV搬运任务创建失败: {errMsg}" });
        }

        _allocationService.Release(startPoint.Reserve5!);
        return Ok(result);
    }

    [HttpPost("release/{locationCode}")]
    public IActionResult Release(string locationCode)
    {
        var result = _allocationService.Release(locationCode);
        if (result.Success)
            return Ok(result);
        return BadRequest(result);
    }

    [HttpPost("lock/{locationCode}")]
    public IActionResult Lock(string locationCode, [FromBody] locks locks)
    {
        var loc = Model_Data.Db.Queryable<Location>()
            .Where(l => l.Reserve5 == locationCode)
            .First();
        Console.WriteLine(locks.Reason);
        if (loc == null)
            return BadRequest(new { Success = false, Message = "库位不存在" });

        var rows = Model_Data.Db.Updateable<Location>()
            .SetColumns(l => new Location
            {
                Status = 1,
                UpdateTime = DateTime.Now
            })
            .Where(l => l.Reserve5 == locationCode)
            .ExecuteCommand();

        return rows > 0
            ? Ok(new { Success = true, Message = "库位已锁定" })
            : BadRequest(new { Success = false, Message = "库位锁定失败" });
    }

    [HttpGet("group-load/{shelfCode}")]
    public IActionResult GetGroupLoad(string shelfCode)
    {
        var info = _allocationService.GetGroupLoad(shelfCode);
        return Ok(info);
    }

   [HttpGet("query-delivery/{number}")]
   public async Task<IActionResult> QueryDeliveryNotice(string number)
   {
       var response = await _kingdeeApi.ViewAsync<KingdeeDeliveryNotice>("SAL_DELIVERYNOTICE", number);
       if (response == null || !response.Result.ResponseStatus.IsSuccess)
           return BadRequest(new { Success = false, Message = "金蝶查询失败，请检查单据号或登录配置" });

       var notice = response.Result.Data!;
       return Ok(new
       {
           Success = true,
           notice.BillNo,
           notice.DocumentStatus,
           notice.Date,
           Customer = notice.Customer?.Name,
           notice.Note,
           Entries = notice.Entries?.Select(e => new
           {
               e.Seq,
               MaterialCode = e.Material?.Number,
               MaterialName = e.Material?.Name,
               e.Qty,
               Unit = e.Unit?.Name,
               Warehouse = e.Stock?.Name,
               e.Lot
           })
       });
   }

    [HttpPost("process-delivery/{number}")]
    public async Task<IActionResult> ProcessDelivery(string number)
    {
        var result = await _deliveryService.ProcessDeliveryAsync(number);
        if (!result.Success)
            return BadRequest(result);
        return Ok(result);
    }

    [HttpGet("query-by-barcode/{barcode}")]
    public IActionResult QueryByBarcode(string barcode)
    {
        var loc = Model_Data.Db.Queryable<Location>()
            .Where(l => l.Reserve5 == barcode)
            .First();

        if (loc == null)
            return BadRequest(new { Success = false, Message = "未找到该条码对应的库位" });

        // 查询托盘产品信息
        object? products = null;
        if (!string.IsNullOrEmpty(loc.PallNo))
        {
            var pallMater = Model_Data.Db.Queryable<PallMater>()
                .Where(p => p.PallNo == loc.PallNo)
                .First();

            if (pallMater != null)
            {
                var productList = new List<object>();
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

                foreach (var (subTitle, weight) in slots)
                {
                    if (string.IsNullOrEmpty(subTitle)) continue;

                    var barcodeInfo = Model_Data.Db.Queryable<Barcode>()
                        .Where(b => b.Number == subTitle)
                        .First();

                    productList.Add(new
                    {
                        Barcode = subTitle,
                        Weight = weight,
                        MaterialNo = barcodeInfo?.MaterialNo,
                        MaterialName = barcodeInfo?.MaterialName,
                        MaterialModel = barcodeInfo?.MaterialModel,
                        Qty = barcodeInfo?.AuxQty
                    });
                }

                products = productList;
            }
        }

        return Ok(new
        {
            Success = true,
            loc.LocationCode,
            loc.LocationType,
            loc.ShelfCode,
            loc.Status,
            StatusText = loc.Status == 0 ? "空闲" : "有货",
            loc.PallNo,
            loc.TotalWeight,
            LimitWeight = loc.LimitWeightt,
            loc.Reserve5,
            loc.EnableFlag,
            Products = products
        });
    }

    [HttpGet("query-by-material/{code}")]
    public IActionResult QueryByMaterialCode(string code)
    {
        if (string.IsNullOrWhiteSpace(code))
            return BadRequest(new { Success = false, Message = "编码不能为空" });

        const string sql = """
            SELECT
                pm.PallNo,
                pm.LocationCode,
                pm.ShelfCode,
                v.SubTitle AS MaterialCode,
                v.Weigh AS Weight
            FROM [dbo].[PallMater] pm
            CROSS APPLY (VALUES
                (pm.SubTitle1,  pm.Weigh1),
                (pm.SubTitle2,  pm.Weigh2),
                (pm.SubTitle3,  pm.Weigh3),
                (pm.SubTitle4,  pm.Weigh4),
                (pm.SubTitle5,  pm.Weigh5),
                (pm.SubTitle6,  pm.Weigh6),
                (pm.SubTitle7,  pm.Weigh7),
                (pm.SubTitle8,  pm.Weigh8),
                (pm.SubTitle9,  pm.Weigh9),
                (pm.SubTitle10, pm.Weigh10),
                (pm.SubTitle11, pm.Weigh11),
                (pm.SubTitle12, pm.Weigh12),
                (pm.SubTitle13, pm.Weigh13),
                (pm.SubTitle14, pm.Weigh14),
                (pm.SubTitle15, pm.Weigh15)
            ) AS v(SubTitle, Weigh)
            WHERE v.SubTitle LIKE @pattern + '%'
            ORDER BY pm.PallNo
            """;

        var items = Model_Data.Db.Ado.SqlQuery<PallMaterCodeItem>(
            sql,
            new SugarParameter("@pattern", code)).ToList();

        return Ok(new
        {
            Success = true,
            Code = code,
            Count = items.Count,
            Data = items
        });
    }
}
