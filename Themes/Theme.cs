/* File: Themes/Theme.cs */
using System.Drawing;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;

namespace CryptoDayTraderSuite.Themes
{
    public static class Theme
    {
        /* Dark Graphite Palette */
        public static Color SidebarBg = Color.FromArgb(21, 23, 23); // #151717
        public static Color ContentBg = Color.FromArgb(30, 32, 38); // #1E2026
        public static Color PanelBg   = Color.FromArgb(37, 39, 45); // #25272D
        
        public static Color SidebarHover = Color.FromArgb(41, 43, 49);
        public static Color SidebarText  = Color.FromArgb(160, 162, 168);
        public static Color TextMuted    = Color.FromArgb(128, 130, 133); // #808285

        public static Color Accent = Color.FromArgb(80, 140, 255); // #508CFF
        public static Color Text   = Color.FromArgb(230, 232, 236); // #E6E8EC

        /* Legacy Aliases for backwards compatibility */
        public static Color Bg => ContentBg;
        public static Color Panel => PanelBg;

        public static void Apply(Form f)
        {
            f.BackColor = SidebarBg; // Shell uses darkest
            f.ForeColor = Text;
            foreach (Control c in f.Controls) ApplyControl(c);
        }

        public static void Apply(UserControl uc)
        {
            uc.BackColor = ContentBg;
            uc.ForeColor = Text;
            foreach (Control c in uc.Controls) ApplyControl(c);
        }

        private static void ApplyControl(Control c)
        {
            if (c is Button) 
            { 
                var b = (Button)c; 
                b.FlatStyle = FlatStyle.Flat; 
                b.FlatAppearance.BorderColor = PanelBg; 
                b.BackColor = PanelBg; 
                b.ForeColor = Text; 
            }
            else if (c is TextBox) { var t = (TextBox)c; t.BackColor = PanelBg; t.ForeColor = Text; t.BorderStyle = BorderStyle.FixedSingle; }
            else if (c is ComboBox) { c.BackColor = PanelBg; c.ForeColor = Text; }
            else if (c is NumericUpDown) { c.BackColor = PanelBg; c.ForeColor = Text; }
            else if (c is DataGridView) 
            { 
                var g = (DataGridView)c; 
                g.BackgroundColor = ContentBg; 
                g.BorderStyle = BorderStyle.None; 
                g.EnableHeadersVisualStyles = false; 
                g.ColumnHeadersDefaultCellStyle.BackColor = PanelBg; 
                g.ColumnHeadersDefaultCellStyle.ForeColor = TextMuted; 
                g.DefaultCellStyle.BackColor = ContentBg; 
                g.DefaultCellStyle.ForeColor = Text; 
                g.GridColor = Color.FromArgb(45, 47, 53);
            }
            else if (c is TabControl) { c.BackColor = ContentBg; c.ForeColor = Text; }
            else if (c is Chart) { 
                var ch = (Chart)c; 
                ch.BackColor = ContentBg; 
                ch.ForeColor = Text; 
                foreach(var a in ch.ChartAreas) { 
                    a.BackColor = ContentBg; 
                    a.AxisX.LabelStyle.ForeColor = TextMuted; 
                    a.AxisY.LabelStyle.ForeColor = TextMuted; 
                    a.AxisX.LineColor = Color.FromArgb(60, 60, 60); 
                    a.AxisY.LineColor = Color.FromArgb(60, 60, 60);
                    a.AxisX.MajorGrid.LineColor = Color.FromArgb(45, 47, 53);
                    a.AxisY.MajorGrid.LineColor = Color.FromArgb(45, 47, 53);
                }
                foreach(var s in ch.Series) { s.LabelForeColor = Text; }
                foreach(var t in ch.Titles) { t.ForeColor = Text; t.BackColor = ContentBg; }
                foreach(var l in ch.Legends) { l.ForeColor = TextMuted; l.BackColor = ContentBg; }
            }
            else 
            { 
                if (c.BackColor == SystemColors.Control) c.BackColor = ContentBg; 
                c.ForeColor = Text; 
            }

            foreach (Control k in c.Controls) ApplyControl(k);
        }
    }
}
