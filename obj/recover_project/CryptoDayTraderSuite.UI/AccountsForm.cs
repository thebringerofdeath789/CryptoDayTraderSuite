using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using CryptoDayTraderSuite.Services;

namespace CryptoDayTraderSuite.UI
{
	public class AccountsForm : Form
	{
		private IContainer components = null;

		private AccountsControl accountsControl;

		public AccountsForm()
			: this(new AccountService())
		{
		}

		public AccountsForm(IAccountService service)
		{
			InitializeComponent();
			accountsControl.Initialize(service);
		}

		protected override void Dispose(bool disposing)
		{
			if (disposing && components != null)
			{
				components.Dispose();
			}
			base.Dispose(disposing);
		}

		private void InitializeComponent()
		{
			this.accountsControl = new CryptoDayTraderSuite.UI.AccountsControl();
			base.SuspendLayout();
			this.accountsControl.Dock = System.Windows.Forms.DockStyle.Fill;
			this.accountsControl.Location = new System.Drawing.Point(0, 0);
			this.accountsControl.Name = "accountsControl";
			this.accountsControl.Size = new System.Drawing.Size(584, 361);
			this.accountsControl.TabIndex = 0;
			base.AutoScaleDimensions = new System.Drawing.SizeF(6f, 13f);
			base.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			base.ClientSize = new System.Drawing.Size(584, 361);
			base.Controls.Add(this.accountsControl);
			base.MaximizeBox = false;
			base.MinimizeBox = false;
			base.Name = "AccountsForm";
			base.ShowIcon = false;
			base.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
			this.Text = "Account Management";
			base.ResumeLayout(false);
		}
	}
}
