using System;
using System.Drawing;
using System.Windows.Forms;
using CryptoDayTraderSuite.Models;
using CryptoDayTraderSuite.Services;

namespace CryptoDayTraderSuite.UI
{
	public class KeyEditDialog : Form
	{
		private string _id;

		private IKeyService _service;

		private ComboBox cmbService;

		private TextBox txtLabel;

		private TextBox txtApiKey;

		private TextBox txtSecret;

		private TextBox txtPass;

		private TextBox txtKeyName;

		private TextBox txtPem;

		private CheckBox chkActive;

		public KeyEditDialog(string id, IKeyService service)
		{
			_id = id;
			_service = service;
			BuildUi();
			if (!string.IsNullOrEmpty(id))
			{
				LoadExisting();
			}
		}

		private void BuildUi()
		{
			Text = (string.IsNullOrEmpty(_id) ? "Add Key" : "Edit Key");
			base.StartPosition = FormStartPosition.CenterParent;
			base.Width = 740;
			base.Height = 600;
			base.FormBorderStyle = FormBorderStyle.FixedDialog;
			base.MaximizeBox = false;
			base.MinimizeBox = false;
			base.Padding = new Padding(0);
			TableLayoutPanel tl = new TableLayoutPanel();
			tl.Dock = DockStyle.Fill;
			tl.ColumnCount = 2;
			tl.Padding = new Padding(14);
			tl.AutoScroll = true;
			tl.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 170f));
			tl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
			base.Controls.Add(tl);
			tl.Controls.Add(new Label
			{
				Text = "Service",
				AutoSize = true,
				Margin = new Padding(0, 8, 8, 8)
			}, 0, 0);
			cmbService = new ComboBox();
			cmbService.DropDownStyle = ComboBoxStyle.DropDownList;
			cmbService.Items.AddRange(new object[4] { "coinbase-exchange", "coinbase-advanced", "kraken", "bitstamp" });
			cmbService.Width = 280;
			cmbService.Margin = new Padding(3, 4, 3, 8);
			tl.Controls.Add(cmbService, 1, 0);
			tl.Controls.Add(new Label
			{
				Text = "Label",
				AutoSize = true,
				Margin = new Padding(0, 8, 8, 8)
			}, 0, 1);
			txtLabel = new TextBox();
			txtLabel.Dock = DockStyle.Fill;
			txtLabel.Margin = new Padding(3, 3, 3, 8);
			tl.Controls.Add(txtLabel, 1, 1);
			tl.Controls.Add(new Label
			{
				Text = "Active",
				AutoSize = true,
				Margin = new Padding(0, 8, 8, 8)
			}, 0, 2);
			chkActive = new CheckBox();
			chkActive.Margin = new Padding(3, 5, 3, 8);
			tl.Controls.Add(chkActive, 1, 2);
			Label sep1 = new Label
			{
				Text = "Coinbase Exchange Credentials",
				AutoSize = true,
				Margin = new Padding(0, 16, 8, 8)
			};
			sep1.Font = new Font(sep1.Font, FontStyle.Bold);
			tl.Controls.Add(sep1, 0, 3);
			tl.SetColumnSpan(sep1, 2);
			tl.Controls.Add(new Label
			{
				Text = "API Key",
				AutoSize = true,
				Margin = new Padding(0, 8, 8, 8)
			}, 0, 4);
			txtApiKey = new TextBox();
			txtApiKey.Dock = DockStyle.Fill;
			txtApiKey.Margin = new Padding(3, 3, 3, 8);
			tl.Controls.Add(txtApiKey, 1, 4);
			tl.Controls.Add(new Label
			{
				Text = "API Secret (base64)",
				AutoSize = true,
				Margin = new Padding(0, 8, 8, 8)
			}, 0, 5);
			txtSecret = new TextBox();
			txtSecret.Dock = DockStyle.Fill;
			txtSecret.Margin = new Padding(3, 3, 3, 8);
			tl.Controls.Add(txtSecret, 1, 5);
			tl.Controls.Add(new Label
			{
				Text = "Passphrase",
				AutoSize = true,
				Margin = new Padding(0, 8, 8, 8)
			}, 0, 6);
			txtPass = new TextBox();
			txtPass.Dock = DockStyle.Fill;
			txtPass.Margin = new Padding(3, 3, 3, 8);
			tl.Controls.Add(txtPass, 1, 6);
			Label sep2 = new Label
			{
				Text = "Coinbase Advanced Credentials",
				AutoSize = true,
				Margin = new Padding(0, 16, 8, 8)
			};
			sep2.Font = new Font(sep2.Font, FontStyle.Bold);
			tl.Controls.Add(sep2, 0, 7);
			tl.SetColumnSpan(sep2, 2);
			tl.Controls.Add(new Label
			{
				Text = "API Key Name",
				AutoSize = true,
				Margin = new Padding(0, 8, 8, 8)
			}, 0, 8);
			txtKeyName = new TextBox();
			txtKeyName.Dock = DockStyle.Fill;
			txtKeyName.Margin = new Padding(3, 3, 3, 8);
			tl.Controls.Add(txtKeyName, 1, 8);
			tl.Controls.Add(new Label
			{
				Text = "EC Private Key (PEM)",
				AutoSize = true,
				Margin = new Padding(0, 8, 8, 8)
			}, 0, 9);
			txtPem = new TextBox();
			txtPem.Multiline = true;
			txtPem.Height = 140;
			txtPem.Dock = DockStyle.Fill;
			txtPem.ScrollBars = ScrollBars.Vertical;
			txtPem.WordWrap = false;
			txtPem.Margin = new Padding(3, 3, 3, 10);
			tl.Controls.Add(txtPem, 1, 9);
			tl.RowStyles.Clear();
			for (int row = 0; row < 9; row++)
			{
				tl.RowStyles.Add(new RowStyle(SizeType.AutoSize));
			}
			tl.RowStyles.Add(new RowStyle(SizeType.Absolute, 156f));
			FlowLayoutPanel bar = new FlowLayoutPanel();
			bar.Dock = DockStyle.Bottom;
			bar.FlowDirection = FlowDirection.RightToLeft;
			bar.Padding = new Padding(12, 10, 12, 10);
			bar.Height = 54;
			Button btnSave = new Button
			{
				Text = "Save"
			};
			btnSave.Click += delegate
			{
				SaveClicked();
			};
			Button btnCancel = new Button
			{
				Text = "Cancel"
			};
			btnCancel.Click += delegate
			{
				base.DialogResult = DialogResult.Cancel;
			};
			btnSave.Size = new Size(84, 28);
			btnCancel.Size = new Size(84, 28);
			bar.Controls.Add(btnSave);
			bar.Controls.Add(btnCancel);
			base.Controls.Add(bar);
			DialogTheme.Apply(this);
		}

		private void LoadExisting()
		{
			if (_service == null)
			{
				return;
			}
			KeyInfo k = _service.Get(_id);
			if (k == null)
			{
				return;
			}
			cmbService.SelectedItem = k.Service;
			txtLabel.Text = k.Label;
			chkActive.Checked = k.Active;
			string v;
			try
			{
				if (k.Data.TryGetValue("ApiKey", out v))
				{
					txtApiKey.Text = _service.Unprotect(v);
				}
			}
			catch
			{
				txtApiKey.Text = "";
			}
			try
			{
				if (k.Data.TryGetValue("ApiSecretBase64", out v))
				{
					txtSecret.Text = _service.Unprotect(v);
				}
			}
			catch
			{
				txtSecret.Text = "";
			}
			try
			{
				if (k.Data.TryGetValue("Passphrase", out v))
				{
					txtPass.Text = _service.Unprotect(v);
				}
			}
			catch
			{
				txtPass.Text = "";
			}
			if (k.Data.TryGetValue("ApiKeyName", out v))
			{
				txtKeyName.Text = v;
			}
			try
			{
				if (k.Data.TryGetValue("ECPrivateKeyPem", out v))
				{
					txtPem.Text = _service.Unprotect(v);
				}
			}
			catch
			{
				txtPem.Text = "";
			}
		}

		private void SaveClicked()
		{
			if (_service == null)
			{
				MessageBox.Show("service unavailable");
				return;
			}
			if (cmbService.SelectedItem == null)
			{
				MessageBox.Show("service required");
				return;
			}
			if (string.IsNullOrWhiteSpace(txtLabel.Text))
			{
				MessageBox.Show("label required");
				return;
			}
			KeyInfo info = _service.Get(_id);
			KeyEntry k = ((string.IsNullOrEmpty(_id) || info == null) ? new KeyEntry
			{
				Id = Guid.NewGuid().ToString()
			} : ((KeyEntry)info));
			if (k.CreatedUtc == default(DateTime))
			{
				k.CreatedUtc = DateTime.UtcNow;
			}
			k.Service = cmbService.SelectedItem.ToString();
			k.Label = txtLabel.Text.Trim();
			k.Active = chkActive.Checked;
			k.Enabled = true;
			k.UpdatedUtc = DateTime.UtcNow;
			k.Data["ApiKey"] = _service.Protect(txtApiKey.Text.Trim());
			k.Data["ApiSecretBase64"] = _service.Protect(txtSecret.Text.Trim());
			k.Data["Passphrase"] = _service.Protect(txtPass.Text.Trim());
			k.Data["ApiKeyName"] = txtKeyName.Text.Trim();
			k.Data["ECPrivateKeyPem"] = _service.Protect(txtPem.Text);
			_service.Upsert((KeyInfo)k);
			if (k.Active)
			{
				_service.SetActive(k.Service, k.Label);
			}
			base.DialogResult = DialogResult.OK;
		}

		private void InitializeComponent()
		{
		}
	}
}
