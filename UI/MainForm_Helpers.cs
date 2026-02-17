using System;
using System.Reflection;
using System.Windows.Forms;

namespace CryptoDayTraderSuite
{
    public partial class MainForm : Form
    {
        /* generic control-field finder shared across partials */
        private T FindField<T>(string name) where T : class
        {
            try
            {
                var fi = this.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                if (fi == null) return null;
                return fi.GetValue(this) as T;
            }
            catch { return null; }
        }
    }
}