using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using CryptoDayTraderSuite.Themes;

namespace CryptoDayTraderSuite.UI
{
    partial class SidebarControl
    {
        /// <summary> 
        /// Required designer variable.
        /// </summary>
        private IContainer components = null;

        /// <summary> 
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this._layoutPanel = new System.Windows.Forms.FlowLayoutPanel();
            this.btnMenu = new System.Windows.Forms.Button();
            this.btnDashboard = new System.Windows.Forms.Button();
            this.btnTrading = new System.Windows.Forms.Button();
            this.btnPlanner = new System.Windows.Forms.Button();
            this.btnAuto = new System.Windows.Forms.Button();
            this.btnAccounts = new System.Windows.Forms.Button();
            this.btnInsights = new System.Windows.Forms.Button();
            this.btnKeys = new System.Windows.Forms.Button();
            this.btnSettings = new System.Windows.Forms.Button();
            this.widgetGovernor = new CryptoDayTraderSuite.UI.GovernorWidget();
            this._layoutPanel.SuspendLayout();
            this.SuspendLayout();
            // 
            // _layoutPanel
            // 
            this._layoutPanel.Controls.Add(this.btnMenu);
            this._layoutPanel.Controls.Add(this.btnDashboard);
            this._layoutPanel.Controls.Add(this.btnTrading);
            this._layoutPanel.Controls.Add(this.btnPlanner);
            this._layoutPanel.Controls.Add(this.btnAuto);
            this._layoutPanel.Controls.Add(this.btnAccounts);
            this._layoutPanel.Controls.Add(this.btnInsights);
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
            // 
            // btnMenu
            // 
            this.btnMenu.FlatAppearance.BorderSize = 0;
            this.btnMenu.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnMenu.Font = new System.Drawing.Font("Segoe MDL2 Assets", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnMenu.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(160)))), ((int)(((byte)(162)))), ((int)(((byte)(168)))));
            this.btnMenu.Location = new System.Drawing.Point(3, 3);
            this.btnMenu.Name = "btnMenu";
            this.btnMenu.Size = new System.Drawing.Size(194, 40);
            this.btnMenu.TabIndex = 0;
            this.btnMenu.Text = ""; 
            this.btnMenu.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.btnMenu.UseVisualStyleBackColor = true;
            this.btnMenu.Click += new System.EventHandler(this.btnMenu_Click);
            // 
            // btnDashboard
            // 
            this.btnDashboard.FlatAppearance.BorderSize = 0;
            this.btnDashboard.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnDashboard.Font = new System.Drawing.Font("Segoe UI Semibold", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnDashboard.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(160)))), ((int)(((byte)(162)))), ((int)(((byte)(168)))));
            this.btnDashboard.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.btnDashboard.Location = new System.Drawing.Point(3, 49);
            this.btnDashboard.Name = "btnDashboard";
            this.btnDashboard.Size = new System.Drawing.Size(194, 45);
            this.btnDashboard.TabIndex = 1;
            this.btnDashboard.Text = "  Dashboard";
            this.btnDashboard.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.btnDashboard.UseVisualStyleBackColor = true;
            this.btnDashboard.Click += new System.EventHandler(this.OnNavClick);
            // 
            // btnTrading
            // 
            this.btnTrading.FlatAppearance.BorderSize = 0;
            this.btnTrading.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnTrading.Font = new System.Drawing.Font("Segoe UI Semibold", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnTrading.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(160)))), ((int)(((byte)(162)))), ((int)(((byte)(168)))));
            this.btnTrading.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.btnTrading.Location = new System.Drawing.Point(3, 100);
            this.btnTrading.Name = "btnTrading";
            this.btnTrading.Size = new System.Drawing.Size(194, 45);
            this.btnTrading.TabIndex = 2;
            this.btnTrading.Text = "  Trading";
            this.btnTrading.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.btnTrading.UseVisualStyleBackColor = true;
            this.btnTrading.Click += new System.EventHandler(this.OnNavClick);
            // 
            // btnPlanner
            // 
            this.btnPlanner.FlatAppearance.BorderSize = 0;
            this.btnPlanner.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnPlanner.Font = new System.Drawing.Font("Segoe UI Semibold", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnPlanner.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(160)))), ((int)(((byte)(162)))), ((int)(((byte)(168)))));
            this.btnPlanner.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.btnPlanner.Location = new System.Drawing.Point(3, 151);
            this.btnPlanner.Name = "btnPlanner";
            this.btnPlanner.Size = new System.Drawing.Size(194, 45);
            this.btnPlanner.TabIndex = 3;
            this.btnPlanner.Text = "  Planner";
            this.btnPlanner.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.btnPlanner.UseVisualStyleBackColor = true;
            this.btnPlanner.Click += new System.EventHandler(this.OnNavClick);
            // 
            // btnAuto
            // 
            this.btnAuto.FlatAppearance.BorderSize = 0;
            this.btnAuto.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnAuto.Font = new System.Drawing.Font("Segoe UI Semibold", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnAuto.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(160)))), ((int)(((byte)(162)))), ((int)(((byte)(168)))));
            this.btnAuto.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.btnAuto.Location = new System.Drawing.Point(3, 202);
            this.btnAuto.Name = "btnAuto";
            this.btnAuto.Size = new System.Drawing.Size(194, 45);
            this.btnAuto.TabIndex = 4;
            this.btnAuto.Text = "  Auto Mode";
            this.btnAuto.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.btnAuto.UseVisualStyleBackColor = true;
            this.btnAuto.Click += new System.EventHandler(this.OnNavClick);
            // 
            // btnAccounts
            // 
            this.btnAccounts.FlatAppearance.BorderSize = 0;
            this.btnAccounts.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnAccounts.Font = new System.Drawing.Font("Segoe UI Semibold", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnAccounts.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(160)))), ((int)(((byte)(162)))), ((int)(((byte)(168)))));
            this.btnAccounts.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.btnAccounts.Name = "btnAccounts";
            this.btnAccounts.Size = new System.Drawing.Size(194, 45);
            this.btnAccounts.TabIndex = 5;
            this.btnAccounts.Text = "  Accounts";
            this.btnAccounts.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.btnAccounts.UseVisualStyleBackColor = true;
            this.btnAccounts.Click += new System.EventHandler(this.OnNavClick);
            // 
            // btnInsights
            // 
            this.btnInsights.FlatAppearance.BorderSize = 0;
            this.btnInsights.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnInsights.Font = new System.Drawing.Font("Segoe UI Semibold", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnInsights.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(160)))), ((int)(((byte)(162)))), ((int)(((byte)(168)))));
            this.btnInsights.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.btnInsights.Name = "btnInsights";
            this.btnInsights.Size = new System.Drawing.Size(194, 45);
            this.btnInsights.TabIndex = 6;
            this.btnInsights.Text = "  Account Insights";
            this.btnInsights.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.btnInsights.UseVisualStyleBackColor = true;
            this.btnInsights.Click += new System.EventHandler(this.OnNavClick);
            // 
            // btnKeys
            // 
            this.btnKeys.FlatAppearance.BorderSize = 0;
            this.btnKeys.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnKeys.Font = new System.Drawing.Font("Segoe UI Semibold", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnKeys.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(160)))), ((int)(((byte)(162)))), ((int)(((byte)(168)))));
            this.btnKeys.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.btnKeys.Name = "btnKeys";
            this.btnKeys.Size = new System.Drawing.Size(194, 45);
            this.btnKeys.TabIndex = 7;
            this.btnKeys.Text = "  API Keys";
            this.btnKeys.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.btnKeys.UseVisualStyleBackColor = true;
            this.btnKeys.Click += new System.EventHandler(this.OnNavClick);
            // 
            // btnSettings
            // 
            this.btnSettings.FlatAppearance.BorderSize = 0;
            this.btnSettings.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnSettings.Font = new System.Drawing.Font("Segoe UI Semibold", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnSettings.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(160)))), ((int)(((byte)(162)))), ((int)(((byte)(168)))));
            this.btnSettings.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.btnSettings.Name = "btnSettings";
            this.btnSettings.Size = new System.Drawing.Size(194, 45);
            this.btnSettings.TabIndex = 8;
            this.btnSettings.Text = "  Settings";
            this.btnSettings.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.btnSettings.UseVisualStyleBackColor = true;
            this.btnSettings.Click += new System.EventHandler(this.OnNavClick);
            // 
            // widgetGovernor
            // 
            this.widgetGovernor.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.widgetGovernor.Location = new System.Drawing.Point(0, 440);
            this.widgetGovernor.Name = "widgetGovernor";
            this.widgetGovernor.Size = new System.Drawing.Size(200, 160);
            this.widgetGovernor.TabIndex = 6;
            // 
            // SidebarControl
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(21)))), ((int)(((byte)(23)))), ((int)(((byte)(23)))));
            this.Controls.Add(this._layoutPanel);
            this.Controls.Add(this.widgetGovernor);
            this.Name = "SidebarControl";
            this.Size = new System.Drawing.Size(200, 600);
            this._layoutPanel.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.FlowLayoutPanel _layoutPanel;
        private System.Windows.Forms.Button btnMenu;
        private System.Windows.Forms.Button btnDashboard;
        private System.Windows.Forms.Button btnTrading;
        private System.Windows.Forms.Button btnPlanner;
        private System.Windows.Forms.Button btnAuto;
        private System.Windows.Forms.Button btnAccounts;
        private System.Windows.Forms.Button btnInsights;
        private System.Windows.Forms.Button btnKeys;
        private System.Windows.Forms.Button btnSettings;
        private CryptoDayTraderSuite.UI.GovernorWidget widgetGovernor;
    }
}