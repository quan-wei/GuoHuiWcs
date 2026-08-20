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
            txtResult = new TextBox();
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
            // txtResult
            //
            txtResult.Location = new Point(12, 60);
            txtResult.Multiline = true;
            txtResult.Name = "txtResult";
            txtResult.ScrollBars = ScrollBars.Vertical;
            txtResult.Size = new Size(760, 370);
            txtResult.TabIndex = 8;
            txtResult.Font = new Font("Consolas", 10);
            //
            // Form1
            //
            AutoScaleDimensions = new SizeF(7F, 17F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(784, 441);
            Controls.Add(txtResult);
            Controls.Add(button1);
            Name = "Form1";
            Text = "国慧WCS";
            ResumeLayout(false);
            PerformLayout();
        }

        private Button button1;
        private TextBox txtResult;
    }
}
