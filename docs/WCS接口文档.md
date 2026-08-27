<!--
  版本: 1.0
  更新日期: 2026-08-13
  说明: 国汇 WCS 系统全部接口文档，含库位管理、AGV 回调、PDA 及金蝶对接
-->
# 国汇 WCS 接口文档

## 基础信息

| 项目 | 说明 |
|------|------|
| 默认 Base URL | 部署后按实际环境配置 |
| Content-Type | `application/json` |
| 响应格式 | 统一包装 `{ Success, Message, ... }` |
| 托盘号 | 系统自动生成，格式 `PALL` + 日期 + 4位序号（如 `PALL202608060001`） |

---

## 一、库位管理接口

**Base Path:** `/api/Location`

### 1.1 自动分配库位

系统自动查找空闲库位并分配，优先一层货架，不满足则向上层货架（二层→三层→四层）查找。分配成功后自动创建 AGV 搬运任务，AGV 失败则回滚。

```
POST /api/Location/allocate
```

**请求体**

| 字段 | 类型 | 必填 | 说明 |
|------|------|------|------|
| `MaterNo` | `string[]` | 是 | 物料条码列表，用于从 WMS 同步物料重量 |
| `StartPoint` | `string` | 是 | 起点（货架编号），AGV 搬运起点 |
| `EndPoint` | `string` | 否 | 终点，通常留空由系统自动分配 |
| `TaskType` | `string` | 否 | 任务类型标识 |
| `AllowUpperLevels` | `bool` | 否 | 是否允许上层货架，默认 `true` |

**请求示例**

```json
{
    "MaterNo": ["BARCODE001", "BARCODE002"],
    "StartPoint": "G2",
    "EndPoint": "",
    "TaskType": "入库",
    "AllowUpperLevels": true
}
```

**成功响应** `200`

```json
{
    "Success": true,
    "LocationCode": "A01-01-01",
    "LocationType": "一层货架",
    "WeightKg": 150.5,
    "PallNo": "PALL202608060001",
    "Message": "分配成功"
}
```

**失败响应** `400`

```json
{
    "Success": false,
    "Message": "无可用库位：一层已满，且上层库位均不满足重量限制"
}
```

**处理流程**

1. 校验 `StartPoint` 不为空
2. 生成托盘号（`PALL` + 日期 + 4位序号）
3. 遍历 `MaterNo`，逐条调用 WMS `wms_bardossier.select` 同步物料信息
4. 按「一层→二层→三层→四层」顺序查找空闲库位，上层须通过货架组重量校验（≤ 2500kg）
5. 分配库位并插入 `PallMater` 记录
6. 调用 AGV 创建搬运任务（起点→分配库位）
7. AGV 失败则自动回滚：释放库位 + 删除 PallMater

---

### 1.2 分配到指定库位

将托盘分配到调用方指定的库位，不走自动查找逻辑，直接逐项校验目标库位是否可用。

```
POST /api/Location/allocate/{locationCode}
```

**路径参数**

| 参数 | 类型 | 必填 | 说明 |
|------|------|------|------|
| `locationCode` | `string` | 是 | 目标库位编码，如 `A01-02-03` |

**请求体**（同 1.1 自动分配）

**校验规则**

| 校验项 | 失败返回 |
|------|------|
| 库位存在 | 终点库位不存在 |
| 库位空闲（Status=0 且 PallNo 为空） | 终点库位已被占用 |
| 库位已启用 | 终点库位已禁用 |
| 货架组重量 ≤ 2500kg | 所在货架对已超重限制 |

**特殊处理：G 开头库位**

当 `locationCode` 以 `G` 开头时，视为出库操作，不校验物料号，物料号可以为空，重量为 0。

---

### 1.3 释放库位

将指定库位置为空闲状态。

```
POST /api/Location/release/{locationCode}
```

| 参数 | 位置 | 类型 | 必填 | 说明 |
|------|------|------|------|------|
| `locationCode` | Path | `string` | 是 | 要释放的库位编码 |

**成功响应** `200`

```json
{ "Success": true, "Message": "库位已释放" }
```

---

### 1.4 锁定库位

将指定库位标记为禁用并锁定（Status=1，EnableFlag=false）。

```
POST /api/Location/lock/{locationCode}
```

| 参数 | 位置 | 类型 | 必填 | 说明 |
|------|------|------|------|------|
| `locationCode` | Path | `string` | 是 | 要锁定的库位编码 |

**成功响应** `200`

```json
{ "Success": true, "Message": "库位已锁定" }
```

---

### 1.5 查询货架组负载

查询指定货架所在组的当前重量负载。

```
GET /api/Location/group-load/{shelfCode}
```

| 参数 | 位置 | 类型 | 必填 | 说明 |
|------|------|------|------|------|
| `shelfCode` | Path | `string` | 是 | 货架编号 |

**成功响应** `200`

```json
{
    "GroupShelfs": ["1", "2"],
    "CurrentWeightKg": 320.0,
    "LimitWeightKg": 2500.0,
    "RemainingWeightKg": 2180.0,
    "TierLoads": {
        "二层货架": 120.0,
        "三层货架": 200.0,
        "四层货架": 0.0
    }
}
```

---

### 1.6 条码查询库位

根据条码号查询对应库位及托盘产品明细。

```
GET /api/Location/query-by-barcode/{barcode}
```

| 参数 | 位置 | 类型 | 必填 | 说明 |
|------|------|------|------|------|
| `barcode` | Path | `string` | 是 | 条码号（对应 `Location.Reserve5`） |

**成功响应** `200`

```json
{
    "Success": true,
    "LocationCode": "A01-01-01",
    "LocationType": "一层货架",
    "ShelfCode": "1",
    "Status": 1,
    "StatusText": "有货",
    "PallNo": "PALL202608060001",
    "TotalWeight": 150.5,
    "LimitWeight": 2500.0,
    "Reserve5": "BARCODE001",
    "EnableFlag": true,
    "Products": [
        {
            "Barcode": "BARCODE001",
            "Weight": 150.5,
            "MaterialNo": "MAT-001",
            "MaterialName": "物料A",
            "MaterialModel": "规格X",
            "Qty": 100.0
        }
    ]
}
```

---

### 1.7 按物料编码查询托盘

根据物料编码前缀查询视图 `dbo.querybyno`，返回匹配的托盘、库位、条码及物料明细。

```
GET /api/Location/query-by-material/{code}
```

| 参数 | 位置 | 类型 | 必填 | 说明 |
|------|------|------|------|------|
| `code` | Path | `string` | 是 | 物料编码前缀，如 `F0.03.00198` |

**成功响应** `200`

```json
{
    "Success": true,
    "Code": "F0.03.00198",
    "Count": 1,
    "Data": [
        {
            "LocationCode": "A01-01-01",
            "Reserve5": "F0.03.00198",
            "LocationType": "一层货架",
            "LimitWeightt": 2500.0,
            "TotalWeight": 150.5,
            "PallNo": "PALL202608060001",
            "PallWeight": 150.5,
            "BarcodeNumber": "F0.03.00198-001",
            "BarType": "01",
            "Qty": 1.0,
            "AuxQty": 150.5,
            "WarehouseName": "原料仓",
            "MaterialNo": "MAT-001",
            "MaterialName": "物料A",
            "SubTitleIndex": 1,
            "SubTitleValue": "F0.03.00198-001",
            "CorrespondingWeight": 150.5
        }
    ]
}
```

**失败响应** `400`

```json
{
    "Success": false,
    "Message": "编码不能为空"
}
```

---

### 1.8 查询金蝶出库通知单 &ensp; `[开发中]`

通过金蝶 View 接口查询出库通知单的详细信息。

```
GET /api/Location/query-delivery/{number}
```

| 参数 | 位置 | 类型 | 必填 | 说明 |
|------|------|------|------|------|
| `number` | Path | `string` | 是 | 金蝶出库通知单单据号（BillNo） |

**成功响应** `200`

```json
{
    "Success": true,
    "BillNo": "SALOUT-2026-001",
    "DocumentStatus": "C",
    "Date": "2026-08-13",
    "Customer": "客户名称",
    "Note": "备注",
    "Entries": [
        {
            "Seq": 1,
            "MaterialCode": "MAT-001",
            "MaterialName": "物料A",
            "Qty": 100.0,
            "Unit": "个",
            "Warehouse": "原料仓",
            "Lot": "20260801"
        }
    ]
}
```

**依赖**

- 金蝶 `Kingdee.BOS.WebApi.ServicesStub.DynamicFormService.View.common.kdsvc` 接口
- 表单标识：`SAL_DELIVERYNOTICE`
- 需要 `KingdeeApiService` 已成功登录金蝶系统

---

### 1.9 处理出库通知单 &ensp; `[开发中]`

根据出库通知单号，从金蝶获取单据 → 匹配托盘 → 生成出库队列任务。

```
POST /api/Location/process-delivery/{number}
```

| 参数 | 位置 | 类型 | 必填 | 说明 |
|------|------|------|------|------|
| `number` | Path | `string` | 是 | 金蝶出库通知单单据号 |

**成功响应** `200`

```json
{
    "Success": true,
    "Message": "处理完成: 创建 2 个任务",
    "DeliveryNo": "SALOUT-2026-001",
    "Tasks": [
        {
            "TaskName": "OUT-SALOUT-2026-001-1-PALL202608060001",
            "PallNo": "PALL202608060001",
            "MaterialCode": "MAT-001",
            "LocationCode": "A01-01-01",
            "Seq": 1
        }
    ],
    "Errors": []
}
```

**处理流程**

1. 调用金蝶 View 接口获取出库通知单
2. 遍历分录行，通过 `Barcode.MaterialNo` 匹配物料编码
3. 在 `PallMater` 的 `SubTitle1~SubTitle6` 中查找匹配的托盘
4. 为每个匹配的托盘生成 `queues` 出库任务（Type=出库，Status=0）

---

## 二、AGV 回调接口

**Base Path:** `/agv/agvCallbackService`

### 2.1 AGV 任务状态回调

接收 AGV 系统推送的机器人任务状态通知。

```
POST /agv/agvCallbackService/agvCallback
```

**请求体**

| 字段 | 类型 | 必填 | 说明 |
|------|------|------|------|
| `reqCode` | `string` | 否 | 请求编码 |
| `method` | `string` | 否 | 方法名 |
| `taskCode` | `string` | 否 | 任务编码 |
| `wbCode` | `string` | 否 | 工位编码 |
| `podCode` | `string` | 否 | 货架编码 |

**请求示例**

```json
{
    "reqCode": "REQ001",
    "method": "taskStatus",
    "taskCode": "TASK-20260813-001",
    "wbCode": "WB01",
    "podCode": "POD01"
}
```

**成功响应** `200`

```json
{ "code": "0", "message": "0", "reqCode": "" }
```

**失败响应** `200`

```json
{ "code": "1", "message": "Invalid JSON data", "reqCode": "" }
```



## 四、金蝶接口汇总

| 接口 | 方法 | 路径 | 状态 |
|------|------|------|------|
| 查询出库通知单 | `GET` | `/api/Location/query-delivery/{number}` | `[开发中]` |
| 处理出库通知单 | `POST` | `/api/Location/process-delivery/{number}` | `[开发中]` |

两个金蝶接口均依赖 `KingdeeApiService` 通过 `Kingdee.BOS.WebApi.ServicesStub.DynamicFormService.View.common.kdsvc` 访问金蝶云星空，需要正确的登录配置（`Kingdee:BaseUrl`、`Kingdee:AcctId`、`Kingdee:UserName`、`Kingdee:Password`）。

**金蝶通用响应模型 `KingdeeViewResponse<T>`：**

```json
{
    "Result": {
        "ResponseStatus": { "IsSuccess": true },
        "Result": { /* T 类型数据 */ }
    }
}
```

---

## 五、第三方依赖

| 系统 | 地址 | 用途 |
|------|------|------|
| WMS | `http://191.167.10.102:8081` | 条码查询（`wms_bardossier.select`），物料重量同步 |
| 金蝶云星空 | `http://191.167.10.102:2126/K3Cloud/` | 出库通知单查询（`SAL_DELIVERYNOTICE`） |
| AGV | `191.167.10.5:8181` | 创建搬运任务，接收状态回调 |

---

## 六、通用说明

- 分配成功后自动创建 AGV 搬运任务（起点→库位），AGV 失败则自动回滚库位分配并删除托盘记录
- 托盘号由系统自动生成，调用方无需传入
- 上层货架（二/三/四层）以货架组为单位校验重量，每组上限 2500kg
- 货架组规则：相邻两个货架编号为一组（奇数 N 与 N+1）
- 地面库位仅用于中转，不参与存储分配
- G 开头库位为出库中转位，重量校验为 0
