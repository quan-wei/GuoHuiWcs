using Microsoft.Extensions.Configuration;
using SqlSugar;

namespace GuoHui_Data.DaoEntity
{
    public class Model_Data
    {
        private static SqlSugarScope? _db;
        private static readonly object _dbLock = new();

        public static SqlSugarScope Db
        {
            get
            {
                if (_db == null)
                {
                    lock (_dbLock)
                    {
                        _db ??= CreateDb();
                    }
                }
                return _db;
            }
        }

        private static string GetConnectionString()
        {
            var config = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: true)
                .Build();
            return config.GetConnectionString("Default")
                ?? "Server=tcp:191.167.10.66;Database=GuoHui_Wcs;User Id=sa;Password=telenadmin99.;TrustServerCertificate=True;Connection Timeout=30;"  ;
        }

        private static SqlSugarScope CreateDb()
        {
            return new SqlSugarScope(new ConnectionConfig()
            {
                ConnectionString = GetConnectionString(),
                DbType = DbType.SqlServer,
                IsAutoCloseConnection = true
            }, it => {
                it.Aop.OnLogExecuting = (sql, para) =>
                {
                    Console.WriteLine("==== SQL ====");
                    Console.WriteLine(sql);
                    Console.WriteLine("==== Params ====");
                    if (para != null)
                    {
                        foreach (var p in para)
                        {
                            Console.WriteLine($"{p.ParameterName} = {p.Value}");
                        }
                    }
                };
            });
        }
    }
}