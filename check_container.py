import pandas as pd
df = pd.read_excel(r"D:/Desktop/仓位信息-20260805155807.xls")

print("=== container type distribution ===")
print(df["容器类型"].value_counts(dropna=False).sort_index())
print()

ct1 = df[df["容器类型"] == 1]
print(f"Rows with container type=1: {len(ct1)}")
print()

print("=== By location type ===")
print(ct1["仓位类型"].value_counts())
print()

print("=== Full list ===")
print(ct1[["仓位编号","仓位类型","容器类型"]].to_string())
