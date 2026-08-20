using GuoHui_Data.DaoEntity;
using Guohui_Wcs.Models;
using Microsoft.AspNetCore.Mvc;
using Models;
using NLog;
using System.Text.Json;

namespace Guohui_Wcs.Controllers
{
    [ApiController]
    [Route("agv/agvCallbackService")]
    public class ReportController : Controller
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        private readonly ILogger<ReportController> _logger;

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

            var queue = Model_Data.Db.Queryable<queues>()
                .Where(q => q.TaskName == taskCode)
                .First();

            if (queue == null)
            {
                Logger.Warn("AGV回调 未找到任务: TaskCode={TaskCode}", taskCode);
                return Ok(new
                {
                    code = "0",
                    message = "task not found",
                    reqCode = jsonData.ReqCode ?? string.Empty
                });
            }

            switch (method.ToLower())
            {
                case "start":
                    queue.Status = "1";
                    Model_Data.Db.Updateable(queue).UpdateColumns(q => q.Status).ExecuteCommand();
                    ReleaseLocation(queue.GetLocation);
                    Logger.Info("AGV任务开始: TaskCode={TaskCode}, 起点释放={GetLocation}", taskCode, queue.GetLocation);
                    break;

                case "begin":
                    queue.Status = "2";
                    Model_Data.Db.Updateable(queue).UpdateColumns(q => q.Status).ExecuteCommand();
                    ReleaseLocation(queue.GetLocation);
                    Logger.Info("AGV任务执行中: TaskCode={TaskCode}, 起点释放={GetLocation}", taskCode, queue.GetLocation);
                    break;

                case "end":
                    queue.Status = "3";
                    Model_Data.Db.Updateable(queue).UpdateColumns(q => q.Status).ExecuteCommand();
                    OccupyLocation(queue.PutLocation);
                    Logger.Info("AGV任务完成: TaskCode={TaskCode}, 终点占用={PutLocation}", taskCode, queue.PutLocation);
                    break;

                case "cancel":
                    queue.Status = "4";
                    queue.Reserver3 = "任务取消";
                    Model_Data.Db.Updateable(queue).UpdateColumns(q => new { q.Status, q.Reserver3 }).ExecuteCommand();
                    Logger.Info("AGV任务取消: TaskCode={TaskCode}, 备注=任务取消", taskCode);
                    break;

                default:
                    Logger.Warn("AGV回调未知method: {Method}", method);
                    break;
            }

            return Ok(new
            {
                code = "0",
                message = "success",
                reqCode = jsonData.ReqCode ?? string.Empty
            });
        }

        private void ReleaseLocation(string? locationCode)
        {
            if (string.IsNullOrEmpty(locationCode)) return;
            var rows = Model_Data.Db.Updateable<Location>()
                .SetColumns(l => new Location { Status = 0, PallNo = null, TotalWeight = null, UpdateTime = DateTime.Now })
                .Where(l => l.LocationCode == locationCode && l.Status == 1)
                .ExecuteCommand();
            Logger.Info("释放库位 {LocationCode}, 影响行数={Rows}", locationCode, rows);
        }

        private void OccupyLocation(string? locationCode)
        {
            if (string.IsNullOrEmpty(locationCode)) return;
            var rows = Model_Data.Db.Updateable<Location>()
                .SetColumns(l => new Location { Status = 1, UpdateTime = DateTime.Now })
                .Where(l => l.LocationCode == locationCode && l.Status == 2)
                .ExecuteCommand();
            Logger.Info("占用库位 {LocationCode}, 影响行数={Rows}", locationCode, rows);
        }
    }
}
