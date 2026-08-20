using GuoHui_Data.DaoEntity;
using Models;

namespace Guohui_Test
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            //Model_Data.Db.DbFirst.IsCreateAttribute().StringNullable().CreateClassFile("E:\\GH_Wcs\\GuoHui_Data\\Dao\\", "Models");
            //Model_Data.Db.CodeFirst.InitTables(typeof(Barcode));//这样一张表就能成功创建了
            //Model_Data.Db.CodeFirst.InitTables(typeof(Location));//这样一张表就能成功创建了
            //Model_Data.Db.CodeFirst.InitTables(typeof(PallMater));//这样一张表就能成功创建了
            //Model_Data.Db.CodeFirst.InitTables(typeof(PallMater));//这样一张表就能成功创建了
            //Model_Data.Db.CodeFirst.InitTables(typeof(queues));//这样一张表就能成功创建了
            Model_Data.Db.CodeFirst.InitTables(typeof(serialsequence));//这样一张表就能成功创建了
            MessageBox.Show("ORM代码已生成！");
        }
    }
}
