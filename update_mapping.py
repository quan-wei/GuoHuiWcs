import pandas as pd
import pyodbc

df = pd.read_excel(r'E:/wechat/xwechat_files/wxid_u88mntunhta322_bb83/msg/file/2026-08/data.xls')
print(f'Excel rows: {len(df)}')

conn = pyodbc.connect(
    'DRIVER={ODBC Driver 17 for SQL Server};'
    'SERVER=192.168.1.73;'
    'DATABASE=GuoHui_Wcs;'
    'UID=tzhuser;'
    'PWD=tzhuser;'
    'Encrypt=no;'
)
cursor = conn.cursor()

updated = 0
for _, row in df.iterrows():
    loc_code = str(row['仓位编号']).strip()
    mapping = str(row['映射编号']).strip()
    cursor.execute(
        'UPDATE Location SET Reserver5 = ?, UpdateTime = GETDATE() WHERE LocationCode = ?',
        (mapping, loc_code)
    )
    updated += cursor.rowcount

conn.commit()
cursor.close()
conn.close()

print(f'Updated {updated} rows')

conn2 = pyodbc.connect(
    'DRIVER={ODBC Driver 17 for SQL Server};'
    'SERVER=192.168.1.73;'
    'DATABASE=GuoHui_Wcs;'
    'UID=tzhuser;'
    'PWD=tzhuser;'
    'Encrypt=no;'
)
cursor2 = conn2.cursor()
cursor2.execute("SELECT COUNT(*) FROM Location WHERE Reserver5 IS NOT NULL AND Reserver5 != ''")
total = cursor2.fetchone()[0]
print(f'Total rows with Reserver5: {total}')

cursor2.execute('SELECT TOP 10 LocationCode, ShelfCode, LocationType, Reserver5 FROM Location ORDER BY Id')
for row in cursor2.fetchall():
    print(f'{row.LocationCode:20s} {row.ShelfCode:10s} {row.LocationType:8s} -> {row.Reserver5}')

cursor2.close()
conn2.close()
