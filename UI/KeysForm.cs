using System;
using System.Windows.Forms;
using CryptoDayTraderSuite.Services;

namespace CryptoDayTraderSuite.UI
{
    public partial class KeysForm : Form
    {
        // Constructor for DI
        public KeysForm(IKeyService service) : this()
        {
            // find the control or add it
            KeysControl control = null;
            foreach (Control c in this.Controls)
            {
                if (c is KeysControl kc) { control = kc; break; }
            }

            if (control == null)
            {
                control = new KeysControl();
                control.Dock = DockStyle.Fill;
                this.Controls.Add(control);
            }

            control.Initialize(service);
        }

        public KeysForm()
        {
            InitializeComponent();
        }
    }
}
