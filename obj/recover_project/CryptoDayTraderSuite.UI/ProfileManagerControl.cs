using System.Drawing;
using System.Windows.Forms;
using CryptoDayTraderSuite.Services;
using CryptoDayTraderSuite.Themes;

namespace CryptoDayTraderSuite.UI
{
	public class ProfileManagerControl : UserControl
	{
		private TabControl _tabs;

		public ProfileManagerControl()
		{
			InitializeComponent();
			Theme.Apply(this);
		}

		public void Initialize(IKeyService keyService, IAccountService accountService, IProfileService profileService)
		{
			_tabs = new TabControl
			{
				Dock = DockStyle.Fill
			};
			KeysControl kc = new KeysControl();
			kc.Initialize(keyService);
			kc.Dock = DockStyle.Fill;
			TabPage tpKeys = new TabPage("API Keys");
			tpKeys.Controls.Add(kc);
			_tabs.TabPages.Add(tpKeys);
			AccountsControl ac = new AccountsControl();
			ac.Initialize(accountService);
			ac.Dock = DockStyle.Fill;
			TabPage tpAcc = new TabPage("Accounts");
			tpAcc.Controls.Add(ac);
			_tabs.TabPages.Add(tpAcc);
			ProfilesControl pc = new ProfilesControl();
			pc.Initialize(profileService);
			pc.Dock = DockStyle.Fill;
			TabPage tpProf = new TabPage("Data Management");
			tpProf.Controls.Add(pc);
			_tabs.TabPages.Add(tpProf);
			base.Controls.Add(_tabs);
			_tabs.BackColor = Theme.ContentBg;
			_tabs.ForeColor = Theme.Text;
			foreach (TabPage page in _tabs.TabPages)
			{
				page.BackColor = Theme.ContentBg;
				page.ForeColor = Theme.Text;
			}
		}

		private void InitializeComponent()
		{
			base.SuspendLayout();
			base.Name = "ProfileManagerControl";
			base.Size = new System.Drawing.Size(800, 600);
			base.ResumeLayout(false);
		}
	}
}
