using System.Windows.Forms;
using CryptoDayTraderSuite.Services;
using CryptoDayTraderSuite.Themes;

namespace CryptoDayTraderSuite.UI
{
    public partial class ProfileManagerControl : UserControl
    {
        private TabControl _tabs;

        public ProfileManagerControl()
        {
            InitializeComponent();
            Theme.Apply(this);
        }

        public void Initialize(IKeyService keyService, IAccountService accountService, IProfileService profileService)
        {
            _tabs = new TabControl { Dock = DockStyle.Fill };
            
            // Tab 1: API Keys
            var kc = new KeysControl();
            kc.Initialize(keyService, accountService);
            kc.Dock = DockStyle.Fill;
            var tpKeys = new TabPage("API Keys");
            tpKeys.Controls.Add(kc);
            _tabs.TabPages.Add(tpKeys);

            // Tab 2: Accounts
            var ac = new AccountsControl();
            ac.Initialize(accountService, keyService);
            ac.Dock = DockStyle.Fill;
            var tpAcc = new TabPage("Accounts");
            tpAcc.Controls.Add(ac);
            _tabs.TabPages.Add(tpAcc);

            // Tab 3: Profiles (Data)
            var pc = new ProfilesControl();
            pc.Initialize(profileService);
            pc.Dock = DockStyle.Fill;
            var tpProf = new TabPage("Data Management");
            tpProf.Controls.Add(pc);
            _tabs.TabPages.Add(tpProf);

            this.Controls.Add(_tabs);
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
            this.SuspendLayout();
            this.Name = "ProfileManagerControl";
            this.Size = new System.Drawing.Size(800, 600);
            this.ResumeLayout(false);
        }
    }
}