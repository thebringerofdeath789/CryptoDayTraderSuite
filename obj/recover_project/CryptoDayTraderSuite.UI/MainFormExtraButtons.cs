using System.Windows.Forms;

namespace CryptoDayTraderSuite.UI
{
	public static class MainFormExtraButtons
	{
		public static void Add(MainForm f)
		{
			try
			{
				Button btnAcc = new Button();
				btnAcc.Text = "Accounts";
				btnAcc.Width = 90;
				btnAcc.Height = 26;
				btnAcc.Top = 12;
				btnAcc.Left = f.ClientSize.Width - btnAcc.Width - 12;
				btnAcc.Anchor = AnchorStyles.Top | AnchorStyles.Right;
				f.Controls.Add(btnAcc);
				Button btnKeys = new Button();
				btnKeys.Text = "Keys";
				btnKeys.Width = 70;
				btnKeys.Height = 26;
				btnKeys.Top = 44;
				btnKeys.Left = f.ClientSize.Width - btnKeys.Width - 12;
				btnKeys.Anchor = AnchorStyles.Top | AnchorStyles.Right;
				f.Controls.Add(btnKeys);
				Button btnAuto = new Button();
				btnAuto.Text = "Auto";
				btnAuto.Width = 70;
				btnAuto.Height = 26;
				btnAuto.Top = 76;
				btnAuto.Left = f.ClientSize.Width - btnAuto.Width - 12;
				btnAuto.Anchor = AnchorStyles.Top | AnchorStyles.Right;
				f.Controls.Add(btnAuto);
				btnAcc.Click += delegate
				{
					using (AccountsForm accountsForm = new AccountsForm())
					{
						accountsForm.ShowDialog(f);
					}
				};
				btnKeys.Click += delegate
				{
					using (KeysForm keysForm = new KeysForm())
					{
						keysForm.ShowDialog(f);
					}
				};
				btnAuto.Click += delegate
				{
					if (f.AutoPlanner == null || f.ExchangeProvider == null || f.KeyService == null || f.AccountService == null)
					{
						MessageBox.Show(f, "Auto mode dependencies are not initialized.", "Auto Mode", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
						return;
					}
					using (AutoModeForm autoModeForm = new AutoModeForm(f.AutoPlanner, f.ExchangeProvider.CreatePublicClient("Coinbase"), f.KeyService, f.AccountService))
					{
						autoModeForm.ShowDialog(f);
					}
				};
				f.Resize += delegate
				{
					btnAcc.Left = f.ClientSize.Width - btnAcc.Width - 12;
					btnKeys.Left = f.ClientSize.Width - btnKeys.Width - 12;
					btnAuto.Left = f.ClientSize.Width - btnAuto.Width - 12;
				};
			}
			catch
			{
			}
		}
	}
}
