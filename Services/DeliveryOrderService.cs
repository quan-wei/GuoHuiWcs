using Guohui_Wcs.Models.Kingdee;
using Models;
using SqlSugar;

namespace Guohui_Wcs.Services;

/// <summary>
/// 出库通知单处理服务：从金蝶获取出库单 → 匹配托盘 → 生成队列任务
/// </summary>
public class DeliveryOrderService
{
    private readonly KingdeeApiService _kingdeeApi;
    private readonly SqlSugarScope _db;
    private readonly ILogger<DeliveryOrderService> _logger;

    public class QtyObject
    {
        //已有库存张数集合
        public decimal? qty { get; set; }

        //已有库存张数集合数量
        public int? qtyCount { get; set; }
    }

    public List<Dictionary<decimal, int>> resultList = new List<Dictionary<decimal, int>>();

    public DeliveryOrderService(KingdeeApiService kingdeeApi, SqlSugarScope db, ILogger<DeliveryOrderService> logger)
    {
        _kingdeeApi = kingdeeApi;
        _db = db;
        _logger = logger;
    }

    /// <summary>
    /// 根据出库通知单号，匹配托盘并生成出库队列任务
    /// </summary>
    public async Task<DeliveryProcessResult> ProcessDeliveryAsync(string deliveryNo)
    {
        // 1. 从金蝶获取出库通知单
        var response = await _kingdeeApi.ViewAsync<KingdeeDeliveryNotice>("SAL_DELIVERYNOTICE", deliveryNo);
        if (response == null || !response.Result.ResponseStatus.IsSuccess)
            return new DeliveryProcessResult { Success = false, Message = "金蝶查询失败，请检查单据号或登录配置" };
        //var json = File.ReadAllText("D:\\code\\国辉仓控\\64dc9982-2f88-4b57-b289-19cb7bae6e7a.json");
        //var response = JsonConvert.DeserializeObject<KingdeeViewResponse<KingdeeDeliveryNotice>>(json);

        var notice = response.Result.Data!;
        if (notice.Entries == null || notice.Entries.Count == 0)
            return new DeliveryProcessResult { Success = false, Message = "出库通知单无分录行" };

        var createdTasks = new List<DeliveryTaskInfo>();
        var errors = new List<string>();

        // 2. 遍历每个分录行，匹配托盘
        foreach (var entry in notice.Entries)
        {
            var materialCode = entry.Material?.Number;
            if (string.IsNullOrEmpty(materialCode))
            {
                errors.Add($"分录行 {entry.Seq}: 物料编码为空");
                continue;
            }

            //找出物料对应的所有托盘
            var barcodeNumbers = await _db.Queryable<QueryByNo>()
                .Where(t => t.MaterialNo == materialCode)
                .OrderBy(t => t.BarcodeNumber)
                .ToListAsync();

            if (barcodeNumbers.Count == 0)
            {
                errors.Add($"分录行 {entry.Seq}: 物料 {materialCode} 未找到对应条码");
                continue;
            }

            //计算总张数
            var totalQty = barcodeNumbers.Sum(t => t.Qty);

            //库存相等
            if (totalQty == entry.Qty)
            {
                // 3. 为每个匹配的托盘生成队列任务
                foreach (var pallet in barcodeNumbers)
                {
                    var taskName = $"OUT-{deliveryNo}-{entry.Seq}-{pallet.PallNo}";

                    createdTasks.Add(new DeliveryTaskInfo
                    {
                        TaskName = taskName,
                        PallNo = pallet.PallNo,
                        MaterialCode = materialCode,
                        LocationCode = pallet.LocationCode,
                        Seq = entry.Seq
                    });
                }
            }
            //库存不足
            else if (totalQty < entry.Qty)
            {
                // 3. 为每个匹配的托盘生成队列任务
                foreach (var pallet in barcodeNumbers)
                {
                    var taskName = $"OUT-{deliveryNo}-{entry.Seq}-{pallet.PallNo}";

                    createdTasks.Add(new DeliveryTaskInfo
                    {
                        TaskName = taskName,
                        PallNo = pallet.PallNo,
                        MaterialCode = materialCode,
                        LocationCode = pallet.LocationCode,
                        Seq = entry.Seq
                    });
                }

                errors.Add($"分录行 {entry.Seq}: 物料 {materialCode} 库存不足，库存数量 {totalQty} ，确认需要出库吗");
            }
            //库存充足
            else
            {
                var currentComb = new Dictionary<decimal, int>();
                var queryQties = barcodeNumbers.GroupBy(t => t.Qty).Select(t => new QtyObject { qty = t.Key, qtyCount = t.Count() }).ToList();

                TryFindComb(queryQties, entry.Qty, 0, 0, currentComb);

                if (resultList.Count > 0)
                {
                    foreach (var item in resultList[1])
                    {
                        var qty = item.Key;
                        var qtyCount = item.Value;

                        var fiterBarcodeNumbers = barcodeNumbers.Where(t => t.Qty == qty).Take(qtyCount).ToList();

                        // 3. 为每个匹配的托盘生成队列任务
                        foreach (var pallet in fiterBarcodeNumbers)
                        {
                            var taskName = $"OUT-{deliveryNo}-{entry.Seq}-{pallet.PallNo}";

                            createdTasks.Add(new DeliveryTaskInfo
                            {
                                TaskName = taskName,
                                PallNo = pallet.PallNo,
                                MaterialCode = materialCode,
                                LocationCode = pallet.LocationCode,
                                Seq = entry.Seq
                            });
                        }
                    }
                }
                else
                {
                    errors.Add($"分录行 {entry.Seq}: 物料 {materialCode} 未匹配到合适的张数，请手动出库拆分");
                }

            }
        }

        return new DeliveryProcessResult
        {
            Success = true,
            Message = $"处理完成: 创建 {createdTasks.Count} 个任务",
            DeliveryNo = deliveryNo,
            Tasks = createdTasks,
            Errors = errors
        };
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="target"></param>
    /// <param name="sum"></param>
    /// <param name="index"></param>
    public void TryFindComb(List<QtyObject> qtyObjects, decimal target, decimal sum, int index, Dictionary<decimal, int> currentComb)
    {
        if (sum == target)
        {
            resultList.Add(new Dictionary<decimal, int>(currentComb));
            return;
        }
        if (index >= qtyObjects.Count || sum > target)
        {
            return;
        }

        var qtyObject = qtyObjects[index];
        for (int i = 0; i <= qtyObject.qtyCount; i++)
        {
            if (i > 0)
            {
                if (!currentComb.ContainsKey(qtyObject.qty.GetValueOrDefault()))
                {
                    currentComb[qtyObject.qty.GetValueOrDefault()] = i;
                }
                else
                {
                    currentComb[qtyObject.qty.GetValueOrDefault()] += i;
                }
            }
            TryFindComb(qtyObjects, target, sum + qtyObject.qty.GetValueOrDefault() * i, index + 1, currentComb);
            if (i > 0)
            {
                if (currentComb[qtyObject.qty.GetValueOrDefault()] <= i)
                {
                    currentComb.Remove(qtyObject.qty.GetValueOrDefault());
                }
                else
                {
                    currentComb[qtyObject.qty.GetValueOrDefault()] -= i;
                }
            }
        }
    }

    public async Task<List<Queues>> CreatQueues(List<DeliveryTaskInfo> infos)
    {
        var createdTasks = new List<Queues>();

        if (infos != null && infos.Count > 0)
        {
            var loc = _db.Queryable<Location>().Where(t => t.Reserve5.StartsWith("G") && t.Status == 0 && t.EnableFlag == true).ToList();

            if (loc == null || loc.Count == 0)
            {
                throw new Exception("没有空闲的地面库位");
            }

            for (var i = 0; i < (infos.Count > loc.Count ? loc.Count : infos.Count); i++)
            {
                var queue = new Queues
                {
                    TaskName = infos[i].TaskName,
                    PallNo = infos[i].PallNo,
                    Type = "出库",
                    GetLocation = infos[i].LocationCode ?? "",
                    PutLocation = loc[i].LocationCode,
                    Status = "0",
                    CreateTime = DateTime.Now
                };

                await _db.Insertable(queue).ExecuteCommandAsync();

                createdTasks.Add(queue);
            }
        }
        return createdTasks;
    }
}

public class DeliveryProcessResult
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public string? DeliveryNo { get; set; }
    public List<DeliveryTaskInfo> Tasks { get; set; } = new();
    public List<string> Errors { get; set; } = new();
}

public class DeliveryTaskInfo
{
    public string? TaskName { get; set; }
    public string? PallNo { get; set; }
    public string? MaterialCode { get; set; }
    public string? LocationCode { get; set; }
    public int Seq { get; set; }
}
