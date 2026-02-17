using System.Drawing;
using System.Windows.Forms;
using CryptoDayTraderSuite.Services;

namespace CryptoDayTraderSuite.UI
{
	public class StatusForm : Form
	{
		private StatusControl _control;

		public StatusForm(IHistoryService historyService)
		{
			InitializeComponent();
			_control = new StatusControl();
			_control.Dock = DockStyle.Fill;
			base.Controls.Add(_control);
			_control.Initialize(historyService);
		}

		public StatusForm()
		{
			InitializeComponent();
		}

		private void InitializeComponent()
		{
			base.SuspendLayout();
			base.AutoScaleDimensions = new System.Drawing.SizeF(6f, 13f);
			base.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			base.ClientSize = new System.Drawing.Size(800, 450);
			this.Text = "Status & Projections";
			base.ResumeLayout(false);
		}
	}
}
