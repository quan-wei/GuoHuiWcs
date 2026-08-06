using Guohui_Wcs.Helper.AgvOrderHleper;
using Guohui_Wcs.Services;
using Microsoft.AspNetCore.Mvc;

namespace Guohui_Wcs.Controllers;

[ApiController]
[Route("api/[controller]")]
public class LocationController : ControllerBase
{
    private readonly LocationAllocationService _allocationService;
    private readonly ILogger<LocationController> _logger;
    AGVOrderHelper aGVOrder= new AGVOrderHelper("191.167.10.5:8181");
    public LocationController(LocationAllocationService allocationService, ILogger<LocationController> logger)
    {
        _allocationService = allocationService;
        _logger = logger;
    }

    [HttpPost("allocate")]
    public async Task<IActionResult> Allocate([FromBody] AllocationRequest request)
    {
        var result = await _allocationService.Allocate(request);
        if (!result.Success)
            return BadRequest(result);

        var target = new List<string> { request.StartPoint!, result.LocationCode! };
        var agvResult = aGVOrder.CreateTask(target);

        if (agvResult == null || agvResult.code != "0")
        {
            _allocationService.RollbackAllocation(result.LocationCode!, result.PallNo!);
            var errMsg = agvResult?.message ?? "AGV系统无响应";
            _logger.LogError("AGV任务创建失败: {Message}, 已回滚库位 {Location}", errMsg, result.LocationCode);
            return BadRequest(new { Success = false, Message = $"AGV搬运任务创建失败: {errMsg}" });
        }

        return Ok(result);
    }

    [HttpPost("allocate/{locationCode}")]
    public async Task<IActionResult> AllocateToSpecific(string locationCode, [FromBody] AllocationRequest request)
    {
        var result =await  _allocationService.AllocateToSpecific(locationCode, request);
        if (!result.Success)
            return BadRequest(result);

        var target = new List<string> { request.StartPoint!, result.LocationCode! };
        var agvResult = aGVOrder.CreateTask(target);

        if (agvResult == null || agvResult.code != "0")
        {
            _allocationService.RollbackAllocation(result.LocationCode!, result.PallNo!);
            var errMsg = agvResult?.message ?? "AGV系统无响应";
            _logger.LogError("AGV任务创建失败: {Message}, 已回滚库位 {Location}", errMsg, result.LocationCode);
            return BadRequest(new { Success = false, Message = $"AGV搬运任务创建失败: {errMsg}" });
        }

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

    [HttpGet("group-load/{shelfCode}")]
    public IActionResult GetGroupLoad(string shelfCode)
    {
        var info = _allocationService.GetGroupLoad(shelfCode);
        return Ok(info);
    }
}
