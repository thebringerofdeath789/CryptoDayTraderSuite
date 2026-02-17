using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using CryptoDayTraderSuite.Models;
using CryptoDayTraderSuite.Services;

namespace CryptoDayTraderSuite.UI
{
	public class AccountEditDialog : Form
	{
		private sealed class KeyOption
		{
			public string Id;

			public string Label;

			public override string ToString()
			{
				return Label;
			}
		}

		private readonly string _id;

		private readonly IAccountService _accountService;

		private readonly IKeyService _keyService;

		private IContainer components = null;

		private TableLayoutPanel mainLayout;

		private Label lblLabel;

		private Label lblService;

		private Label lblMode;

		private Label lblRisk;

		private Label lblMax;

		private Label lblCredHeader;

		private FlowLayoutPanel actionPanel;

		private Button btnCancel;

		private Button btnSave;

		private TextBox txtLabel;

		private ComboBox cmbService;

		private ComboBox cmbMode;

		private NumericUpDown numRisk;

		private NumericUpDown numMax;

		private TextBox txtKeyId;

		private Label lblExistingKey;

		private ComboBox cmbExistingKey;

		private TextBox txtKeyLabel;

		private TextBox txtApiKey;

		private TextBox txtSecret;

		private TextBox txtPassphrase;

		private TextBox txtApiKeyName;

		private TextBox txtPem;

		private Label lblKeyId;

		private Label lblKeyLabel;

		private Label lblApiKey;

		private Label lblSecret;

		private Label lblPassphrase;

		private Label lblApiKeyName;

		private Label lblPem;

		public AccountEditDialog(string id, IAccountService accountService, IKeyService keyService = null)
		{
			_id = id;
			_accountService = accountService ?? throw new ArgumentNullException("accountService");
			_keyService = keyService;
			InitializeComponent();
			DialogTheme.Apply(this);
			Text = (string.IsNullOrEmpty(_id) ? "Add Account" : "Edit Account");
			if (cmbService.Items.Count > 0)
			{
				cmbService.SelectedIndex = 0;
			}
			if (cmbMode.Items.Count > 0)
			{
				cmbMode.SelectedIndex = 0;
			}
			if (!string.IsNullOrEmpty(id))
			{
				LoadExisting();
			}
			PopulateExistingKeys();
			AttachRealtimeValidationHandlers();
			UpdateCredentialFieldVisibility();
			UpdateSaveButtonState();
		}

		private void AttachRealtimeValidationHandlers()
		{
			txtLabel.TextChanged += InputStateChanged;
			cmbMode.SelectedIndexChanged += InputStateChanged;
			cmbExistingKey.SelectedIndexChanged += InputStateChanged;
			txtApiKey.TextChanged += InputStateChanged;
			txtSecret.TextChanged += InputStateChanged;
			txtPassphrase.TextChanged += InputStateChanged;
			txtApiKeyName.TextChanged += InputStateChanged;
			txtPem.TextChanged += InputStateChanged;
		}

		private void InputStateChanged(object sender, EventArgs e)
		{
			UpdateSaveButtonState();
		}

		private void UpdateSaveButtonState()
		{
			if (btnSave != null)
			{
				btnSave.Enabled = CanSaveWithCurrentInputs();
			}
		}

		private bool CanSaveWithCurrentInputs()
		{
			if (string.IsNullOrWhiteSpace(txtLabel.Text))
			{
				return false;
			}
			if (cmbService.SelectedItem == null)
			{
				return false;
			}
			if (cmbMode.SelectedItem == null)
			{
				return false;
			}
			string service = cmbService.SelectedItem.ToString();
			if (_keyService == null)
			{
				return true;
			}
			if (IsPaperService(service))
			{
				return true;
			}
			string selectedExistingKeyId = GetSelectedExistingKeyId();
			bool hasCredentialEdits = HasCredentialInputs();
			if (string.IsNullOrWhiteSpace(selectedExistingKeyId) && !hasCredentialEdits)
			{
				return false;
			}
			if (!string.IsNullOrWhiteSpace(selectedExistingKeyId) && !hasCredentialEdits)
			{
				return true;
			}
			if (IsCoinbaseAdvanced(service))
			{
				return !string.IsNullOrWhiteSpace(txtApiKeyName.Text) && !string.IsNullOrWhiteSpace(txtPem.Text);
			}
			return !string.IsNullOrWhiteSpace(txtApiKey.Text) && !string.IsNullOrWhiteSpace(txtSecret.Text);
		}

		private void LoadExisting()
		{
			AccountInfo a = _accountService.Get(_id);
			if (a == null)
			{
				return;
			}
			txtLabel.Text = a.Label;
			if (!string.IsNullOrWhiteSpace(a.Service) && cmbService.Items.Contains(a.Service))
			{
				cmbService.SelectedItem = a.Service;
			}
			else if (cmbService.Items.Count > 0)
			{
				cmbService.SelectedIndex = 0;
			}
			string modeText = a.Mode.ToString();
			if (!string.IsNullOrWhiteSpace(modeText) && cmbMode.Items.Contains(modeText))
			{
				cmbMode.SelectedItem = modeText;
			}
			else if (cmbMode.Items.Count > 0)
			{
				cmbMode.SelectedIndex = 0;
			}
			numRisk.Value = a.RiskPerTradePct;
			numMax.Value = a.MaxConcurrentTrades;
			txtKeyId.Text = a.KeyEntryId;
			PopulateExistingKeys(a.KeyEntryId);
			if (_keyService != null && !string.IsNullOrWhiteSpace(a.KeyEntryId))
			{
				KeyInfo existing = _keyService.Get(a.KeyEntryId);
				if (existing != null)
				{
					txtKeyLabel.Text = existing.Label;
					txtApiKey.Text = SafeUnprotect(existing.ApiKey);
					txtSecret.Text = SafeUnprotect((!string.IsNullOrWhiteSpace(existing.ApiSecretBase64)) ? existing.ApiSecretBase64 : existing.Secret);
					txtPassphrase.Text = SafeUnprotect(existing.Passphrase);
					txtApiKeyName.Text = existing.ApiKeyName ?? string.Empty;
					txtPem.Text = SafeUnprotect(existing.ECPrivateKeyPem);
				}
			}
		}

		private void SaveClicked()
		{
			if (string.IsNullOrWhiteSpace(txtLabel.Text))
			{
				MessageBox.Show("label required");
				return;
			}
			if (cmbService.SelectedItem == null)
			{
				MessageBox.Show("service required");
				return;
			}
			if (cmbMode.SelectedItem == null)
			{
				MessageBox.Show("mode required");
				return;
			}
			AccountInfo a = ((!string.IsNullOrEmpty(_id)) ? (_accountService.Get(_id) ?? new AccountInfo
			{
				Id = Guid.NewGuid().ToString(),
				CreatedUtc = DateTime.UtcNow
			}) : new AccountInfo
			{
				Id = Guid.NewGuid().ToString(),
				CreatedUtc = DateTime.UtcNow
			});
			a.Label = txtLabel.Text.Trim();
			a.Service = cmbService.SelectedItem.ToString();
			Enum.TryParse<AccountMode>(cmbMode.SelectedItem.ToString(), ignoreCase: true, out var m);
			a.Mode = m;
			a.RiskPerTradePct = numRisk.Value;
			a.MaxConcurrentTrades = (int)numMax.Value;
			string selectedService = cmbService.SelectedItem.ToString();
			string keyEntryId = BuildKeyEntryId(selectedService, txtLabel.Text.Trim(), a.KeyEntryId);
			if (keyEntryId != null)
			{
				a.KeyEntryId = keyEntryId;
				a.Enabled = true;
				a.UpdatedUtc = DateTime.UtcNow;
				_accountService.Upsert(a);
				base.DialogResult = DialogResult.OK;
			}
		}

		private string BuildKeyEntryId(string service, string accountLabel, string existingKeyId)
		{
			if (_keyService == null)
			{
				return txtKeyId.Text.Trim();
			}
			if (IsPaperService(service))
			{
				return string.Empty;
			}
			string selectedExistingKeyId = GetSelectedExistingKeyId();
			bool hasCredentialEdits = HasCredentialInputs();
			if (string.IsNullOrWhiteSpace(selectedExistingKeyId) && !hasCredentialEdits)
			{
				MessageBox.Show("Select an Existing API Key or enter new credentials before saving this non-paper account.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
				return null;
			}
			if (!string.IsNullOrWhiteSpace(selectedExistingKeyId) && !hasCredentialEdits)
			{
				txtKeyId.Text = selectedExistingKeyId;
				return selectedExistingKeyId;
			}
			if (IsCoinbaseAdvanced(service))
			{
				if (string.IsNullOrWhiteSpace(txtApiKeyName.Text) || string.IsNullOrWhiteSpace(txtPem.Text))
				{
					MessageBox.Show("Coinbase Advanced requires API Key Name and EC Private Key (PEM).", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
					return null;
				}
			}
			else if (string.IsNullOrWhiteSpace(txtApiKey.Text) || string.IsNullOrWhiteSpace(txtSecret.Text))
			{
				MessageBox.Show("This exchange requires API Key and API Secret.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
				return null;
			}
			string keyLabel = txtKeyLabel.Text.Trim();
			if (string.IsNullOrWhiteSpace(keyLabel))
			{
				keyLabel = accountLabel;
			}
			if (string.IsNullOrWhiteSpace(keyLabel))
			{
				keyLabel = "Default";
			}
			string preferredExistingId = ((!string.IsNullOrWhiteSpace(selectedExistingKeyId)) ? selectedExistingKeyId : existingKeyId);
			KeyInfo existing = ((!string.IsNullOrWhiteSpace(preferredExistingId)) ? _keyService.Get(preferredExistingId) : null);
			if (existing == null)
			{
				existing = _keyService.Get(service, keyLabel);
			}
			if (existing == null)
			{
				existing = new KeyInfo
				{
					Broker = service,
					Label = keyLabel,
					CreatedUtc = DateTime.UtcNow,
					Enabled = true
				};
			}
			existing.Broker = service;
			existing.Service = service;
			existing.Label = keyLabel;
			existing.Enabled = true;
			existing.ApiKey = _keyService.Protect(txtApiKey.Text.Trim());
			existing.ApiSecretBase64 = _keyService.Protect(txtSecret.Text.Trim());
			existing.Secret = existing.ApiSecretBase64;
			existing.Passphrase = _keyService.Protect(txtPassphrase.Text.Trim());
			existing.ApiKeyName = txtApiKeyName.Text.Trim();
			existing.ECPrivateKeyPem = _keyService.Protect(txtPem.Text);
			_keyService.Upsert(existing);
			string keyId = KeyEntry.MakeId(service, keyLabel);
			txtKeyId.Text = keyId;
			return keyId;
		}

		private void UpdateCredentialFieldVisibility()
		{
			string service = ((cmbService != null && cmbService.SelectedItem != null) ? cmbService.SelectedItem.ToString() : string.Empty);
			bool isPaper = IsPaperService(service);
			bool isAdvanced = IsCoinbaseAdvanced(service);
			bool usingKeyService = _keyService != null;
			bool showCredentialSection = usingKeyService && !isPaper;
			lblExistingKey.Visible = showCredentialSection;
			cmbExistingKey.Visible = showCredentialSection;
			lblCredHeader.Visible = showCredentialSection;
			lblKeyId.Visible = !usingKeyService || isPaper;
			txtKeyId.Visible = !usingKeyService || isPaper;
			lblKeyLabel.Visible = showCredentialSection;
			txtKeyLabel.Visible = showCredentialSection;
			lblApiKey.Visible = showCredentialSection && !isAdvanced;
			txtApiKey.Visible = showCredentialSection && !isAdvanced;
			lblSecret.Visible = showCredentialSection && !isAdvanced;
			txtSecret.Visible = showCredentialSection && !isAdvanced;
			lblPassphrase.Visible = showCredentialSection && !isAdvanced;
			txtPassphrase.Visible = showCredentialSection && !isAdvanced;
			lblApiKeyName.Visible = showCredentialSection && isAdvanced;
			txtApiKeyName.Visible = showCredentialSection && isAdvanced;
			lblPem.Visible = showCredentialSection && isAdvanced;
			txtPem.Visible = showCredentialSection && isAdvanced;
			if (usingKeyService && !isPaper && string.IsNullOrWhiteSpace(txtKeyLabel.Text) && !string.IsNullOrWhiteSpace(txtLabel.Text))
			{
				txtKeyLabel.Text = txtLabel.Text.Trim();
			}
			UpdateSaveButtonState();
		}

		private void PopulateExistingKeys(string selectedKeyId = null)
		{
			if (cmbExistingKey == null)
			{
				return;
			}
			cmbExistingKey.BeginUpdate();
			try
			{
				cmbExistingKey.Items.Clear();
				cmbExistingKey.Items.Add(new KeyOption
				{
					Id = string.Empty,
					Label = "(Create / Update with fields below)"
				});
				string service = ((cmbService != null && cmbService.SelectedItem != null) ? cmbService.SelectedItem.ToString() : string.Empty);
				if (_keyService != null && !string.IsNullOrWhiteSpace(service) && !IsPaperService(service))
				{
					List<KeyInfo> keys = _keyService.GetAll();
					foreach (KeyInfo key in keys)
					{
						if (key != null && string.Equals(key.Broker ?? key.Service ?? string.Empty, service, StringComparison.OrdinalIgnoreCase) && key.Enabled)
						{
							string keyId = KeyEntry.MakeId(service, key.Label ?? string.Empty);
							cmbExistingKey.Items.Add(new KeyOption
							{
								Id = keyId,
								Label = (key.Label ?? "(unnamed)") + " [" + service + "]"
							});
						}
					}
				}
				string targetId = (string.IsNullOrWhiteSpace(selectedKeyId) ? txtKeyId.Text.Trim() : selectedKeyId.Trim());
				int selectedIndex = 0;
				if (!string.IsNullOrWhiteSpace(targetId))
				{
					for (int i = 0; i < cmbExistingKey.Items.Count; i++)
					{
						if (cmbExistingKey.Items[i] is KeyOption opt && string.Equals(opt.Id, targetId, StringComparison.OrdinalIgnoreCase))
						{
							selectedIndex = i;
							break;
						}
					}
				}
				if (cmbExistingKey.Items.Count > 0)
				{
					cmbExistingKey.SelectedIndex = selectedIndex;
				}
			}
			finally
			{
				cmbExistingKey.EndUpdate();
			}
		}

		private string GetSelectedExistingKeyId()
		{
			KeyOption selected = ((cmbExistingKey != null) ? (cmbExistingKey.SelectedItem as KeyOption) : null);
			return (selected != null) ? (selected.Id ?? string.Empty).Trim() : string.Empty;
		}

		private bool HasCredentialInputs()
		{
			if (!string.IsNullOrWhiteSpace(txtApiKey.Text))
			{
				return true;
			}
			if (!string.IsNullOrWhiteSpace(txtSecret.Text))
			{
				return true;
			}
			if (!string.IsNullOrWhiteSpace(txtPassphrase.Text))
			{
				return true;
			}
			if (!string.IsNullOrWhiteSpace(txtApiKeyName.Text))
			{
				return true;
			}
			if (!string.IsNullOrWhiteSpace(txtPem.Text))
			{
				return true;
			}
			return false;
		}

		private static bool IsPaperService(string service)
		{
			return string.Equals(service ?? string.Empty, "paper", StringComparison.OrdinalIgnoreCase);
		}

		private static bool IsCoinbaseAdvanced(string service)
		{
			return string.Equals(service ?? string.Empty, "coinbase-advanced", StringComparison.OrdinalIgnoreCase);
		}

		private string SafeUnprotect(string value)
		{
			if (string.IsNullOrWhiteSpace(value) || _keyService == null)
			{
				return string.Empty;
			}
			try
			{
				return _keyService.Unprotect(value);
			}
			catch
			{
				return string.Empty;
			}
		}

		private void cmbService_SelectedIndexChanged(object sender, EventArgs e)
		{
			PopulateExistingKeys();
			UpdateCredentialFieldVisibility();
			UpdateSaveButtonState();
		}

		private void cmbExistingKey_SelectedIndexChanged(object sender, EventArgs e)
		{
			string keyId = GetSelectedExistingKeyId();
			if (!string.IsNullOrWhiteSpace(keyId) && _keyService != null)
			{
				KeyInfo key = _keyService.Get(keyId);
				if (key != null)
				{
					txtKeyId.Text = keyId;
					txtKeyLabel.Text = key.Label ?? txtKeyLabel.Text;
					txtApiKey.Text = SafeUnprotect(key.ApiKey);
					txtSecret.Text = SafeUnprotect((!string.IsNullOrWhiteSpace(key.ApiSecretBase64)) ? key.ApiSecretBase64 : key.Secret);
					txtPassphrase.Text = SafeUnprotect(key.Passphrase);
					txtApiKeyName.Text = key.ApiKeyName ?? string.Empty;
					txtPem.Text = SafeUnprotect(key.ECPrivateKeyPem);
					UpdateSaveButtonState();
				}
			}
		}

		private void btnSave_Click(object sender, EventArgs e)
		{
			SaveClicked();
		}

		private void btnCancel_Click(object sender, EventArgs e)
		{
			base.DialogResult = DialogResult.Cancel;
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
			this.mainLayout = new System.Windows.Forms.TableLayoutPanel();
			this.lblLabel = new System.Windows.Forms.Label();
			this.txtLabel = new System.Windows.Forms.TextBox();
			this.lblService = new System.Windows.Forms.Label();
			this.cmbService = new System.Windows.Forms.ComboBox();
			this.lblMode = new System.Windows.Forms.Label();
			this.cmbMode = new System.Windows.Forms.ComboBox();
			this.lblRisk = new System.Windows.Forms.Label();
			this.numRisk = new System.Windows.Forms.NumericUpDown();
			this.lblMax = new System.Windows.Forms.Label();
			this.numMax = new System.Windows.Forms.NumericUpDown();
			this.lblKeyId = new System.Windows.Forms.Label();
			this.txtKeyId = new System.Windows.Forms.TextBox();
			this.lblExistingKey = new System.Windows.Forms.Label();
			this.cmbExistingKey = new System.Windows.Forms.ComboBox();
			this.lblCredHeader = new System.Windows.Forms.Label();
			this.lblKeyLabel = new System.Windows.Forms.Label();
			this.txtKeyLabel = new System.Windows.Forms.TextBox();
			this.lblApiKey = new System.Windows.Forms.Label();
			this.txtApiKey = new System.Windows.Forms.TextBox();
			this.lblSecret = new System.Windows.Forms.Label();
			this.txtSecret = new System.Windows.Forms.TextBox();
			this.lblPassphrase = new System.Windows.Forms.Label();
			this.txtPassphrase = new System.Windows.Forms.TextBox();
			this.lblApiKeyName = new System.Windows.Forms.Label();
			this.txtApiKeyName = new System.Windows.Forms.TextBox();
			this.lblPem = new System.Windows.Forms.Label();
			this.txtPem = new System.Windows.Forms.TextBox();
			this.actionPanel = new System.Windows.Forms.FlowLayoutPanel();
			this.btnCancel = new System.Windows.Forms.Button();
			this.btnSave = new System.Windows.Forms.Button();
			this.mainLayout.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)this.numRisk).BeginInit();
			((System.ComponentModel.ISupportInitialize)this.numMax).BeginInit();
			this.actionPanel.SuspendLayout();
			base.SuspendLayout();
			this.mainLayout.AutoScroll = true;
			this.mainLayout.ColumnCount = 2;
			this.mainLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 148f));
			this.mainLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100f));
			this.mainLayout.Controls.Add(this.lblLabel, 0, 0);
			this.mainLayout.Controls.Add(this.txtLabel, 1, 0);
			this.mainLayout.Controls.Add(this.lblService, 0, 1);
			this.mainLayout.Controls.Add(this.cmbService, 1, 1);
			this.mainLayout.Controls.Add(this.lblMode, 0, 2);
			this.mainLayout.Controls.Add(this.cmbMode, 1, 2);
			this.mainLayout.Controls.Add(this.lblRisk, 0, 3);
			this.mainLayout.Controls.Add(this.numRisk, 1, 3);
			this.mainLayout.Controls.Add(this.lblMax, 0, 4);
			this.mainLayout.Controls.Add(this.numMax, 1, 4);
			this.mainLayout.Controls.Add(this.lblKeyId, 0, 5);
			this.mainLayout.Controls.Add(this.txtKeyId, 1, 5);
			this.mainLayout.Controls.Add(this.lblExistingKey, 0, 6);
			this.mainLayout.Controls.Add(this.cmbExistingKey, 1, 6);
			this.mainLayout.Controls.Add(this.lblCredHeader, 0, 7);
			this.mainLayout.Controls.Add(this.lblKeyLabel, 0, 8);
			this.mainLayout.Controls.Add(this.txtKeyLabel, 1, 8);
			this.mainLayout.Controls.Add(this.lblApiKey, 0, 9);
			this.mainLayout.Controls.Add(this.txtApiKey, 1, 9);
			this.mainLayout.Controls.Add(this.lblSecret, 0, 10);
			this.mainLayout.Controls.Add(this.txtSecret, 1, 10);
			this.mainLayout.Controls.Add(this.lblPassphrase, 0, 11);
			this.mainLayout.Controls.Add(this.txtPassphrase, 1, 11);
			this.mainLayout.Controls.Add(this.lblApiKeyName, 0, 12);
			this.mainLayout.Controls.Add(this.txtApiKeyName, 1, 12);
			this.mainLayout.Controls.Add(this.lblPem, 0, 13);
			this.mainLayout.Controls.Add(this.txtPem, 1, 13);
			this.mainLayout.Dock = System.Windows.Forms.DockStyle.Fill;
			this.mainLayout.Location = new System.Drawing.Point(0, 0);
			this.mainLayout.Name = "mainLayout";
			this.mainLayout.Padding = new System.Windows.Forms.Padding(14);
			this.mainLayout.RowCount = 14;
			this.mainLayout.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.mainLayout.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.mainLayout.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.mainLayout.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.mainLayout.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.mainLayout.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.mainLayout.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.mainLayout.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.mainLayout.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.mainLayout.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.mainLayout.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.mainLayout.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.mainLayout.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.mainLayout.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.mainLayout.Size = new System.Drawing.Size(704, 525);
			this.mainLayout.TabIndex = 0;
			this.lblLabel.Anchor = System.Windows.Forms.AnchorStyles.Left;
			this.lblLabel.AutoSize = true;
			this.lblLabel.Location = new System.Drawing.Point(17, 17);
			this.lblLabel.Name = "lblLabel";
			this.lblLabel.Size = new System.Drawing.Size(33, 13);
			this.lblLabel.TabIndex = 0;
			this.lblLabel.Text = "Label";
			this.txtLabel.Anchor = System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
			this.txtLabel.Location = new System.Drawing.Point(165, 14);
			this.txtLabel.Margin = new System.Windows.Forms.Padding(3, 3, 3, 8);
			this.txtLabel.Name = "txtLabel";
			this.txtLabel.Size = new System.Drawing.Size(522, 20);
			this.txtLabel.TabIndex = 1;
			this.lblService.Anchor = System.Windows.Forms.AnchorStyles.Left;
			this.lblService.AutoSize = true;
			this.lblService.Location = new System.Drawing.Point(17, 48);
			this.lblService.Name = "lblService";
			this.lblService.Size = new System.Drawing.Size(43, 13);
			this.lblService.TabIndex = 2;
			this.lblService.Text = "Service";
			this.cmbService.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.cmbService.FormattingEnabled = true;
			this.cmbService.Items.AddRange(new object[5] { "paper", "coinbase-exchange", "coinbase-advanced", "kraken", "bitstamp" });
			this.cmbService.Location = new System.Drawing.Point(165, 45);
			this.cmbService.Margin = new System.Windows.Forms.Padding(3, 3, 3, 8);
			this.cmbService.Name = "cmbService";
			this.cmbService.Size = new System.Drawing.Size(260, 21);
			this.cmbService.TabIndex = 3;
			this.cmbService.SelectedIndexChanged += new System.EventHandler(cmbService_SelectedIndexChanged);
			this.lblMode.Anchor = System.Windows.Forms.AnchorStyles.Left;
			this.lblMode.AutoSize = true;
			this.lblMode.Location = new System.Drawing.Point(17, 77);
			this.lblMode.Name = "lblMode";
			this.lblMode.Size = new System.Drawing.Size(34, 13);
			this.lblMode.TabIndex = 4;
			this.lblMode.Text = "Mode";
			this.cmbMode.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.cmbMode.FormattingEnabled = true;
			this.cmbMode.Items.AddRange(new object[2] { "Paper", "Live" });
			this.cmbMode.Location = new System.Drawing.Point(165, 74);
			this.cmbMode.Margin = new System.Windows.Forms.Padding(3, 3, 3, 8);
			this.cmbMode.Name = "cmbMode";
			this.cmbMode.Size = new System.Drawing.Size(260, 21);
			this.cmbMode.TabIndex = 5;
			this.lblRisk.Anchor = System.Windows.Forms.AnchorStyles.Left;
			this.lblRisk.AutoSize = true;
			this.lblRisk.Location = new System.Drawing.Point(17, 106);
			this.lblRisk.Name = "lblRisk";
			this.lblRisk.Size = new System.Drawing.Size(79, 13);
			this.lblRisk.TabIndex = 6;
			this.lblRisk.Text = "Risk per trade %";
			this.numRisk.DecimalPlaces = 2;
			this.numRisk.Location = new System.Drawing.Point(165, 103);
			this.numRisk.Margin = new System.Windows.Forms.Padding(3, 3, 3, 8);
			this.numRisk.Name = "numRisk";
			this.numRisk.Size = new System.Drawing.Size(120, 20);
			this.numRisk.TabIndex = 7;
			this.numRisk.Value = new decimal(new int[4] { 50, 0, 0, 131072 });
			this.lblMax.Anchor = System.Windows.Forms.AnchorStyles.Left;
			this.lblMax.AutoSize = true;
			this.lblMax.Location = new System.Drawing.Point(17, 135);
			this.lblMax.Name = "lblMax";
			this.lblMax.Size = new System.Drawing.Size(77, 13);
			this.lblMax.TabIndex = 8;
			this.lblMax.Text = "Max concurrent";
			this.numMax.Location = new System.Drawing.Point(165, 132);
			this.numMax.Margin = new System.Windows.Forms.Padding(3, 3, 3, 8);
			this.numMax.Minimum = new decimal(new int[4] { 1, 0, 0, 0 });
			this.numMax.Name = "numMax";
			this.numMax.Size = new System.Drawing.Size(120, 20);
			this.numMax.TabIndex = 9;
			this.numMax.Value = new decimal(new int[4] { 3, 0, 0, 0 });
			this.lblKeyId.Anchor = System.Windows.Forms.AnchorStyles.Left;
			this.lblKeyId.AutoSize = true;
			this.lblKeyId.Location = new System.Drawing.Point(17, 164);
			this.lblKeyId.Name = "lblKeyId";
			this.lblKeyId.Size = new System.Drawing.Size(59, 13);
			this.lblKeyId.TabIndex = 10;
			this.lblKeyId.Text = "Key Entry Id";
			this.txtKeyId.Anchor = System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
			this.txtKeyId.Location = new System.Drawing.Point(165, 161);
			this.txtKeyId.Margin = new System.Windows.Forms.Padding(3, 3, 3, 8);
			this.txtKeyId.Name = "txtKeyId";
			this.txtKeyId.Size = new System.Drawing.Size(522, 20);
			this.txtKeyId.TabIndex = 11;
			this.lblExistingKey.Anchor = System.Windows.Forms.AnchorStyles.Left;
			this.lblExistingKey.AutoSize = true;
			this.lblExistingKey.Location = new System.Drawing.Point(17, 194);
			this.lblExistingKey.Name = "lblExistingKey";
			this.lblExistingKey.Size = new System.Drawing.Size(70, 13);
			this.lblExistingKey.TabIndex = 12;
			this.lblExistingKey.Text = "Existing API Key";
			this.cmbExistingKey.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.cmbExistingKey.FormattingEnabled = true;
			this.cmbExistingKey.Location = new System.Drawing.Point(165, 190);
			this.cmbExistingKey.Margin = new System.Windows.Forms.Padding(3, 3, 3, 10);
			this.cmbExistingKey.Name = "cmbExistingKey";
			this.cmbExistingKey.Size = new System.Drawing.Size(522, 21);
			this.cmbExistingKey.TabIndex = 13;
			this.cmbExistingKey.SelectedIndexChanged += new System.EventHandler(cmbExistingKey_SelectedIndexChanged);
			this.lblCredHeader.AutoSize = true;
			this.mainLayout.SetColumnSpan(this.lblCredHeader, 2);
			this.lblCredHeader.Font = new System.Drawing.Font("Segoe UI", 9f, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, 0);
			this.lblCredHeader.Location = new System.Drawing.Point(17, 236);
			this.lblCredHeader.Margin = new System.Windows.Forms.Padding(3, 16, 3, 8);
			this.lblCredHeader.Name = "lblCredHeader";
			this.lblCredHeader.Size = new System.Drawing.Size(87, 15);
			this.lblCredHeader.TabIndex = 14;
			this.lblCredHeader.Text = "API Credentials";
			this.lblKeyLabel.Anchor = System.Windows.Forms.AnchorStyles.Left;
			this.lblKeyLabel.AutoSize = true;
			this.lblKeyLabel.Location = new System.Drawing.Point(17, 264);
			this.lblKeyLabel.Name = "lblKeyLabel";
			this.lblKeyLabel.Size = new System.Drawing.Size(56, 13);
			this.lblKeyLabel.TabIndex = 15;
			this.lblKeyLabel.Text = "Key Label";
			this.txtKeyLabel.Anchor = System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
			this.txtKeyLabel.Location = new System.Drawing.Point(165, 261);
			this.txtKeyLabel.Margin = new System.Windows.Forms.Padding(3, 3, 3, 8);
			this.txtKeyLabel.Name = "txtKeyLabel";
			this.txtKeyLabel.Size = new System.Drawing.Size(522, 20);
			this.txtKeyLabel.TabIndex = 16;
			this.lblApiKey.Anchor = System.Windows.Forms.AnchorStyles.Left;
			this.lblApiKey.AutoSize = true;
			this.lblApiKey.Location = new System.Drawing.Point(17, 293);
			this.lblApiKey.Name = "lblApiKey";
			this.lblApiKey.Size = new System.Drawing.Size(43, 13);
			this.lblApiKey.TabIndex = 17;
			this.lblApiKey.Text = "API Key";
			this.txtApiKey.Anchor = System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
			this.txtApiKey.Location = new System.Drawing.Point(165, 290);
			this.txtApiKey.Margin = new System.Windows.Forms.Padding(3, 3, 3, 8);
			this.txtApiKey.Name = "txtApiKey";
			this.txtApiKey.Size = new System.Drawing.Size(522, 20);
			this.txtApiKey.TabIndex = 18;
			this.lblSecret.Anchor = System.Windows.Forms.AnchorStyles.Left;
			this.lblSecret.AutoSize = true;
			this.lblSecret.Location = new System.Drawing.Point(17, 322);
			this.lblSecret.Name = "lblSecret";
			this.lblSecret.Size = new System.Drawing.Size(89, 13);
			this.lblSecret.TabIndex = 19;
			this.lblSecret.Text = "API Secret (base64)";
			this.txtSecret.Anchor = System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
			this.txtSecret.Location = new System.Drawing.Point(165, 319);
			this.txtSecret.Margin = new System.Windows.Forms.Padding(3, 3, 3, 8);
			this.txtSecret.Name = "txtSecret";
			this.txtSecret.Size = new System.Drawing.Size(522, 20);
			this.txtSecret.TabIndex = 20;
			this.lblPassphrase.Anchor = System.Windows.Forms.AnchorStyles.Left;
			this.lblPassphrase.AutoSize = true;
			this.lblPassphrase.Location = new System.Drawing.Point(17, 351);
			this.lblPassphrase.Name = "lblPassphrase";
			this.lblPassphrase.Size = new System.Drawing.Size(61, 13);
			this.lblPassphrase.TabIndex = 21;
			this.lblPassphrase.Text = "Passphrase";
			this.txtPassphrase.Anchor = System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
			this.txtPassphrase.Location = new System.Drawing.Point(165, 348);
			this.txtPassphrase.Margin = new System.Windows.Forms.Padding(3, 3, 3, 8);
			this.txtPassphrase.Name = "txtPassphrase";
			this.txtPassphrase.Size = new System.Drawing.Size(522, 20);
			this.txtPassphrase.TabIndex = 22;
			this.lblApiKeyName.Anchor = System.Windows.Forms.AnchorStyles.Left;
			this.lblApiKeyName.AutoSize = true;
			this.lblApiKeyName.Location = new System.Drawing.Point(17, 380);
			this.lblApiKeyName.Name = "lblApiKeyName";
			this.lblApiKeyName.Size = new System.Drawing.Size(70, 13);
			this.lblApiKeyName.TabIndex = 23;
			this.lblApiKeyName.Text = "API Key Name";
			this.txtApiKeyName.Anchor = System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
			this.txtApiKeyName.Location = new System.Drawing.Point(165, 377);
			this.txtApiKeyName.Margin = new System.Windows.Forms.Padding(3, 3, 3, 8);
			this.txtApiKeyName.Name = "txtApiKeyName";
			this.txtApiKeyName.Size = new System.Drawing.Size(522, 20);
			this.txtApiKeyName.TabIndex = 24;
			this.lblPem.Anchor = System.Windows.Forms.AnchorStyles.Left;
			this.lblPem.AutoSize = true;
			this.lblPem.Location = new System.Drawing.Point(17, 426);
			this.lblPem.Name = "lblPem";
			this.lblPem.Size = new System.Drawing.Size(78, 13);
			this.lblPem.TabIndex = 25;
			this.lblPem.Text = "EC Private Key";
			this.txtPem.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
			this.txtPem.Location = new System.Drawing.Point(165, 406);
			this.txtPem.AcceptsReturn = true;
			this.txtPem.Margin = new System.Windows.Forms.Padding(3, 3, 3, 10);
			this.txtPem.Multiline = true;
			this.txtPem.Name = "txtPem";
			this.txtPem.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
			this.txtPem.Size = new System.Drawing.Size(522, 90);
			this.txtPem.TabIndex = 26;
			this.txtPem.WordWrap = false;
			this.actionPanel.Controls.Add(this.btnCancel);
			this.actionPanel.Controls.Add(this.btnSave);
			this.actionPanel.Dock = System.Windows.Forms.DockStyle.Bottom;
			this.actionPanel.FlowDirection = System.Windows.Forms.FlowDirection.RightToLeft;
			this.actionPanel.Location = new System.Drawing.Point(0, 525);
			this.actionPanel.Name = "actionPanel";
			this.actionPanel.Padding = new System.Windows.Forms.Padding(12, 10, 12, 10);
			this.actionPanel.Size = new System.Drawing.Size(704, 52);
			this.actionPanel.TabIndex = 1;
			this.btnCancel.Location = new System.Drawing.Point(606, 13);
			this.btnCancel.Name = "btnCancel";
			this.btnCancel.Size = new System.Drawing.Size(74, 28);
			this.btnCancel.TabIndex = 1;
			this.btnCancel.Text = "Cancel";
			this.btnCancel.UseVisualStyleBackColor = true;
			this.btnCancel.Click += new System.EventHandler(btnCancel_Click);
			this.btnSave.Location = new System.Drawing.Point(526, 13);
			this.btnSave.Name = "btnSave";
			this.btnSave.Size = new System.Drawing.Size(74, 28);
			this.btnSave.TabIndex = 0;
			this.btnSave.Text = "Save";
			this.btnSave.UseVisualStyleBackColor = true;
			this.btnSave.Click += new System.EventHandler(btnSave_Click);
			base.AcceptButton = this.btnSave;
			base.AutoScaleDimensions = new System.Drawing.SizeF(6f, 13f);
			base.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			base.CancelButton = this.btnCancel;
			base.ClientSize = new System.Drawing.Size(704, 577);
			base.Controls.Add(this.mainLayout);
			base.Controls.Add(this.actionPanel);
			base.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
			base.MaximizeBox = false;
			base.MinimizeBox = false;
			base.Name = "AccountEditDialog";
			base.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
			this.Text = "Account";
			this.mainLayout.ResumeLayout(false);
			this.mainLayout.PerformLayout();
			((System.ComponentModel.ISupportInitialize)this.numRisk).EndInit();
			((System.ComponentModel.ISupportInitialize)this.numMax).EndInit();
			this.actionPanel.ResumeLayout(false);
			base.ResumeLayout(false);
		}
	}
}
