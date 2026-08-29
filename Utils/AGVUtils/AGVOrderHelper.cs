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
        /// <param name="taskId">任务唯一标识</param>
        /// <param name="targetPoints">任务目标点</param>
        /// <param name="taskConfig">任务配置的id</param>
        /// <returns></returns>
        public OrderResult CreateTask(List<string> targetPoints)
        {
            string url = BaseUrl + "/rcms/services/rest/hikRpcService/genAgvSchedulingTask";
            Guid guid= Guid.NewGuid();
            var requestParam = new
            {
                reqCode = guid.ToString("N"),
                taskTyp="F11",
                ctnrTyp="1",
                priority="1",
                taskCode= guid.ToString("N"),
                positionCodePath = new[]
                {
        new {
            positionCode = targetPoints[0]+"${05}",
            type = "00"
        },
        new {
            positionCode =  targetPoints[1]+"${05}",
            type ="00"
        }
    }
            };
            HttpUtils httpUtils = new HttpUtils();
            Logger.Info("发送AGV任务指令：" + targetPoints[0] +
                "--指令数据:" + JsonConvert.SerializeObject(requestParam));
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
