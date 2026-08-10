using GuoHui_Data.DaoEntity;
using Models;
using System.Xml;

namespace Guohui_Test
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
            LoadConfigToUI();
        }

        /// <summary>从 App.config 加载配置到 UI</summary>
        private void LoadConfigToUI()
        {
            try
            {
                var configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Guohui_Test.dll.config");
                if (!File.Exists(configPath))
                {
                    configPath = Path.Combine("E:\\GH_Wcs", "Guohui_Test", "App.config");
                }

                if (File.Exists(configPath))
                {
                    var doc = new XmlDocument();
                    doc.Load(configPath);
                    var nodes = doc.SelectNodes("//configuration/appSettings/add");
                    if (nodes != null)
                    {
                        foreach (XmlNode node in nodes)
                        {
                            var key = node.Attributes?["key"]?.Value;
                            var val = node.Attributes?["value"]?.Value;
                            switch (key)
                            {
                                case "X-KDApi-ServerUrl": txtServerUrl.Text = val ?? ""; break;
                                case "X-KDApi-AcctID": txtAcctId.Text = val ?? ""; break;
                                case "X-KDApi-UserName": txtUsername.Text = val ?? ""; break;
                                case "X-KDApi-AppID": txtAppId.Text = val ?? ""; break;
                                case "X-KDApi-AppSec": txtAppSec.Text = val ?? ""; break;
                            }
                        }
                    }
                    txtResult.Text = $"已加载配置: {configPath}\r\n";
                }
            }
            catch (Exception ex)
            {
                txtResult.Text = $"加载配置失败: {ex.Message}";
            }
        }

        /// <summary>保存 UI 配置到 App.config</summary>
        private void SaveConfigFromUI()
        {
            try
            {
                var configPath = Path.Combine("E:\\GH_Wcs", "Guohui_Test", "App.config");
                var doc = new XmlDocument();
                doc.Load(configPath);
                var nodes = doc.SelectNodes("//configuration/appSettings/add");
                if (nodes != null)
                {
                    foreach (XmlNode node in nodes)
                    {
                        var key = node.Attributes?["key"]?.Value;
                        switch (key)
                        {
                            case "X-KDApi-ServerUrl": node.Attributes!["value"].Value = txtServerUrl.Text; break;
                            case "X-KDApi-AcctID": node.Attributes!["value"].Value = txtAcctId.Text; break;
                            case "X-KDApi-UserName": node.Attributes!["value"].Value = txtUsername.Text; break;
                            case "X-KDApi-AppID": node.Attributes!["value"].Value = txtAppId.Text; break;
                            case "X-KDApi-AppSec": node.Attributes!["value"].Value = txtAppSec.Text; break;
                        }
                    }
                }
                doc.Save(configPath);
            }
            catch (Exception ex)
            {
                txtResult.Text = $"保存配置失败: {ex.Message}";
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            //Model_Data.Db.DbFirst.IsCreateAttribute().StringNullable().CreateClassFile("E:\\GH_Wcs\\GuoHui_Data\\Dao\\", "Models");
            //Model_Data.Db.CodeFirst.InitTables(typeof(Barcode));//这样一个表就能成功创建了
            //Model_Data.Db.CodeFirst.InitTables(typeof(Location));//这样一个表就能成功创建了
            //Model_Data.Db.CodeFirst.InitTables(typeof(PallMater));//这样一个表就能成功创建了
            //Model_Data.Db.CodeFirst.InitTables(typeof(PallMater));//这样一个表就能成功创建了
            //Model_Data.Db.CodeFirst.InitTables(typeof(queues));//这样一个表就能成功创建了
            Model_Data.Db.CodeFirst.InitTables(typeof(serialsequence));//这样一个表就能成功创建了
            MessageBox.Show("ORM代码已生成！");
        }

        /// <summary>测试金蝶登录（SDK 方式）</summary>
        private void btnTestLogin_Click(object sender, EventArgs e)
        {
            SaveConfigFromUI();
            txtResult.Text = "正在测试登录...\r\n";
            btnTestLogin.Enabled = false;
            Application.DoEvents();

            try
            {
                var svc = new KingdeeSdkService();
                var msg = svc.TestLogin();
                txtResult.Text = $"{msg}\r\n\r\nSDK 版本: Kingdee.CDP.WebApi.SDK";
            }
            catch (Exception ex)
            {
                txtResult.Text = $"异常: {ex.Message}\r\n\r\n{ex.StackTrace}";
            }
            finally
            {
                btnTestLogin.Enabled = true;
            }
        }

        /// <summary>测试查询物料（SDK 方式）</summary>
        private void btnTestQuery_Click(object sender, EventArgs e)
        {
            SaveConfigFromUI();
            txtResult.Text = "正在测试查询...\r\n";
            btnTestQuery.Enabled = false;
            Application.DoEvents();

            try
            {
                var svc = new KingdeeSdkService();
                var result = svc.ExecuteBillQuery(
                    "BD_MATERIAL",
                    "FMaterialId,FNumber,FName,FSpecification",
                    "",
                    10);

                txtResult.Text = result;
            }
            catch (Exception ex)
            {
                txtResult.Text = $"异常: {ex.Message}\r\n\r\n{ex.StackTrace}";
            }
            finally
            {
                btnTestQuery.Enabled = true;
            }
        }
    }
}
