/* File: UI/MainForm_ThemeHook.cs */
/* partial class so you don't have to touch Designer */
using System.Windows.Forms;
using CryptoDayTraderSuite.Themes;

namespace CryptoDayTraderSuite
{
    public partial class MainForm : Form
    {
        protected override void OnShown(System.EventArgs e)
        {
            base.OnShown(e);
            Theme.Apply(this); /* apply dark theme */
        }
    }
}