import pandas as pd
df = pd.read_excel(r"E:/wechat/xwechat_files/wxid_u88mntunhta322_bb83/msg/file/2026-08/data.xls")
lines = []
for _, row in df.iterrows():
    loc = str(row["仓位编号"]).strip()
    mapping = str(row["映射编号"]).strip()
    lines.append(f"UPDATE Location SET Reserve5 = '{mapping}', UpdateTime = GETDATE() WHERE LocationCode = '{loc}';")
with open(r"E:\GH_Wcs\update_mapping.sql", "w", encoding="utf-8") as f:
    f.write("\n".join(lines))
print(f"Generated {len(lines)} SQL statements")
