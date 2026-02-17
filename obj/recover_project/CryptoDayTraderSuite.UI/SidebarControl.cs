using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using CryptoDayTraderSuite.Services;
using CryptoDayTraderSuite.Themes;

namespace CryptoDayTraderSuite.UI
{
	public class SidebarControl : UserControl
	{
		private bool _isExpanded = true;

		private Button _activeBtn;

		private int _expandedGovernorHeight = 160;

		private IContainer components = null;

		private FlowLayoutPanel _layoutPanel;

		private Button btnMenu;

		private Button btnDashboard;

		private Button btnTrading;

		private Button btnPlanner;

		private Button btnAuto;

		private Button btnAccounts;

		private Button btnKeys;

		private Button btnSettings;

		private GovernorWidget widgetGovernor;

		public bool IsExpanded
		{
			get
			{
				return _isExpanded;
			}
			set
			{
				_isExpanded = value;
				UpdateLayoutState();
			}
		}

		public event Action<string> NavigationSelected;

		public SidebarControl()
		{
			InitializeComponent();
			ApplyTheme();
			MinimumSize = new Size(50, 0);
			if (widgetGovernor != null)
			{
				_expandedGovernorHeight = widgetGovernor.Height;
			}
			if (_layoutPanel != null)
			{
				_layoutPanel.AutoScroll = false;
				_layoutPanel.Padding = Padding.Empty;
				foreach (Control c in _layoutPanel.Controls)
				{
					if (c is Button b)
					{
						b.Margin = Padding.Empty;
					}
				}
			}
			base.SizeChanged += delegate
			{
				LayoutSidebarRegions();
			};
			SetActive(btnDashboard);
			LayoutSidebarRegions();
		}

		public void SetActivePage(string page)
		{
			string btnName = "btn" + page;
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
			BackColor = Theme.SidebarBg;
			_layoutPanel.BackColor = Theme.SidebarBg;
			if (widgetGovernor != null)
			{
				widgetGovernor.BackColor = Theme.SidebarBg;
			}
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
			base.Width = (_isExpanded ? 200 : 50);
			_layoutPanel.Width = base.Width;
			foreach (Control c in _layoutPanel.Controls)
			{
				if (c is Button b)
				{
					int targetWidth = ((_layoutPanel != null) ? _layoutPanel.ClientSize.Width : base.Width);
					if (targetWidth < 0)
					{
						targetWidth = 0;
					}
					b.Width = targetWidth;
					if (b == btnMenu)
					{
						b.Text = "\ue700";
						b.TextAlign = ContentAlignment.MiddleCenter;
					}
					else
					{
						b.Text = (_isExpanded ? ("  " + b.Name.Replace("btn", "")) : "");
						b.TextAlign = (_isExpanded ? ContentAlignment.MiddleLeft : ContentAlignment.MiddleCenter);
					}
					if (!_isExpanded && b != btnMenu)
					{
						b.Image = null;
					}
				}
			}
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
			if (_layoutPanel == null)
			{
				return;
			}
			int totalHeight = base.ClientSize.Height;
			if (totalHeight < 0)
			{
				totalHeight = 0;
			}
			bool showGovernor = widgetGovernor != null && widgetGovernor.Visible;
			int governorHeight = (showGovernor ? _expandedGovernorHeight : 0);
			int buttonCount = 0;
			foreach (Control c in _layoutPanel.Controls)
			{
				if (c is Button)
				{
					buttonCount++;
				}
			}
			if (buttonCount <= 0)
			{
				_layoutPanel.Height = totalHeight;
				return;
			}
			int minimumNavHeight = 28 * buttonCount;
			if (showGovernor)
			{
				int maxGovernorHeight = totalHeight - minimumNavHeight;
				if (maxGovernorHeight < 0)
				{
					maxGovernorHeight = 0;
				}
				if (governorHeight > maxGovernorHeight)
				{
					governorHeight = maxGovernorHeight;
				}
				widgetGovernor.Height = governorHeight;
			}
			int navHeight = totalHeight - governorHeight;
			if (navHeight < 0)
			{
				navHeight = 0;
			}
			_layoutPanel.Height = navHeight;
			int buttonHeight = ((buttonCount > 0) ? (navHeight / buttonCount) : navHeight);
			if (buttonHeight < 22)
			{
				buttonHeight = 22;
			}
			int panelWidth = _layoutPanel.ClientSize.Width;
			if (panelWidth < 0)
			{
				panelWidth = 0;
			}
			foreach (Control c2 in _layoutPanel.Controls)
			{
				if (c2 is Button b)
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
				string page = b.Name.Replace("btn", "");
				this.NavigationSelected?.Invoke(page);
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

		protected override void Dispose(bool disposing)
		{
			if (disposing && components != null)
			{
				components.Dispose();
			}
			base.Dispose(disposing);
		}

		private void InitializeComponent()
		{
			this._layoutPanel = new System.Windows.Forms.FlowLayoutPanel();
			this.btnMenu = new System.Windows.Forms.Button();
			this.btnDashboard = new System.Windows.Forms.Button();
			this.btnTrading = new System.Windows.Forms.Button();
			this.btnPlanner = new System.Windows.Forms.Button();
			this.btnAuto = new System.Windows.Forms.Button();
			this.btnAccounts = new System.Windows.Forms.Button();
			this.btnKeys = new System.Windows.Forms.Button();
			this.btnSettings = new System.Windows.Forms.Button();
			this.widgetGovernor = new CryptoDayTraderSuite.UI.GovernorWidget();
			this._layoutPanel.SuspendLayout();
			base.SuspendLayout();
			this._layoutPanel.Controls.Add(this.btnMenu);
			this._layoutPanel.Controls.Add(this.btnDashboard);
			this._layoutPanel.Controls.Add(this.btnTrading);
			this._layoutPanel.Controls.Add(this.btnPlanner);
			this._layoutPanel.Controls.Add(this.btnAuto);
			this._layoutPanel.Controls.Add(this.btnAccounts);
			this._layoutPanel.Controls.Add(this.btnKeys);
			this._layoutPanel.Controls.Add(this.btnSettings);
			this._layoutPanel.Dock = System.Windows.Forms.DockStyle.Top;
			this._layoutPanel.FlowDirection = System.Windows.Forms.FlowDirection.TopDown;
			this._layoutPanel.Location = new System.Drawing.Point(0, 0);
			this._layoutPanel.Name = "_layoutPanel";
			this._layoutPanel.Size = new System.Drawing.Size(200, 440);
			this._layoutPanel.TabIndex = 0;
			this._layoutPanel.WrapContents = false;
			this._layoutPanel.AutoScroll = false;
			this.btnMenu.FlatAppearance.BorderSize = 0;
			this.btnMenu.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
			this.btnMenu.Font = new System.Drawing.Font("Segoe MDL2 Assets", 12f, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
			this.btnMenu.ForeColor = System.Drawing.Color.FromArgb(160, 162, 168);
			this.btnMenu.Location = new System.Drawing.Point(3, 3);
			this.btnMenu.Name = "btnMenu";
			this.btnMenu.Size = new System.Drawing.Size(194, 40);
			this.btnMenu.TabIndex = 0;
			this.btnMenu.Text = "\ue700";
			this.btnMenu.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			this.btnMenu.UseVisualStyleBackColor = true;
			this.btnMenu.Click += new System.EventHandler(btnMenu_Click);
			this.btnDashboard.FlatAppearance.BorderSize = 0;
			this.btnDashboard.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
			this.btnDashboard.Font = new System.Drawing.Font("Segoe UI Semibold", 9.75f, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, 0);
			this.btnDashboard.ForeColor = System.Drawing.Color.FromArgb(160, 162, 168);
			this.btnDashboard.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft;
			this.btnDashboard.Location = new System.Drawing.Point(3, 49);
			this.btnDashboard.Name = "btnDashboard";
			this.btnDashboard.Size = new System.Drawing.Size(194, 45);
			this.btnDashboard.TabIndex = 1;
			this.btnDashboard.Text = "  Dashboard";
			this.btnDashboard.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			this.btnDashboard.UseVisualStyleBackColor = true;
			this.btnDashboard.Click += new System.EventHandler(OnNavClick);
			this.btnTrading.FlatAppearance.BorderSize = 0;
			this.btnTrading.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
			this.btnTrading.Font = new System.Drawing.Font("Segoe UI Semibold", 9.75f, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, 0);
			this.btnTrading.ForeColor = System.Drawing.Color.FromArgb(160, 162, 168);
			this.btnTrading.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft;
			this.btnTrading.Location = new System.Drawing.Point(3, 100);
			this.btnTrading.Name = "btnTrading";
			this.btnTrading.Size = new System.Drawing.Size(194, 45);
			this.btnTrading.TabIndex = 2;
			this.btnTrading.Text = "  Trading";
			this.btnTrading.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			this.btnTrading.UseVisualStyleBackColor = true;
			this.btnTrading.Click += new System.EventHandler(OnNavClick);
			this.btnPlanner.FlatAppearance.BorderSize = 0;
			this.btnPlanner.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
			this.btnPlanner.Font = new System.Drawing.Font("Segoe UI Semibold", 9.75f, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, 0);
			this.btnPlanner.ForeColor = System.Drawing.Color.FromArgb(160, 162, 168);
			this.btnPlanner.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft;
			this.btnPlanner.Location = new System.Drawing.Point(3, 151);
			this.btnPlanner.Name = "btnPlanner";
			this.btnPlanner.Size = new System.Drawing.Size(194, 45);
			this.btnPlanner.TabIndex = 3;
			this.btnPlanner.Text = "  Planner";
			this.btnPlanner.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			this.btnPlanner.UseVisualStyleBackColor = true;
			this.btnPlanner.Click += new System.EventHandler(OnNavClick);
			this.btnAuto.FlatAppearance.BorderSize = 0;
			this.btnAuto.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
			this.btnAuto.Font = new System.Drawing.Font("Segoe UI Semibold", 9.75f, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, 0);
			this.btnAuto.ForeColor = System.Drawing.Color.FromArgb(160, 162, 168);
			this.btnAuto.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft;
			this.btnAuto.Location = new System.Drawing.Point(3, 202);
			this.btnAuto.Name = "btnAuto";
			this.btnAuto.Size = new System.Drawing.Size(194, 45);
			this.btnAuto.TabIndex = 4;
			this.btnAuto.Text = "  Auto Mode";
			this.btnAuto.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			this.btnAuto.UseVisualStyleBackColor = true;
			this.btnAuto.Click += new System.EventHandler(OnNavClick);
			this.btnAccounts.FlatAppearance.BorderSize = 0;
			this.btnAccounts.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
			this.btnAccounts.Font = new System.Drawing.Font("Segoe UI Semibold", 9.75f, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, 0);
			this.btnAccounts.ForeColor = System.Drawing.Color.FromArgb(160, 162, 168);
			this.btnAccounts.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft;
			this.btnAccounts.Name = "btnAccounts";
			this.btnAccounts.Size = new System.Drawing.Size(194, 45);
			this.btnAccounts.TabIndex = 5;
			this.btnAccounts.Text = "  Accounts";
			this.btnAccounts.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			this.btnAccounts.UseVisualStyleBackColor = true;
			this.btnAccounts.Click += new System.EventHandler(OnNavClick);
			this.btnKeys.FlatAppearance.BorderSize = 0;
			this.btnKeys.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
			this.btnKeys.Font = new System.Drawing.Font("Segoe UI Semibold", 9.75f, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, 0);
			this.btnKeys.ForeColor = System.Drawing.Color.FromArgb(160, 162, 168);
			this.btnKeys.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft;
			this.btnKeys.Name = "btnKeys";
			this.btnKeys.Size = new System.Drawing.Size(194, 45);
			this.btnKeys.TabIndex = 6;
			this.btnKeys.Text = "  API Keys";
			this.btnKeys.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			this.btnKeys.UseVisualStyleBackColor = true;
			this.btnKeys.Click += new System.EventHandler(OnNavClick);
			this.btnSettings.FlatAppearance.BorderSize = 0;
			this.btnSettings.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
			this.btnSettings.Font = new System.Drawing.Font("Segoe UI Semibold", 9.75f, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, 0);
			this.btnSettings.ForeColor = System.Drawing.Color.FromArgb(160, 162, 168);
			this.btnSettings.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft;
			this.btnSettings.Name = "btnSettings";
			this.btnSettings.Size = new System.Drawing.Size(194, 45);
			this.btnSettings.TabIndex = 7;
			this.btnSettings.Text = "  Settings";
			this.btnSettings.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			this.btnSettings.UseVisualStyleBackColor = true;
			this.btnSettings.Click += new System.EventHandler(OnNavClick);
			this.widgetGovernor.Dock = System.Windows.Forms.DockStyle.Bottom;
			this.widgetGovernor.Location = new System.Drawing.Point(0, 440);
			this.widgetGovernor.Name = "widgetGovernor";
			this.widgetGovernor.Size = new System.Drawing.Size(200, 160);
			this.widgetGovernor.TabIndex = 6;
			base.AutoScaleDimensions = new System.Drawing.SizeF(6f, 13f);
			base.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.BackColor = System.Drawing.Color.FromArgb(21, 23, 23);
			base.Controls.Add(this._layoutPanel);
			base.Controls.Add(this.widgetGovernor);
			base.Name = "SidebarControl";
			base.Size = new System.Drawing.Size(200, 600);
			this._layoutPanel.ResumeLayout(false);
			base.ResumeLayout(false);
		}
	}
}
