namespace CryptoDayTraderSuite.UI
{
    partial class GovernorWidget
    {
        private System.ComponentModel.IContainer components = null;
        private System.Windows.Forms.Label lblHeader;
        private System.Windows.Forms.Label lblBias;
        private System.Windows.Forms.Label lblReason;
        private System.Windows.Forms.Label lblStatus;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null)) components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.lblHeader = new System.Windows.Forms.Label();
            this.lblBias = new System.Windows.Forms.Label();
            this.lblReason = new System.Windows.Forms.Label();
            this.lblStatus = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // lblHeader
            // 
            this.lblHeader.AutoSize = true;
            this.lblHeader.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblHeader.ForeColor = System.Drawing.Color.Gray;
            this.lblHeader.Location = new System.Drawing.Point(4, 4);
            this.lblHeader.Name = "lblHeader";
            this.lblHeader.Size = new System.Drawing.Size(84, 13);
            this.lblHeader.TabIndex = 0;
            this.lblHeader.Text = "AI MARKET BIAS";
            // 
            // lblBias
            // 
            this.lblBias.AutoSize = true;
            this.lblBias.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblBias.ForeColor = System.Drawing.Color.White;
            this.lblBias.Location = new System.Drawing.Point(3, 20);
            this.lblBias.Name = "lblBias";
            this.lblBias.Size = new System.Drawing.Size(81, 21);
            this.lblBias.TabIndex = 1;
            this.lblBias.Text = "PENDING";
            // 
            // lblReason
            // 
            this.lblReason.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.lblReason.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblReason.ForeColor = System.Drawing.Color.Silver;
            this.lblReason.Location = new System.Drawing.Point(4, 50);
            this.lblReason.Name = "lblReason";
            this.lblReason.Size = new System.Drawing.Size(192, 90);
            this.lblReason.TabIndex = 2;
            this.lblReason.Text = "Waiting for analysis...";
            // 
            // lblStatus
            // 
            this.lblStatus.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.lblStatus.AutoSize = true;
            this.lblStatus.ForeColor = System.Drawing.Color.DimGray;
            this.lblStatus.Location = new System.Drawing.Point(4, 145);
            this.lblStatus.Name = "lblStatus";
            this.lblStatus.Size = new System.Drawing.Size(73, 13);
            this.lblStatus.TabIndex = 3;
            this.lblStatus.Text = "Disconnected";
            // 
            // GovernorWidget
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(30)))), ((int)(((byte)(32)))), ((int)(((byte)(38)))));
            this.Controls.Add(this.lblStatus);
            this.Controls.Add(this.lblReason);
            this.Controls.Add(this.lblBias);
            this.Controls.Add(this.lblHeader);
            this.Name = "GovernorWidget";
            this.Size = new System.Drawing.Size(200, 160);
            this.ResumeLayout(false);
            this.PerformLayout();
        }
    }
}
