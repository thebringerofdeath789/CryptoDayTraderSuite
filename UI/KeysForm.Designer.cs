namespace CryptoDayTraderSuite.UI
{
    partial class KeysForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.keysControl1 = new CryptoDayTraderSuite.UI.KeysControl();
            this.SuspendLayout();
            // 
            // keysControl1
            // 
            this.keysControl1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.keysControl1.Location = new System.Drawing.Point(0, 0);
            this.keysControl1.Name = "keysControl1";
            this.keysControl1.Size = new System.Drawing.Size(800, 450);
            this.keysControl1.TabIndex = 0;
            // 
            // KeysForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(800, 450);
            this.Controls.Add(this.keysControl1);
            this.Name = "KeysForm";
            this.Text = "API Keys";
            this.ResumeLayout(false);

        }

        #endregion

        private CryptoDayTraderSuite.UI.KeysControl keysControl1;
    }
}