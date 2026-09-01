using Dm.util;
using GuoHui_Data.DaoEntity;
using Guohui_Wcs.Models.Kingdee;
using Models;
using Newtonsoft.Json;
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

    public class CombinationGroup
    {
        public decimal Total { get; set; }
        public List<QtyObject> Items { get; set; }
    }

    public List<List<decimal>> resultList = new List<List<decimal>>();

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
            var barcodeNumbers_DM = await _db.Queryable<QueryByNo>()
                .Where(t => t.MaterialNo == materialCode && t.LocationType == "地面库位")
                .OrderBy(t => t.BarcodeNumber)
                .ToListAsync();

            //找出物料对应的所有托盘
            var barcodeNumbers_ZK = await _db.Queryable<QueryByNo>()
                .Where(t => t.MaterialNo == materialCode && t.LocationType != "地面库位")
                .OrderBy(t => t.BarcodeNumber)
                .ToListAsync();

            var barcodeNumbers = barcodeNumbers_DM.Concat(barcodeNumbers_ZK).ToList();

            if (barcodeNumbers.Count == 0)
            {
                errors.Add($"分录行 {entry.Seq}: 物料 {materialCode} 未找到对应条码");
                continue;
            }

            //计算总张数
            var totalQty = barcodeNumbers.Sum(t => t.Qty);

            //库存不足或相等
            if (totalQty <= entry.Qty)
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
            //库存充足
            else
            {
                var combination = new List<List<DeliveryTaskInfo>>();

                var queryQties = barcodeNumbers.GroupBy(t => t.Qty).Select(t => new QtyObject { qty = t.Key, qtyCount = t.Count() }).ToList();
                var selectedCounts = new int[queryQties.Count];
                var result = new List<CombinationGroup>();

                Find(queryQties, entry.Qty, 0, 0, selectedCounts, result);

                if (result.Count > 0)
                {
                    var maxItem = result.MaxBy(t => t.Total);

                    foreach (var item in maxItem.Items)
                    {
                        var qty = item.qty;
                        var qtyCount = item.qtyCount;

                        var fiterBarcodeNumbers = barcodeNumbers.Where(t => t.Qty == qty).Take(qtyCount.GetValueOrDefault()).ToList();

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
            Message = $"处理完成: 创建 {createdTasks.Count} 个组合任务",
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
    public void Find(
        List<QtyObject> source,
        decimal target,
        int index,
        decimal currentTotal,
        int[] selectedCounts,
        List<CombinationGroup> result)
    {
        if (index == source.Count)
        {
            if (currentTotal <= 0) return;

            var group = new CombinationGroup
            {
                Total = currentTotal,
                Items = source
                    .Select((item, i) => new { item, count = selectedCounts[i] })
                    .Where(x => x.count > 0)
                    .Select(x => new QtyObject
                    {
                        qty = x.item.qty,
                        qtyCount = x.count
                    })
                    .ToList()
            };

            result.Add(group);
            return;
        }

        var item = source[index];

        for (int count = 0; count <= item.qtyCount; count++)
        {
            decimal newTotal = currentTotal + item.qty.GetValueOrDefault() * count;

            if (newTotal > target)
            {
                break;
            }

            selectedCounts[index] = count;

            Find(source, target, index + 1, newTotal, selectedCounts, result);
        }

        selectedCounts[index] = 0;
    }

    public async Task<List<Queues>> CreatQueues(List<DeliveryTaskInfo> infos)
    {
        var createdTasks = new List<Queues>();

        if (infos != null && infos.Count > 0)
        {
            var loc = _db.Queryable<Location>().Where(t => t.Reserve5!.StartsWith('G') && t.Status == 0 && t.EnableFlag == true).ToList();

            for (var i = 0; i < (infos.Count > loc.Count ? loc.Count : infos.Count); i++)
            {
                var pallNo = GeneratePallNo();

                var queue = new Queues
                {
                    TaskName = infos[i].TaskName,
                    PallNo = pallNo,
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

    private string GeneratePallNo()
    {
        var today = DateTime.Now.ToString("yyyyMMdd");
        var sequence = IncrementSequence(today);
        return $"PALL{today}{sequence:D4}";
    }

    /// <summary>
    /// 原子递增当日序号并返回新值，避免"先查后改"在并发下生成重复的 PallNo。
    /// </summary>
    private int IncrementSequence(string today)
    {
        const string updateSql = """
            UPDATE serialsequence
            SET CurrentSequence = ISNULL(CurrentSequence, 0) + 1
            OUTPUT inserted.CurrentSequence
            WHERE SerialDate = @date
            """;

        var updated = _db.Ado.SqlQuery<int>(updateSql, new SugarParameter("@date", today));
        if (updated.Count > 0)
            return updated[0];

        try
        {
            _db.Insertable(new SerialSequence
            {
                SerialDate = today,
                CurrentSequence = 1
            }).ExecuteCommand();
            return 1;
        }
        catch
        {
            // 当日记录已被并发请求插入，主键冲突，重走递增取号
            var retried = _db.Ado.SqlQuery<int>(updateSql, new SugarParameter("@date", today));
            if (retried.Count > 0)
                return retried[0];
            throw;
        }
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
    //分录单编号
    public int Seq { get; set; }
}
