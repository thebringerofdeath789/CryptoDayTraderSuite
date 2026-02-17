/* File: UI/PlannerForm.Designer.cs */
/* Author: Gregory King */
/* Date: 2025-08-10 */
/* Description: designer for planner form compatible with .net 4.8 */
/* Functions: InitializeComponent, Dispose */

namespace CryptoDayTraderSuite.UI
{
    partial class PlannerForm
    {
        private System.ComponentModel.IContainer components = null;
        private System.Windows.Forms.DataGridView gridPlanned;
        private System.Windows.Forms.DataGridView gridPreds;
        private System.Windows.Forms.Button btnRefresh;
        private System.Windows.Forms.Button btnSave;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null)) { components.Dispose(); }
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.gridPlanned = new System.Windows.Forms.DataGridView();
            this.gridPreds = new System.Windows.Forms.DataGridView();
            this.btnRefresh = new System.Windows.Forms.Button();
            this.btnSave = new System.Windows.Forms.Button();
            ((System.ComponentModel.ISupportInitialize)(this.gridPlanned)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.gridPreds)).BeginInit();
            this.SuspendLayout();
            /* gridPlanned */
            this.gridPlanned.AllowUserToAddRows = false;
            this.gridPlanned.AllowUserToDeleteRows = false;
            this.gridPlanned.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.gridPlanned.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.gridPlanned.Location = new System.Drawing.Point(12, 12);
            this.gridPlanned.Name = "gridPlanned";
            this.gridPlanned.RowHeadersVisible = false;
            this.gridPlanned.Size = new System.Drawing.Size(760, 200);
            this.gridPlanned.TabIndex = 0;
            this.gridPlanned.Columns.Add(new System.Windows.Forms.DataGridViewCheckBoxColumn { HeaderText = "Enabled", Width = 60 });
            this.gridPlanned.Columns.Add("Exchange", "Exchange");
            this.gridPlanned.Columns.Add("Product", "Product");
            this.gridPlanned.Columns.Add("Strategy", "Strategy");
            this.gridPlanned.Columns.Add("Side", "Side");
            this.gridPlanned.Columns.Add("Qty", "Qty");
            this.gridPlanned.Columns.Add("Price", "Price");
            this.gridPlanned.Columns.Add("Edge", "Edge");
            this.gridPlanned.Columns.Add("Notes", "Notes");
            /* gridPreds */
            this.gridPreds.AllowUserToAddRows = false;
            this.gridPreds.AllowUserToDeleteRows = false;
            this.gridPreds.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
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
            /* btnRefresh */
            this.btnRefresh.Location = new System.Drawing.Point(12, 218);
            this.btnRefresh.Name = "btnRefresh";
            this.btnRefresh.Size = new System.Drawing.Size(120, 28);
            this.btnRefresh.TabIndex = 2;
            this.btnRefresh.Text = "Refresh";
            this.btnRefresh.UseVisualStyleBackColor = true;
            this.btnRefresh.Click += new System.EventHandler(this.btnRefresh_Click);
            /* btnSave */
            this.btnSave.Location = new System.Drawing.Point(138, 218);
            this.btnSave.Name = "btnSave";
            this.btnSave.Size = new System.Drawing.Size(120, 28);
            this.btnSave.TabIndex = 3;
            this.btnSave.Text = "Apply Changes";
            this.btnSave.UseVisualStyleBackColor = true;
            this.btnSave.Click += new System.EventHandler(this.btnSave_Click);
            /* PlannerForm */
            this.ClientSize = new System.Drawing.Size(784, 434);
            this.Controls.Add(this.btnSave);
            this.Controls.Add(this.btnRefresh);
            this.Controls.Add(this.gridPreds);
            this.Controls.Add(this.gridPlanned);
            this.Name = "PlannerForm";
            this.Text = "Planner";
            ((System.ComponentModel.ISupportInitialize)(this.gridPlanned)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.gridPreds)).EndInit();
            this.ResumeLayout(false);
        }
    }
}