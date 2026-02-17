using System;
using System.Windows.Forms;
using CryptoDayTraderSuite.Services;

namespace CryptoDayTraderSuite.UI
{
    public partial class StatusForm : Form
    {
        private StatusControl _control;

        public StatusForm(IHistoryService historyService)
        {
            InitializeComponent();
            _control = new StatusControl();
            _control.Dock = DockStyle.Fill;
            this.Controls.Add(_control);
            _control.Initialize(historyService);
        }

        public StatusForm()
        {
            InitializeComponent();
            // Designer/legacy constructor intentionally initializes only shell form state.
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(800, 450);
            this.Text = "Status & Projections";
            this.ResumeLayout(false);
        }
    }
}