using Guohui_Wcs.Entity;
using Guohui_Wcs.Utils;
using Newtonsoft.Json;
using NLog;

namespace Guohui_Wcs.Utils.AGVUtils
{
    public class AGVOrderHelper
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        public string BaseUrl { get; }

        public AGVOrderHelper(string baseUrl)
        {
            BaseUrl = "http://" + baseUrl;
        }
        /// <summary>
        /// 创建任务
        /// </summary>
        /// <param name="targetPoints">任务目标点</param>
        /// <param name="taskCode">任务编码，AGV 回调反馈原样带回，用于匹配本地 queues 里的任务；不传则用请求号代替</param>
        /// <returns></returns>
        public OrderResult CreateTask(List<string> targetPoints, string? taskCode = null)
        {
            string url = BaseUrl + "/rcms/services/rest/hikRpcService/genAgvSchedulingTask";
            Guid guid= Guid.NewGuid();
            var requestParam = new Dictionary<string, object?>
            {
                ["reqCode"] = guid.ToString("N"),
                ["taskTyp"] = "F11",
                ["ctnrTyp"] = "1",
                ["priority"] = "1",
                ["taskCode"] = string.IsNullOrEmpty(taskCode) ? guid.ToString("N") : taskCode,
                ["positionCodePath"] = new[]
                {
                    new { positionCode = targetPoints[0] + "${05}", type = "00" },
                    new { positionCode = targetPoints[1] + "${05}", type = "00" }
                }
            };
            HttpUtils httpUtils = new HttpUtils();
            Logger.Info("发送AGV任务指令：taskCode={TaskCode}, from={From}, to={To}, 指令数据:{Body}",
                requestParam["taskCode"], targetPoints[0], targetPoints[1], JsonConvert.SerializeObject(requestParam));
            var resultStr = httpUtils.HttpPost(url, requestParam, null);

            if (string.IsNullOrEmpty(resultStr))
            {
                return null;
            }

            var result = JsonConvert.DeserializeObject<OrderResult>(resultStr);
            Logger.Info(resultStr);

            return result;

        }

    }
}
