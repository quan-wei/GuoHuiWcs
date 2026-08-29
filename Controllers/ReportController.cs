using GuoHui_Data.DaoEntity;
using Guohui_Wcs.Models;
using Microsoft.AspNetCore.Mvc;
using NLog;
using System.Net.Sockets;
using System.Text.Json;

namespace Guohui_Wcs.Controllers
{
    [ApiController]
    [Route("agv/agvCallbackService")]
    public class ReportController : Controller
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        [HttpPost("agvCallback")]
        public IActionResult UpdateGlobalVariableAsync([FromBody] RobotTaskNotification jsonData)
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

            return Ok(new
            {
                code = "0",
                message = "0",
                reqCode = string.Empty
            });
        }

    }
}
