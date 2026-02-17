/* File: Themes/LightTheme.cs */
/* Author: Gregory King */
/* Date: 2025-08-10 */
/* Description: Light theme implementation for the trading suite */

using System.Drawing;
using System.Windows.Forms;

namespace CryptoDayTraderSuite.Themes
{
    public static class LightTheme
    {
        public static Color Bg = Color.FromArgb(248, 249, 250);
        public static Color Panel = Color.FromArgb(255, 255, 255);
        public static Color Text = Color.FromArgb(33, 37, 41);
        public static Color Border = Color.FromArgb(222, 226, 230);
        public static Color Accent = Color.FromArgb(13, 110, 253);
        public static Color Success = Color.FromArgb(25, 135, 84);
        public static Color Warning = Color.FromArgb(255, 193, 7);
        public static Color Danger = Color.FromArgb(220, 53, 69);

        public static void Apply(Form f)
        {
            f.BackColor = Bg;
            f.ForeColor = Text;
            
            foreach (Control c in f.Controls)
            {
                ApplyControl(c);
            }
        }

        private static void ApplyControl(Control c)
        {
            if (c is Button btn)
            {
                btn.FlatStyle = FlatStyle.Flat;
                btn.FlatAppearance.BorderColor = Border;
                btn.BackColor = Panel;
                btn.ForeColor = Text;
            }
            else if (c is TextBox || c is ComboBox || c is NumericUpDown)
            {
                c.BackColor = Panel;
                c.ForeColor = Text;
                if (c is TextBox txt) txt.BorderStyle = BorderStyle.FixedSingle;
            }
            else if (c is DataGridView grid)
            {
                grid.BackgroundColor = Panel;
                grid.BorderStyle = BorderStyle.None;
                grid.EnableHeadersVisualStyles = false;
                grid.ColumnHeadersDefaultCellStyle.BackColor = Bg;
                grid.ColumnHeadersDefaultCellStyle.ForeColor = Text;
                grid.DefaultCellStyle.BackColor = Panel;
                grid.DefaultCellStyle.ForeColor = Text;
                grid.GridColor = Border;
            }
            else if (c is TabControl)
            {
                c.BackColor = Bg;
                c.ForeColor = Text;
            }
            else if (c is MenuStrip menu)
            {
                menu.BackColor = Panel;
                menu.ForeColor = Text;
                foreach (ToolStripMenuItem item in menu.Items)
                {
                    ApplyMenuColors(item);
                }
            }
            else
            {
                if (c.BackColor == SystemColors.Control)
                    c.BackColor = Panel;
                c.ForeColor = Text;
            }

            foreach (Control child in c.Controls)
            {
                ApplyControl(child);
            }
        }

        private static void ApplyMenuColors(ToolStripMenuItem item)
        {
            item.BackColor = Panel;
            item.ForeColor = Text;
            foreach (ToolStripItem subItem in item.DropDownItems)
            {
                if (subItem is ToolStripMenuItem menuItem)
                {
                    ApplyMenuColors(menuItem);
                }
            }
        }
    }
}