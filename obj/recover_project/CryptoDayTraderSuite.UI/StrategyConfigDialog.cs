using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using CryptoDayTraderSuite.Strategy;
using CryptoDayTraderSuite.Themes;

namespace CryptoDayTraderSuite.UI
{
	public class StrategyConfigDialog : Form
	{
		private readonly StrategyEngine _engine;

		private IContainer components = null;

		private TableLayoutPanel layoutRoot;

		private TableLayoutPanel layoutTop;

		private ComboBox cmbStrategy;

		private Label lblStrategy;

		private PropertyGrid propertyGrid;

		private Button btnClose;

		public StrategyConfigDialog(StrategyEngine engine)
		{
			InitializeComponent();
			DialogTheme.Apply(this);
			propertyGrid.ViewBackColor = Theme.ContentBg;
			propertyGrid.ViewForeColor = Theme.Text;
			propertyGrid.LineColor = Theme.PanelBg;
			propertyGrid.CategoryForeColor = Theme.Accent;
			propertyGrid.HelpBackColor = Theme.PanelBg;
			propertyGrid.HelpForeColor = Theme.TextMuted;
			_engine = engine;
			cmbStrategy.Items.Add("ORB Strategy");
			cmbStrategy.Items.Add("VWAP Trend");
			cmbStrategy.Items.Add("RSI Reversion");
			cmbStrategy.Items.Add("Donchian 20");
			cmbStrategy.SelectedIndex = 0;
		}

		private void cmbStrategy_SelectedIndexChanged(object sender, EventArgs e)
		{
			if (_engine != null)
			{
				switch (cmbStrategy.SelectedItem.ToString())
				{
				case "ORB Strategy":
					propertyGrid.SelectedObject = _engine.Orb;
					break;
				case "VWAP Trend":
					propertyGrid.SelectedObject = _engine.VwapTrend;
					break;
				case "RSI Reversion":
					propertyGrid.SelectedObject = _engine.RsiReversion;
					break;
				case "Donchian 20":
					propertyGrid.SelectedObject = _engine.Donchian;
					break;
				}
			}
		}

		private void btnClose_Click(object sender, EventArgs e)
		{
			Close();
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
			this.layoutRoot = new System.Windows.Forms.TableLayoutPanel();
			this.layoutTop = new System.Windows.Forms.TableLayoutPanel();
			this.cmbStrategy = new System.Windows.Forms.ComboBox();
			this.lblStrategy = new System.Windows.Forms.Label();
			this.propertyGrid = new System.Windows.Forms.PropertyGrid();
			this.btnClose = new System.Windows.Forms.Button();
			this.layoutRoot.SuspendLayout();
			this.layoutTop.SuspendLayout();
			base.SuspendLayout();
			this.layoutRoot.ColumnCount = 1;
			this.layoutRoot.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100f));
			this.layoutRoot.Controls.Add(this.layoutTop, 0, 0);
			this.layoutRoot.Controls.Add(this.propertyGrid, 0, 1);
			this.layoutRoot.Controls.Add(this.btnClose, 0, 2);
			this.layoutRoot.Dock = System.Windows.Forms.DockStyle.Fill;
			this.layoutRoot.Location = new System.Drawing.Point(0, 0);
			this.layoutRoot.Name = "layoutRoot";
			this.layoutRoot.Padding = new System.Windows.Forms.Padding(14);
			this.layoutRoot.RowCount = 3;
			this.layoutRoot.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
			this.layoutRoot.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100f));
			this.layoutRoot.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 48f));
			this.layoutRoot.Size = new System.Drawing.Size(500, 560);
			this.layoutRoot.TabIndex = 0;
			this.layoutTop.ColumnCount = 2;
			this.layoutTop.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 92f));
			this.layoutTop.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100f));
			this.layoutTop.Controls.Add(this.lblStrategy, 0, 0);
			this.layoutTop.Controls.Add(this.cmbStrategy, 1, 0);
			this.layoutTop.Dock = System.Windows.Forms.DockStyle.Fill;
			this.layoutTop.Location = new System.Drawing.Point(17, 17);
			this.layoutTop.Name = "layoutTop";
			this.layoutTop.RowCount = 1;
			this.layoutTop.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100f));
			this.layoutTop.Size = new System.Drawing.Size(466, 32);
			this.layoutTop.TabIndex = 0;
			this.cmbStrategy.Dock = System.Windows.Forms.DockStyle.Fill;
			this.cmbStrategy.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.cmbStrategy.FormattingEnabled = true;
			this.cmbStrategy.Location = new System.Drawing.Point(95, 4);
			this.cmbStrategy.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
			this.cmbStrategy.Name = "cmbStrategy";
			this.cmbStrategy.Size = new System.Drawing.Size(368, 21);
			this.cmbStrategy.TabIndex = 0;
			this.cmbStrategy.SelectedIndexChanged += new System.EventHandler(cmbStrategy_SelectedIndexChanged);
			this.lblStrategy.Anchor = System.Windows.Forms.AnchorStyles.Left;
			this.lblStrategy.AutoSize = true;
			this.lblStrategy.Location = new System.Drawing.Point(3, 9);
			this.lblStrategy.Name = "lblStrategy";
			this.lblStrategy.Size = new System.Drawing.Size(49, 13);
			this.lblStrategy.TabIndex = 1;
			this.lblStrategy.Text = "Strategy:";
			this.propertyGrid.Dock = System.Windows.Forms.DockStyle.Fill;
			this.propertyGrid.Location = new System.Drawing.Point(17, 55);
			this.propertyGrid.Name = "propertyGrid";
			this.propertyGrid.Size = new System.Drawing.Size(466, 440);
			this.propertyGrid.TabIndex = 2;
			this.propertyGrid.ToolbarVisible = false;
			this.btnClose.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right;
			this.btnClose.Location = new System.Drawing.Point(390, 509);
			this.btnClose.Margin = new System.Windows.Forms.Padding(3, 10, 3, 0);
			this.btnClose.Name = "btnClose";
			this.btnClose.Size = new System.Drawing.Size(93, 30);
			this.btnClose.TabIndex = 3;
			this.btnClose.Text = "Close";
			this.btnClose.UseVisualStyleBackColor = true;
			this.btnClose.Click += new System.EventHandler(btnClose_Click);
			base.AutoScaleDimensions = new System.Drawing.SizeF(6f, 13f);
			base.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			base.ClientSize = new System.Drawing.Size(500, 560);
			base.Controls.Add(this.layoutRoot);
			base.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
			base.MaximizeBox = false;
			base.MinimizeBox = false;
			base.Name = "StrategyConfigDialog";
			base.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
			this.Text = "Configuration";
			this.layoutRoot.ResumeLayout(false);
			this.layoutTop.ResumeLayout(false);
			this.layoutTop.PerformLayout();
			base.ResumeLayout(false);
		}
	}
}
