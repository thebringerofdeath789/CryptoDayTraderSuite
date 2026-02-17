/* File: Themes/ModernLightTheme.cs */
/* Author: Gregory King */
/* Date: 2025-08-10 */
/* Description: Modern light theme implementation with improved UI/UX */

using System.Drawing;
using System.Windows.Forms;

namespace CryptoDayTraderSuite.Themes
{
    public static class ModernLightTheme
    {
        // Core colors
        public static Color Primary = Color.FromArgb(13, 110, 253);      // Bootstrap primary blue
        public static Color Background = Color.FromArgb(248, 249, 250);  // Light gray background
        public static Color Surface = Color.FromArgb(255, 255, 255);     // White surface
        public static Color Text = Color.FromArgb(33, 37, 41);          // Dark text
        public static Color Border = Color.FromArgb(222, 226, 230);     // Light border
        
        // Accent colors
        public static Color Success = Color.FromArgb(25, 135, 84);      // Green
        public static Color Warning = Color.FromArgb(255, 193, 7);      // Yellow
        public static Color Danger = Color.FromArgb(220, 53, 69);       // Red
        public static Color Info = Color.FromArgb(13, 202, 240);        // Light blue

        // Control-specific colors
        public static Color InputBackground = Color.FromArgb(255, 255, 255);
        public static Color ButtonHover = Color.FromArgb(227, 242, 253);
        public static Color GridHeader = Color.FromArgb(244, 246, 249);
        public static Color TabActive = Color.FromArgb(255, 255, 255);
        public static Color TabInactive = Color.FromArgb(242, 244, 247);

        public static void Apply(Form form)
        {
            form.BackColor = Background;
            form.ForeColor = Text;

            foreach (Control c in form.Controls)
                ApplyToControl(c);
        }

        private static void ApplyToControl(Control control)
        {
            if (control is Button btn)
            {
                btn.FlatStyle = FlatStyle.Flat;
                btn.FlatAppearance.BorderColor = Border;
                btn.BackColor = Surface;
                btn.ForeColor = Text;
                btn.Font = new Font("Segoe UI", 9F, FontStyle.Regular);
                btn.Padding = new Padding(10, 5, 10, 5);
                btn.Height = 32;
            }
            else if (control is TextBox txt)
            {
                txt.BorderStyle = BorderStyle.FixedSingle;
                txt.BackColor = InputBackground;
                txt.ForeColor = Text;
                txt.Font = new Font("Segoe UI", 9F, FontStyle.Regular);
            }
            else if (control is ComboBox cmb)
            {
                cmb.FlatStyle = FlatStyle.Flat;
                cmb.BackColor = InputBackground;
                cmb.ForeColor = Text;
                cmb.Font = new Font("Segoe UI", 9F, FontStyle.Regular);
                cmb.Height = 32;
            }
            else if (control is DataGridView grid)
            {
                grid.BackgroundColor = Surface;
                grid.BorderStyle = BorderStyle.None;
                grid.GridColor = Border;
                grid.EnableHeadersVisualStyles = false;
                grid.ColumnHeadersDefaultCellStyle.BackColor = GridHeader;
                grid.ColumnHeadersDefaultCellStyle.ForeColor = Text;
                grid.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
                grid.DefaultCellStyle.BackColor = Surface;
                grid.DefaultCellStyle.ForeColor = Text;
                grid.DefaultCellStyle.Font = new Font("Segoe UI", 9F, FontStyle.Regular);
                grid.RowHeadersDefaultCellStyle.BackColor = GridHeader;
            }
            else if (control is TabControl tabs)
            {
                tabs.DrawMode = TabDrawMode.OwnerDrawFixed;
                tabs.BackColor = Background;
                tabs.ForeColor = Text;
                tabs.Font = new Font("Segoe UI", 9F, FontStyle.Regular);
                
                tabs.DrawItem += (s, e) =>
                {
                    var g = e.Graphics;
                    var tabRect = tabs.GetTabRect(e.Index);
                    var selected = e.Index == tabs.SelectedIndex;
                    
                    using (var brush = new SolidBrush(selected ? TabActive : TabInactive))
                        g.FillRectangle(brush, tabRect);
                    
                    var textRect = tabRect;
                    textRect.Inflate(-6, -4);
                    using (var brush = new SolidBrush(Text))
                        g.DrawString(tabs.TabPages[e.Index].Text, tabs.Font, brush, textRect);
                    
                    if (selected)
                    {
                        using (var pen = new Pen(Primary, 2))
                            g.DrawLine(pen, tabRect.Left, tabRect.Bottom - 2, tabRect.Right, tabRect.Bottom - 2);
                    }
                };
            }
            else if (control is MenuStrip menu)
            {
                menu.BackColor = Surface;
                menu.ForeColor = Text;
                menu.Font = new Font("Segoe UI", 9F, FontStyle.Regular);
                menu.Padding = new Padding(6, 2, 0, 2);
                
                foreach (ToolStripMenuItem item in menu.Items)
                    ApplyToMenuItem(item);
            }

            foreach (Control child in control.Controls)
                ApplyToControl(child);
        }

        private static void ApplyToMenuItem(ToolStripMenuItem item)
        {
            item.BackColor = Surface;
            item.ForeColor = Text;
            item.Font = new Font("Segoe UI", 9F, FontStyle.Regular);
            
            foreach (ToolStripItem subItem in item.DropDownItems)
            {
                if (subItem is ToolStripMenuItem menuItem)
                    ApplyToMenuItem(menuItem);
            }
        }
    }
}