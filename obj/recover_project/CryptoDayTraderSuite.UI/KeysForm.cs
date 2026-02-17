using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using CryptoDayTraderSuite.Services;

namespace CryptoDayTraderSuite.UI
{
	public class KeysForm : Form
	{
		private IContainer components = null;

		private KeysControl keysControl1;

		public KeysForm(IKeyService service)
			: this()
		{
			KeysControl control = null;
			foreach (Control c in base.Controls)
			{
				if (c is KeysControl kc)
				{
					control = kc;
					break;
				}
			}
			if (control == null)
			{
				control = new KeysControl
				{
					Dock = DockStyle.Fill
				};
				base.Controls.Add(control);
			}
			control.Initialize(service);
		}

		public KeysForm()
		{
			InitializeComponent();
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
			this.keysControl1 = new CryptoDayTraderSuite.UI.KeysControl();
			base.SuspendLayout();
			this.keysControl1.Dock = System.Windows.Forms.DockStyle.Fill;
			this.keysControl1.Location = new System.Drawing.Point(0, 0);
			this.keysControl1.Name = "keysControl1";
			this.keysControl1.Size = new System.Drawing.Size(800, 450);
			this.keysControl1.TabIndex = 0;
			base.AutoScaleDimensions = new System.Drawing.SizeF(6f, 13f);
			base.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			base.ClientSize = new System.Drawing.Size(800, 450);
			base.Controls.Add(this.keysControl1);
			base.Name = "KeysForm";
			this.Text = "API Keys";
			base.ResumeLayout(false);
		}
	}
}
