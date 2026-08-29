using Guohui_Wcs.Models.Kingdee;
using Guohui_Wcs.Services;
using Guohui_Wcs.Utils.AGVUtils;
using Microsoft.AspNetCore.Mvc;

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
        var agvResult = _agvOrder.CreateTask(target);

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
        var agvResult = _agvOrder.CreateTask(target);

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
        return Ok(result);
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
