using Guohui_Wcs.Models;
using Guohui_Wcs.Services;
using Microsoft.AspNetCore.Mvc;
using NLog;
using System.Text.Json;

namespace Guohui_Wcs.Controllers
{
    [ApiController]
    [Route("agv/agvCallbackService")]
    public class ReportController : Controller
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        private readonly LocationAllocationService _allocationService;

        public ReportController(LocationAllocationService allocationService)
        {
            _allocationService = allocationService;
        }

        [HttpPost("agvCallback")]
        public IActionResult AgvCallback([FromBody] RobotTaskNotification jsonData)
        {
            Logger.Info("AGV回调入参: {JsonData}", JsonSerializer.Serialize(jsonData));
            if (jsonData == null)
            {
                return Ok(new
                {
                    code = "1",
                    message = "Invalid JSON data",
                    reqCode = string.Empty
                });
            }

            var method = jsonData.Method ?? string.Empty;
            var taskCode = jsonData.TaskCode ?? string.Empty;
            var wbCode = jsonData.WbCode ?? string.Empty;

            Logger.Info("AGV回调 method={Method} taskCode={TaskCode} wbCode={WbCode}", method, taskCode, wbCode);

            if (string.IsNullOrEmpty(taskCode))
            {
                Logger.Warn("AGV回调 taskCode 为空");
                return Ok(new
                {
                    code = "0",
                    message = "taskCode is empty",
                    reqCode = jsonData.ReqCode ?? string.Empty
                });
            }

            var message = _allocationService.HandleAgvCallback(method, taskCode, wbCode);

            return Ok(new
            {
                code = "0",
                message,
                reqCode = jsonData.ReqCode ?? string.Empty
            });
        }
    }
}
