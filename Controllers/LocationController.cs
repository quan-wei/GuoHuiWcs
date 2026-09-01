using GuoHui_Data.DaoEntity;
using Guohui_Wcs.Models.Kingdee;
using Guohui_Wcs.Services;
using Guohui_Wcs.Utils.AGVUtils;
using Microsoft.AspNetCore.Mvc;
using Models;
using NetTaste;

namespace Guohui_Wcs.Controllers;

[ApiController]
[Route("api/[controller]")]
public class LocationController : ControllerBase
{
    private readonly LocationAllocationService _allocationService;
    private readonly KingdeeApiService _kingdeeApi;
    private readonly DeliveryOrderService _deliveryService;
    private readonly AGVOrderHelper _agvOrder;
    private readonly ILogger<LocationController> _logger;

    public LocationController(
        LocationAllocationService allocationService,
        KingdeeApiService kingdeeApi,
        DeliveryOrderService deliveryService,
        AGVOrderHelper agvOrder,
        ILogger<LocationController> logger)
    {
        _allocationService = allocationService;
        _kingdeeApi = kingdeeApi;
        _deliveryService = deliveryService;
        _agvOrder = agvOrder;
        _logger = logger;
    }

    [HttpPost("allocate")]
    public async Task<IActionResult> Allocate([FromBody] AllocationRequest request)
    {
        var result = await _allocationService.Allocate(request);
        if (!result.Success)
            return BadRequest(result);

        var startPoint = _allocationService.GetLocationByReserve5(request.StartPoint!);
        if (startPoint == null)
        {
            _allocationService.RollbackAllocation(result.LocationCode!, result.PallNo!);
            return BadRequest(new { Success = false, Message = "起点库位不存在，已回滚本次分配" });
        }

        var target = new List<string> { startPoint.LocationCode, result.LocationCode! };
        var agvResult = _agvOrder.CreateTask(target, result.TaskName);

        if (agvResult == null || agvResult.Code != "0")
        {
            _allocationService.RollbackAllocation(result.LocationCode!, result.PallNo!);
            var errMsg = agvResult?.Message ?? "AGV系统无响应";
            _logger.LogError("AGV任务创建失败: {Message}, 已回滚库位 {Location}", errMsg, result.LocationCode);
            return BadRequest(new { Success = false, Message = $"AGV搬运任务创建失败: {errMsg}" });
        }

        // 起点库位不在此处释放，由 AGV start/begin 回调统一释放
        return Ok(result);
    }

    [HttpPost("allocate/{locationCode}")]
    public async Task<IActionResult> AllocateToSpecific(string locationCode, [FromBody] AllocationRequest request)
    {
        var result = await _allocationService.AllocateToSpecific(locationCode, request);
        if (!result.Success)
            return BadRequest(result);

        var startPoint = _allocationService.GetLocationByReserve5(request.StartPoint!);
        if (startPoint == null)
        {
            _allocationService.RollbackAllocation(result.LocationCode!, result.PallNo!);
            return BadRequest(new { Success = false, Message = "起点库位不存在，已回滚本次分配" });
        }

        var target = new List<string> { startPoint.LocationCode, result.LocationCode! };
        var agvResult = _agvOrder.CreateTask(target, result.TaskName);

        if (agvResult == null || agvResult.Code != "0")
        {
            _allocationService.RollbackAllocation(result.LocationCode!, result.PallNo!);
            var errMsg = agvResult?.Message ?? "AGV系统无响应";
            _logger.LogError("AGV任务创建失败: {Message}, 已回滚库位 {Location}", errMsg, result.LocationCode);
            return BadRequest(new { Success = false, Message = $"AGV搬运任务创建失败: {errMsg}" });
        }

        // 起点库位不在此处释放，由 AGV start/begin 回调统一释放
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
    public IActionResult Lock(string locationCode, [FromBody] LockRequest request)
    {
        var result = _allocationService.LockLocation(locationCode);
        if (result.Success)
            return Ok(result);
        return BadRequest(result);
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

        var loc = Model_Data.Db.Queryable<Location>().Where(t => t.Reserve5!.StartsWith('G') && t.Status == 0 && t.EnableFlag == true).ToList();

        if (loc == null || loc.Count == 0)
        {
            return BadRequest(new DeliveryProcessResult { Success = false, Message = "没有空闲的地面库位" });
        }

        if (result.Tasks.Count > loc.Count)
        {
            result.Message += "空闲地面库位不足，只会下架部分物料，请注意";
        }

        return Ok(result);
    }

    [HttpPost("delivery-queues")]
    public async Task<IActionResult> DeliveryQueues([FromBody] List<DeliveryTaskInfo> tasks)
    {
        try
        {
            var errorMsg = "";

            var result = await _deliveryService.CreatQueues(tasks);

            if (result.Count > 0)
            {
                foreach (var item in result)
                {
                    var target = new List<string> { item.GetLocation!, item.PutLocation! };
                    var agvResult = _agvOrder.CreateTask(target);

                    if (agvResult == null || agvResult.Code != "0")
                    {
                        var errMsg = agvResult?.Message ?? "AGV系统无响应";
                        _logger.LogError("AGV任务创建失败: {Message}", errMsg);
                        errorMsg += $"AGV搬运任务创建失败: {errMsg}";
                    }

                    var lRows = Model_Data.Db.Updateable<Location>()
                        .SetColumns(l => new Location
                        {
                            Status = 2,
                            UpdateTime = DateTime.Now
                        })
                        .Where(l => l.LocationCode == item.PutLocation)
                        .ExecuteCommand();
                }
            }
            return Ok(new AllocationResult { Success = true, Message = !string.IsNullOrWhiteSpace(errorMsg) ? errorMsg : "任务执行成功" });
        }
        catch (Exception ex)
        {
            return BadRequest(new AllocationResult { Success = false, Message = ex.Message });
        }
    }

    [HttpGet("query-by-barcode/{barcode}")]
    public IActionResult QueryByBarcode(string barcode)
    {
        var detail = _allocationService.GetLocationDetailByBarcode(barcode);
        if (detail == null)
            return BadRequest(new { Success = false, Message = "未找到该条码对应的库位" });

        return Ok(detail);
    }

    [HttpGet("query-by-material/{code}")]
    public IActionResult QueryByMaterialCode(string code)
    {
        if (string.IsNullOrWhiteSpace(code))
            return BadRequest(new { Success = false, Message = "编码不能为空" });

        var items = _allocationService.QueryLocationsByMaterial(code);

        return Ok(new
        {
            Success = true,
            Code = code,
            Count = items.Count,
            Data = items
        });
    }
}
