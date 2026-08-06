namespace Guohui_Test
{
    partial class Form1
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            button1 = new Button();
            txtServerUrl = new TextBox();
            txtAcctId = new TextBox();
            txtUsername = new TextBox();
            txtAppId = new TextBox();
            txtAppSec = new TextBox();
            btnTestLogin = new Button();
            btnTestQuery = new Button();
            txtResult = new TextBox();
            label1 = new Label();
            label2 = new Label();
            label3 = new Label();
            label4 = new Label();
            label5 = new Label();
            SuspendLayout();
            //
            // button1
            //
            button1.Location = new Point(12, 12);
            button1.Name = "button1";
            button1.Size = new Size(120, 30);
            button1.TabIndex = 0;
            button1.Text = "生成ORM代码";
            button1.UseVisualStyleBackColor = true;
            button1.Click += button1_Click;
            //
            // txtServerUrl
            //
            txtServerUrl.Location = new Point(120, 60);
            txtServerUrl.Name = "txtServerUrl";
            txtServerUrl.Size = new Size(350, 23);
            txtServerUrl.TabIndex = 1;
            txtServerUrl.Text = "http://your-server/k3cloud/";
            //
            // txtAcctId
            //
            txtAcctId.Location = new Point(120, 90);
            txtAcctId.Name = "txtAcctId";
            txtAcctId.Size = new Size(350, 23);
            txtAcctId.TabIndex = 2;
            //
            // txtUsername
            //
            txtUsername.Location = new Point(120, 120);
            txtUsername.Name = "txtUsername";
            txtUsername.Size = new Size(350, 23);
            txtUsername.TabIndex = 3;
            //
            // txtAppId
            //
            txtAppId.Location = new Point(120, 150);
            txtAppId.Name = "txtAppId";
            txtAppId.Size = new Size(350, 23);
            txtAppId.TabIndex = 4;
            //
            // txtAppSec
            //
            txtAppSec.Location = new Point(120, 180);
            txtAppSec.Name = "txtAppSec";
            txtAppSec.Size = new Size(350, 23);
            txtAppSec.TabIndex = 5;
            txtAppSec.UseSystemPasswordChar = true;
            //
            // btnTestLogin
            //
            btnTestLogin.Location = new Point(490, 60);
            btnTestLogin.Name = "btnTestLogin";
            btnTestLogin.Size = new Size(120, 30);
            btnTestLogin.TabIndex = 6;
            btnTestLogin.Text = "测试登录";
            btnTestLogin.UseVisualStyleBackColor = true;
            btnTestLogin.Click += btnTestLogin_Click;
            //
            // btnTestQuery
            //
            btnTestQuery.Location = new Point(490, 100);
            btnTestQuery.Name = "btnTestQuery";
            btnTestQuery.Size = new Size(120, 30);
            btnTestQuery.TabIndex = 7;
            btnTestQuery.Text = "测试查询物料";
            btnTestQuery.UseVisualStyleBackColor = true;
            btnTestQuery.Click += btnTestQuery_Click;
            //
            // txtResult
            //
            txtResult.Location = new Point(12, 220);
            txtResult.Multiline = true;
            txtResult.Name = "txtResult";
            txtResult.ScrollBars = ScrollBars.Vertical;
            txtResult.Size = new Size(760, 210);
            txtResult.TabIndex = 8;
            txtResult.Font = new Font("Consolas", 10);
            //
            // label1
            //
            label1.AutoSize = true;
            label1.Location = new Point(12, 63);
            label1.Name = "label1";
            label1.Size = new Size(84, 17);
            label1.TabIndex = 9;
            label1.Text = "ServerUrl:";
            //
            // label2
            //
            label2.AutoSize = true;
            label2.Location = new Point(12, 93);
            label2.Name = "label2";
            label2.Size = new Size(55, 17);
            label2.TabIndex = 10;
            label2.Text = "AcctID:";
            //
            // label3
            //
            label3.AutoSize = true;
            label3.Location = new Point(12, 123);
            label3.Name = "label3";
            label3.Size = new Size(73, 17);
            label3.TabIndex = 11;
            label3.Text = "Username:";
            //
            // label4
            //
            label4.AutoSize = true;
            label4.Location = new Point(12, 153);
            label4.Name = "label4";
            label4.Size = new Size(46, 17);
            label4.TabIndex = 12;
            label4.Text = "AppID:";
            //
            // label5
            //
            label5.AutoSize = true;
            label5.Location = new Point(12, 183);
            label5.Name = "label5";
            label5.Size = new Size(70, 17);
            label5.TabIndex = 13;
            label5.Text = "AppSecret:";
            //
            // Form1
            //
            AutoScaleDimensions = new SizeF(7F, 17F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(784, 441);
            Controls.Add(label5);
            Controls.Add(label4);
            Controls.Add(label3);
            Controls.Add(label2);
            Controls.Add(label1);
            Controls.Add(txtResult);
            Controls.Add(btnTestQuery);
            Controls.Add(btnTestLogin);
            Controls.Add(txtAppSec);
            Controls.Add(txtAppId);
            Controls.Add(txtUsername);
            Controls.Add(txtAcctId);
            Controls.Add(txtServerUrl);
            Controls.Add(button1);
            Name = "Form1";
            Text = "国慧WCS - 金蝶接口测试";
            ResumeLayout(false);
            PerformLayout();
        }

        private Button button1;
        private TextBox txtServerUrl;
        private TextBox txtAcctId;
        private TextBox txtUsername;
        private TextBox txtAppId;
        private TextBox txtAppSec;
        private Button btnTestLogin;
        private Button btnTestQuery;
        private TextBox txtResult;
        private Label label1;
        private Label label2;
        private Label label3;
        private Label label4;
        private Label label5;
    }
}
