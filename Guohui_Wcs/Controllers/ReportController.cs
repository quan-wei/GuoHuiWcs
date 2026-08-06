using GuoHui_Data.DaoEntity;
using Guohui_Wcs.Models;
using Microsoft.AspNetCore.Mvc;
using NLog;
using System.Net.Sockets;

namespace Guohui_Wcs.Controllers
{
    [ApiController]
    [Route("agv/agvCallbackService")]
    public class ReportController : Controller
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        private readonly ILogger<ReportController> _logger;
        [HttpPost("agvCallback")]
        public  IActionResult UpdateGlobalVariableAsync([FromBody] RobotTaskNotification jsonData)
        {
            if (jsonData == null)
            {
                return Ok(new
                {
                    code = "1",
                    message = "Invalid JSON data",
                    reqCode = string.Empty
                });
            }
            else 
            {
                return Ok(new
                {
                    code = "0",
                    message = "0",
                    reqCode = string.Empty
                });
            }

        }

    }
}
