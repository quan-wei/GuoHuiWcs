import pandas as pd
df = pd.read_excel(r"D:/Desktop/仓位信息-20260805155807.xls")
ct1 = df[df["容器类型"] == 1]
codes = ct1["仓位编号"].tolist()

sql = "-- container type=1 locations, total {} rows\n".format(len(codes))
sql += "-- distribution: 一层货架 61, 二层货架 27, 地面库位 5\n\n"

for code in codes:
    sql += "UPDATE Location SET Status = 1, UpdateTime = GETDATE() WHERE LocationCode = '{}';\n".format(code)

with open(r"E:\GH_Wcs\update_status.sql", "w", encoding="utf-8") as f:
    f.write(sql)

print("Generated {} SQL statements".format(len(codes)))
