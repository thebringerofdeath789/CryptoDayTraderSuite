namespace CryptoDayTraderSuite.UI
{
    partial class AccountsForm
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
            this.accountsControl = new CryptoDayTraderSuite.UI.AccountsControl();
            this.SuspendLayout();
            // 
            // accountsControl
            // 
            this.accountsControl.Dock = System.Windows.Forms.DockStyle.Fill;
            this.accountsControl.Location = new System.Drawing.Point(0, 0);
            this.accountsControl.Name = "accountsControl";
            this.accountsControl.Size = new System.Drawing.Size(584, 361);
            this.accountsControl.TabIndex = 0;
            // 
            // AccountsForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(584, 361);
            this.Controls.Add(this.accountsControl);
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "AccountsForm";
            this.ShowIcon = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Account Management";
            this.ResumeLayout(false);

        }

        #endregion

        private AccountsControl accountsControl;
    }
}