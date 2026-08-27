using GuoHui_Data.DaoEntity;
using Guohui_Wcs.Helper.AgvOrderHleper;
using Guohui_Wcs.Models.Kingdee;
using Guohui_Wcs.Services;
using Microsoft.AspNetCore.Mvc;
using Models;
using SqlSugar;
using System.Drawing;

namespace Guohui_Wcs.Controllers;

[ApiController]
[Route("api/[controller]")]
public class LocationController : ControllerBase
{
    public sealed class QueryByNoItem
    {
        public string? LocationCode { get; set; }
        public string? Reserve5 { get; set; }
        public string? LocationType { get; set; }
        public decimal? LimitWeightt { get; set; }
        public decimal? TotalWeight { get; set; }
        public string? PallNo { get; set; }
        public decimal? PallWeight { get; set; }
        public string? BarcodeNumber { get; set; }
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

    public class locks
    {
        public string? Reason {  get; set; }
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
        string taskcode=Guid.NewGuid().ToString("N");
        queues queues = new queues()
        {
            TaskName = taskcode,
            PallNo = result.PallNo,
            Type = "入库",
            GetLocation = point.LocationCode,
            PutLocation = result.LocationCode,
            Status = "0",
            CreateTime = DateTime.Now
        };
        Model_Data.Db.Insertable(queues).ExecuteCommand();
        var agvResult = aGVOrder.CreateTask(target, taskcode);

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
        string taskcode = Guid.NewGuid().ToString("N");
        queues queues = new queues()
        {
            TaskName = taskcode,
            PallNo = result.PallNo,
            Type = "入库",
            GetLocation = startPoint.LocationCode,
            PutLocation = locationCode,
            Status = "0",
            CreateTime = DateTime.Now
        };
        Model_Data.Db.Insertable(queues).ExecuteCommand();
        var agvResult = aGVOrder.CreateTask(target, taskcode);

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

    [HttpGet("query-by-material/{code}")]
    public IActionResult QueryByMaterialCode(string code)
    {
        if (string.IsNullOrWhiteSpace(code))
            return BadRequest(new { Success = false, Message = "编码不能为空" });

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

        var items = Model_Data.Db.Ado.SqlQuery<QueryByNoItem>(
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
