using System.Collections.Generic;
using System.Windows.Forms;

namespace CryptoDayTraderSuite.Util
{
    public static class ToolTipHelper
    {
        public static void Apply(Form f, Dictionary<Control, string> map)
        {
            if (f == null || map == null || map.Count == 0) return;
            var tt = new ToolTip();
            tt.AutoPopDelay = 20000;
            tt.InitialDelay = 400;
            tt.ReshowDelay = 200;
            tt.ShowAlways = true;
            foreach (var kv in map)
            {
                if (kv.Key != null && kv.Value != null) tt.SetToolTip(kv.Key, kv.Value);
            }
        }
    }
}