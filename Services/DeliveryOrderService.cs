using Guohui_Wcs.Models.Kingdee;
using GuoHui_Data.DaoEntity;
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

            // 通过 Barcode.MaterialNo 找到对应的条码号
            var barcodeNumbers = await _db.Queryable<Barcode>()
                .Where(b => b.MaterialNo == materialCode)
                .Select(b => b.Number)
                .ToListAsync();

            if (barcodeNumbers.Count == 0)
            {
                errors.Add($"分录行 {entry.Seq}: 物料 {materialCode} 未找到对应条码");
                continue;
            }

            // 在 PallMater 的 SubTitle1~SubTitle6 中匹配条码号
            var pallets = await _db.Queryable<PallMater>()
                .Where(p =>
                    barcodeNumbers.Contains(p.SubTitle1!) ||
                    barcodeNumbers.Contains(p.SubTitle2!) ||
                    barcodeNumbers.Contains(p.SubTitle3!) ||
                    barcodeNumbers.Contains(p.SubTitle4!) ||
                    barcodeNumbers.Contains(p.SubTitle5!) ||
                    barcodeNumbers.Contains(p.SubTitle6!))
                .ToListAsync();

            if (pallets.Count == 0)
            {
                errors.Add($"分录行 {entry.Seq}: 物料 {materialCode} 未找到对应托盘");
                continue;
            }

            // 3. 为每个匹配的托盘生成队列任务
            foreach (var pallet in pallets)
            {
                var taskName = $"OUT-{deliveryNo}-{entry.Seq}-{pallet.PallNo}";
                var queue = new Queues
                {
                    TaskName = taskName,
                    PallNo = pallet.PallNo,
                    Type = "出库",
                    GetLocation = pallet.LocationCode ?? "",
                    PutLocation = "",
                    Status = "0",
                    CreateTime = DateTime.Now
                };

                await _db.Insertable(queue).ExecuteCommandAsync();

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

        return new DeliveryProcessResult
        {
            Success = true,
            Message = $"处理完成: 创建 {createdTasks.Count} 个任务",
            DeliveryNo = deliveryNo,
            Tasks = createdTasks,
            Errors = errors
        };
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
