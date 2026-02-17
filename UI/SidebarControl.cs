using System;
using System.Drawing;
using System.Windows.Forms;
using CryptoDayTraderSuite.Themes;
using CryptoDayTraderSuite.Services;

namespace CryptoDayTraderSuite.UI
{
    public partial class SidebarControl : UserControl
    {
        public event Action<string> NavigationSelected;
        
        private bool _isExpanded = true;
        private Button _activeBtn;
        private int _expandedGovernorHeight = 160;

        public bool IsExpanded
        {
            get => _isExpanded;
            set
            {
                _isExpanded = value;
                UpdateLayoutState();
            }
        }

        public SidebarControl()
        {
            InitializeComponent();
            ApplyTheme();
            MinimumSize = new Size(50, 0);
            if (widgetGovernor != null) _expandedGovernorHeight = widgetGovernor.Height;
            if (_layoutPanel != null)
            {
                _layoutPanel.AutoScroll = false;
                _layoutPanel.Padding = Padding.Empty;
                foreach (Control c in _layoutPanel.Controls)
                {
                    if (c is Button b)
                    {
                        b.Margin = Padding.Empty;
                        if (b != btnMenu && b.Tag == null)
                        {
                            var label = (b.Text ?? string.Empty).Trim();
                            if (label.StartsWith("  "))
                            {
                                label = label.Substring(2).Trim();
                            }
                            b.Tag = label;
                        }
                    }
                }
            }
            this.SizeChanged += (s, e) => LayoutSidebarRegions();
            
            // Default select Dashboard
            SetActive(btnDashboard);
            LayoutSidebarRegions();
        }

        public void SetActivePage(string page)
        {
            var btnName = "btn" + page;
            foreach (Control c in _layoutPanel.Controls)
            {
                if (c is Button b && b.Name == btnName)
                {
                    SetActive(b);
                    break;
                }
            }
        }

        public void Configure(AIGovernor gov)
        {
            if (widgetGovernor != null)
            {
                widgetGovernor.Configure(gov);
            }
        }

        private void ApplyTheme()
        {
            this.BackColor = Theme.SidebarBg;
            _layoutPanel.BackColor = Theme.SidebarBg;
            
            // Widget handles its own theme but backcolor must match
            if (widgetGovernor != null) widgetGovernor.BackColor = Theme.SidebarBg;

            foreach (Control c in _layoutPanel.Controls)
            {
                if (c is Button b)
                {
                    b.BackColor = Theme.SidebarBg;
                    b.ForeColor = Theme.SidebarText;
                    b.FlatAppearance.MouseOverBackColor = Theme.SidebarHover;
                    b.FlatAppearance.MouseDownBackColor = Theme.PanelBg;
                }
            }
        }

        private void btnMenu_Click(object sender, EventArgs e)
        {
            IsExpanded = !IsExpanded;
        }

        private void UpdateLayoutState()
        {
            this.Width = _isExpanded ? 200 : 50;
            _layoutPanel.Width = this.Width;
            
            foreach (Control c in _layoutPanel.Controls)
            {
                if (c is Button b)
                {
                    var targetWidth = _layoutPanel != null ? _layoutPanel.ClientSize.Width : this.Width;
                    if (targetWidth < 0) targetWidth = 0;
                    b.Width = targetWidth;
                    if (b == btnMenu)
                    {
                        b.Text = "";
                        b.TextAlign = ContentAlignment.MiddleCenter;
                    }
                    else
                    {
                        var label = Convert.ToString(b.Tag) ?? b.Name.Replace("btn", "");
                        b.Text = _isExpanded ? "  " + label : "";
                        b.TextAlign = _isExpanded ? ContentAlignment.MiddleLeft : ContentAlignment.MiddleCenter;
                    }
                    
                    // Keep icon alignment
                    if (!_isExpanded && b != btnMenu) b.Image = null;
                }
            }

            // Hide/Show Governor when collapsed (too complex to shrink)
            if (widgetGovernor != null)
            {
                if (_isExpanded)
                {
                    widgetGovernor.Visible = true;
                    widgetGovernor.Height = _expandedGovernorHeight;
                }
                else
                {
                    widgetGovernor.Visible = false;
                    widgetGovernor.Height = 0;
                }
            }

            LayoutSidebarRegions();

            PerformLayout();
        }

        private void LayoutSidebarRegions()
        {
            if (_layoutPanel == null) return;

            var totalHeight = this.ClientSize.Height;
            if (totalHeight < 0) totalHeight = 0;

            var showGovernor = widgetGovernor != null && widgetGovernor.Visible;
            var governorHeight = showGovernor ? _expandedGovernorHeight : 0;

            var buttonCount = 0;
            foreach (Control c in _layoutPanel.Controls)
            {
                if (c is Button) buttonCount++;
            }

            if (buttonCount <= 0)
            {
                _layoutPanel.Height = totalHeight;
                return;
            }

            const int minimumPerButton = 28;
            var minimumNavHeight = minimumPerButton * buttonCount;

            if (showGovernor)
            {
                var maxGovernorHeight = totalHeight - minimumNavHeight;
                if (maxGovernorHeight < 0) maxGovernorHeight = 0;
                if (governorHeight > maxGovernorHeight) governorHeight = maxGovernorHeight;
                widgetGovernor.Height = governorHeight;
            }

            var navHeight = totalHeight - governorHeight;
            if (navHeight < 0) navHeight = 0;
            _layoutPanel.Height = navHeight;

            var buttonHeight = buttonCount > 0 ? navHeight / buttonCount : navHeight;
            if (buttonHeight < 22) buttonHeight = 22;

            var panelWidth = _layoutPanel.ClientSize.Width;
            if (panelWidth < 0) panelWidth = 0;

            foreach (Control c in _layoutPanel.Controls)
            {
                if (c is Button b)
                {
                    b.Margin = Padding.Empty;
                    b.Width = panelWidth;
                    b.Height = buttonHeight;
                }
            }
        }

        private void OnNavClick(object sender, EventArgs e)
        {
            if (sender is Button b)
            {
                SetActive(b);
                var page = b.Name.Replace("btn", "");
                NavigationSelected?.Invoke(page);
            }
        }

        private void SetActive(Button b)
        {
            if (_activeBtn != null)
            {
                _activeBtn.ForeColor = Theme.SidebarText;
                _activeBtn.BackColor = Theme.SidebarBg;
            }
            
            _activeBtn = b;
            _activeBtn.ForeColor = Theme.Accent;
            _activeBtn.BackColor = Theme.SidebarHover;
        }
    }
}