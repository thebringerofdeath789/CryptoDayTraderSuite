using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using CryptoDayTraderSuite.Models;

namespace CryptoDayTraderSuite.UI
{
	public class PlannerForm : Form
	{
		private List<TradeRecord> _planned = new List<TradeRecord>();

		private List<PredictionRecord> _preds = new List<PredictionRecord>();

		private IContainer components = null;

		private DataGridView gridPlanned;

		private DataGridView gridPreds;

		private Button btnRefresh;

		private Button btnSave;

		private SplitContainer _split;

		public event EventHandler RequestRefresh;

		public PlannerForm()
		{
			InitializeComponent();
		}

		public void SetData(List<TradeRecord> planned, List<PredictionRecord> predictions)
		{
			_planned = planned ?? new List<TradeRecord>();
			if (predictions != null)
			{
				_preds = predictions;
			}
			LoadGrids();
		}

		private void LoadGrids()
		{
			gridPlanned.Rows.Clear();
			foreach (TradeRecord p in _planned)
			{
				gridPlanned.Rows.Add(p.Enabled, p.Exchange, p.ProductId, p.Strategy, p.Side, p.Quantity, p.Price, p.EstEdge, p.Notes);
			}
			gridPreds.Rows.Clear();
			foreach (PredictionRecord r in _preds.OrderByDescending((PredictionRecord x) => x.AtUtc).Take(500))
			{
				gridPreds.Rows.Add(r.ProductId, r.AtUtc.ToLocalTime(), r.HorizonMinutes, r.Direction, r.Probability, r.ExpectedReturn, r.ExpectedVol, r.RealizedKnown, r.RealizedDirection, r.RealizedReturn);
			}
		}

		private void btnRefresh_Click(object sender, EventArgs e)
		{
			this.RequestRefresh?.Invoke(this, EventArgs.Empty);
		}

		private void btnSave_Click(object sender, EventArgs e)
		{
			for (int i = 0; i < gridPlanned.Rows.Count; i++)
			{
				DataGridViewRow row = gridPlanned.Rows[i];
				if (row.Cells[0].Value != null)
				{
					bool en = Convert.ToBoolean(row.Cells[0].Value);
					if (i < _planned.Count)
					{
						_planned[i].Enabled = en;
					}
				}
			}
			MessageBox.Show("Planner updated. (Memory only until re-plan).", "Updated", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
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
			this.gridPlanned = new System.Windows.Forms.DataGridView();
			this.gridPreds = new System.Windows.Forms.DataGridView();
			this.btnRefresh = new System.Windows.Forms.Button();
			this.btnSave = new System.Windows.Forms.Button();
			((System.ComponentModel.ISupportInitialize)this.gridPlanned).BeginInit();
			((System.ComponentModel.ISupportInitialize)this.gridPreds).BeginInit();
			base.SuspendLayout();
			this.gridPlanned.AllowUserToAddRows = false;
			this.gridPlanned.AllowUserToDeleteRows = false;
			this.gridPlanned.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
			this.gridPlanned.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
			this.gridPlanned.Location = new System.Drawing.Point(12, 12);
			this.gridPlanned.Name = "gridPlanned";
			this.gridPlanned.RowHeadersVisible = false;
			this.gridPlanned.Size = new System.Drawing.Size(760, 200);
			this.gridPlanned.TabIndex = 0;
			this.gridPlanned.Columns.Add(new System.Windows.Forms.DataGridViewCheckBoxColumn
			{
				HeaderText = "Enabled",
				Width = 60
			});
			this.gridPlanned.Columns.Add("Exchange", "Exchange");
			this.gridPlanned.Columns.Add("Product", "Product");
			this.gridPlanned.Columns.Add("Strategy", "Strategy");
			this.gridPlanned.Columns.Add("Side", "Side");
			this.gridPlanned.Columns.Add("Qty", "Qty");
			this.gridPlanned.Columns.Add("Price", "Price");
			this.gridPlanned.Columns.Add("Edge", "Edge");
			this.gridPlanned.Columns.Add("Notes", "Notes");
			this.gridPreds.AllowUserToAddRows = false;
			this.gridPreds.AllowUserToDeleteRows = false;
			this.gridPreds.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
			this.gridPreds.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
			this.gridPreds.Location = new System.Drawing.Point(12, 252);
			this.gridPreds.Name = "gridPreds";
			this.gridPreds.RowHeadersVisible = false;
			this.gridPreds.Size = new System.Drawing.Size(760, 170);
			this.gridPreds.TabIndex = 1;
			this.gridPreds.Columns.Add("Product", "Product");
			this.gridPreds.Columns.Add("AtLocal", "At (Local)");
			this.gridPreds.Columns.Add("Horizon", "Horizon (min)");
			this.gridPreds.Columns.Add("Dir", "Dir (-1/0/1)");
			this.gridPreds.Columns.Add("Prob", "Prob");
			this.gridPreds.Columns.Add("ExpRet", "ExpRet");
			this.gridPreds.Columns.Add("ExpVol", "ExpVol");
			this.gridPreds.Columns.Add("Realized", "Realized?");
			this.gridPreds.Columns.Add("RealizedDir", "R Dir");
			this.gridPreds.Columns.Add("RealizedRet", "R Ret");
			this.btnRefresh.Location = new System.Drawing.Point(12, 218);
			this.btnRefresh.Name = "btnRefresh";
			this.btnRefresh.Size = new System.Drawing.Size(120, 28);
			this.btnRefresh.TabIndex = 2;
			this.btnRefresh.Text = "Refresh";
			this.btnRefresh.UseVisualStyleBackColor = true;
			this.btnRefresh.Click += new System.EventHandler(btnRefresh_Click);
			this.btnSave.Location = new System.Drawing.Point(138, 218);
			this.btnSave.Name = "btnSave";
			this.btnSave.Size = new System.Drawing.Size(120, 28);
			this.btnSave.TabIndex = 3;
			this.btnSave.Text = "Apply Changes";
			this.btnSave.UseVisualStyleBackColor = true;
			this.btnSave.Click += new System.EventHandler(btnSave_Click);
			base.ClientSize = new System.Drawing.Size(784, 434);
			base.Controls.Add(this.btnSave);
			base.Controls.Add(this.btnRefresh);
			base.Controls.Add(this.gridPreds);
			base.Controls.Add(this.gridPlanned);
			base.Name = "PlannerForm";
			this.Text = "Planner";
			((System.ComponentModel.ISupportInitialize)this.gridPlanned).EndInit();
			((System.ComponentModel.ISupportInitialize)this.gridPreds).EndInit();
			base.ResumeLayout(false);
		}

		protected override void OnLoad(EventArgs e)
		{
			base.OnLoad(e);
			BuildResponsivePlanner();
			MinimumSize = new Size(700, 400);
		}

		private void BuildResponsivePlanner()
		{
			if (gridPlanned != null && gridPreds != null)
			{
				_split = new SplitContainer();
				_split.Dock = DockStyle.Fill;
				_split.Orientation = Orientation.Vertical;
				_split.SplitterWidth = 6;
				_split.Panel1MinSize = 300;
				_split.Panel2MinSize = 300;
				_split.SplitterDistance = base.ClientSize.Width / 2;
				gridPlanned.Parent = _split.Panel1;
				gridPreds.Parent = _split.Panel2;
				gridPlanned.Dock = DockStyle.Fill;
				gridPreds.Dock = DockStyle.Fill;
				gridPlanned.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
				gridPreds.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
				if (btnRefresh != null)
				{
					btnRefresh.Parent = _split.Panel1;
					btnRefresh.Dock = DockStyle.Top;
				}
				if (btnSave != null)
				{
					btnSave.Parent = _split.Panel1;
					btnSave.Dock = DockStyle.Top;
				}
				base.Controls.Add(_split);
			}
		}
	}
}
