using System;
using System.Windows.Forms;
using CryptoDayTraderSuite.Services;

namespace CryptoDayTraderSuite.UI
{
    public partial class AccountsForm : Form
    {
        public AccountsForm() : this(new AccountService()) { }

        public AccountsForm(IAccountService service)
        {
            InitializeComponent();
            accountsControl.Initialize(service);
        }
    }
}
