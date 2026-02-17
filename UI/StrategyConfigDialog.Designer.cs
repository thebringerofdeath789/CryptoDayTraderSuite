namespace CryptoDayTraderSuite.UI
{
    partial class StrategyConfigDialog
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

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

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
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
            this.SuspendLayout();
            // 
            // layoutRoot
            // 
            this.layoutRoot.ColumnCount = 1;
            this.layoutRoot.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.layoutRoot.Controls.Add(this.layoutTop, 0, 0);
            this.layoutRoot.Controls.Add(this.propertyGrid, 0, 1);
            this.layoutRoot.Controls.Add(this.btnClose, 0, 2);
            this.layoutRoot.Dock = System.Windows.Forms.DockStyle.Fill;
            this.layoutRoot.Location = new System.Drawing.Point(0, 0);
            this.layoutRoot.Name = "layoutRoot";
            this.layoutRoot.Padding = new System.Windows.Forms.Padding(14);
            this.layoutRoot.RowCount = 3;
            this.layoutRoot.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
            this.layoutRoot.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.layoutRoot.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 48F));
            this.layoutRoot.Size = new System.Drawing.Size(500, 560);
            this.layoutRoot.TabIndex = 0;
            // 
            // layoutTop
            // 
            this.layoutTop.ColumnCount = 2;
            this.layoutTop.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 92F));
            this.layoutTop.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.layoutTop.Controls.Add(this.lblStrategy, 0, 0);
            this.layoutTop.Controls.Add(this.cmbStrategy, 1, 0);
            this.layoutTop.Dock = System.Windows.Forms.DockStyle.Fill;
            this.layoutTop.Location = new System.Drawing.Point(17, 17);
            this.layoutTop.Name = "layoutTop";
            this.layoutTop.RowCount = 1;
            this.layoutTop.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.layoutTop.Size = new System.Drawing.Size(466, 32);
            this.layoutTop.TabIndex = 0;
            // 
            // cmbStrategy
            // 
            this.cmbStrategy.Dock = System.Windows.Forms.DockStyle.Fill;
            this.cmbStrategy.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbStrategy.FormattingEnabled = true;
            this.cmbStrategy.Location = new System.Drawing.Point(95, 4);
            this.cmbStrategy.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.cmbStrategy.Name = "cmbStrategy";
            this.cmbStrategy.Size = new System.Drawing.Size(368, 21);
            this.cmbStrategy.TabIndex = 0;
            this.cmbStrategy.SelectedIndexChanged += new System.EventHandler(this.cmbStrategy_SelectedIndexChanged);
            // 
            // lblStrategy
            // 
            this.lblStrategy.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.lblStrategy.AutoSize = true;
            this.lblStrategy.Location = new System.Drawing.Point(3, 9);
            this.lblStrategy.Name = "lblStrategy";
            this.lblStrategy.Size = new System.Drawing.Size(49, 13);
            this.lblStrategy.TabIndex = 1;
            this.lblStrategy.Text = "Strategy:";
            // 
            // propertyGrid
            // 
            this.propertyGrid.Dock = System.Windows.Forms.DockStyle.Fill;
            this.propertyGrid.Location = new System.Drawing.Point(17, 55);
            this.propertyGrid.Name = "propertyGrid";
            this.propertyGrid.Size = new System.Drawing.Size(466, 440);
            this.propertyGrid.TabIndex = 2;
            this.propertyGrid.ToolbarVisible = false;
            // 
            // btnClose
            // 
            this.btnClose.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Right | System.Windows.Forms.AnchorStyles.Top)));
            this.btnClose.Location = new System.Drawing.Point(390, 509);
            this.btnClose.Margin = new System.Windows.Forms.Padding(3, 10, 3, 0);
            this.btnClose.Name = "btnClose";
            this.btnClose.Size = new System.Drawing.Size(93, 30);
            this.btnClose.TabIndex = 3;
            this.btnClose.Text = "Close";
            this.btnClose.UseVisualStyleBackColor = true;
            this.btnClose.Click += new System.EventHandler(this.btnClose_Click);
            // 
            // StrategyConfigDialog
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(500, 560);
            this.Controls.Add(this.layoutRoot);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "StrategyConfigDialog";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Configuration";
            this.layoutRoot.ResumeLayout(false);
            this.layoutTop.ResumeLayout(false);
            this.layoutTop.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.TableLayoutPanel layoutRoot;
        private System.Windows.Forms.TableLayoutPanel layoutTop;
        private System.Windows.Forms.ComboBox cmbStrategy;
        private System.Windows.Forms.Label lblStrategy;
        private System.Windows.Forms.PropertyGrid propertyGrid;
        private System.Windows.Forms.Button btnClose;
    }
}