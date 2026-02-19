using System;
using System.IO;
using System.Windows.Forms;
using CryptoDayTraderSuite.Models;
using CryptoDayTraderSuite.Services;
using CryptoDayTraderSuite.Util;

namespace CryptoDayTraderSuite.UI
{
    public partial class AccountEditDialog : Form
    {
        private readonly string _id;
        private readonly IAccountService _accountService;
        private readonly IKeyService _keyService;

        private sealed class KeyOption
        {
            public string Id;
            public string Label;
            public override string ToString()
            {
                return Label;
            }
        }

        public AccountEditDialog(string id, IAccountService accountService, IKeyService keyService = null)
        {
            _id = id;
            _accountService = accountService ?? throw new ArgumentNullException(nameof(accountService));
            _keyService = keyService;
            InitializeComponent();
            DialogTheme.Apply(this);
            this.Text = string.IsNullOrEmpty(_id) ? "Add Account" : "Edit Account";
            if (cmbService.Items.Count > 0) cmbService.SelectedIndex = 0;
            if (cmbMode.Items.Count > 0) cmbMode.SelectedIndex = 0;
            if (!string.IsNullOrEmpty(id)) LoadExisting();
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
            if (btnSave == null) return;
            btnSave.Enabled = CanSaveWithCurrentInputs();
        }

        private bool CanSaveWithCurrentInputs()
        {
            if (string.IsNullOrWhiteSpace(txtLabel.Text)) return false;
            if (cmbService.SelectedItem == null) return false;
            if (cmbMode.SelectedItem == null) return false;

            var service = cmbService.SelectedItem.ToString();
            NormalizeCoinbaseCredentialFieldsForService(service);
            var policy = ExchangeCredentialPolicy.ForService(service);
            if (_keyService == null) return true;
            if (policy.IsPaper) return true;

            var selectedExistingKeyId = GetSelectedExistingKeyId();
            var hasCredentialEdits = HasAnyCredentialInputs();

            if (string.IsNullOrWhiteSpace(selectedExistingKeyId) && !hasCredentialEdits)
            {
                return false;
            }

            if (!string.IsNullOrWhiteSpace(selectedExistingKeyId) && !hasCredentialEdits)
            {
                return true;
            }

            return HasRequiredCredentialInputs(policy);
        }

        private void LoadExisting()
        {
            var a = _accountService.Get(_id);
            if (a == null) return;
            txtLabel.Text = a.Label;
            if (!string.IsNullOrWhiteSpace(a.Service) && cmbService.Items.Contains(a.Service))
                cmbService.SelectedItem = a.Service;
            else if (cmbService.Items.Count > 0)
                cmbService.SelectedIndex = 0;

            var modeText = a.Mode.ToString();
            if (!string.IsNullOrWhiteSpace(modeText) && cmbMode.Items.Contains(modeText))
                cmbMode.SelectedItem = modeText;
            else if (cmbMode.Items.Count > 0)
                cmbMode.SelectedIndex = 0;

            numRisk.Value = a.RiskPerTradePct;
            numMax.Value = a.MaxConcurrentTrades;
            txtKeyId.Text = a.KeyEntryId;

            PopulateExistingKeys(a.KeyEntryId);

            if (_keyService == null || string.IsNullOrWhiteSpace(a.KeyEntryId)) return;

            var existing = _keyService.Get(a.KeyEntryId);
            if (existing == null) return;

            txtKeyLabel.Text = existing.Label;
            txtApiKey.Text = SafeUnprotect(existing.ApiKey);
            txtSecret.Text = SafeUnprotect(!string.IsNullOrWhiteSpace(existing.ApiSecretBase64) ? existing.ApiSecretBase64 : existing.Secret);
            txtPassphrase.Text = SafeUnprotect(existing.Passphrase);
            txtApiKeyName.Text = existing.ApiKeyName ?? string.Empty;
            txtPem.Text = SafeUnprotect(existing.ECPrivateKeyPem);
        }

        private void SaveClicked()
        {
            if (string.IsNullOrWhiteSpace(txtLabel.Text)) { MessageBox.Show("label required"); return; }
            if (cmbService.SelectedItem == null) { MessageBox.Show("service required"); return; }
            if (cmbMode.SelectedItem == null) { MessageBox.Show("mode required"); return; }

            AccountInfo a;
            if (string.IsNullOrEmpty(_id))
            {
                 a = new AccountInfo { Id = Guid.NewGuid().ToString(), CreatedUtc = DateTime.UtcNow };
            }
            else
            {
                 a = _accountService.Get(_id) ?? new AccountInfo { Id = Guid.NewGuid().ToString(), CreatedUtc = DateTime.UtcNow };
            }

            a.Label = txtLabel.Text.Trim();
            a.Service = cmbService.SelectedItem.ToString();
            
            AccountMode m;
            Enum.TryParse(cmbMode.SelectedItem.ToString(), true, out m);
            a.Mode = m;
            
            a.RiskPerTradePct = numRisk.Value;
            a.MaxConcurrentTrades = (int)numMax.Value;
            var selectedService = cmbService.SelectedItem.ToString();
            var keyEntryId = BuildKeyEntryId(selectedService, txtLabel.Text.Trim(), a.KeyEntryId);
            if (keyEntryId == null) return;
            a.KeyEntryId = keyEntryId;
            a.Enabled = true;
            a.UpdatedUtc = DateTime.UtcNow;
            
            _accountService.Upsert(a);
            this.DialogResult = System.Windows.Forms.DialogResult.OK;
        }

        private string BuildKeyEntryId(string service, string accountLabel, string existingKeyId)
        {
            NormalizeCoinbaseCredentialFieldsForService(service);
            var policy = ExchangeCredentialPolicy.ForService(service);

            if (_keyService == null)
            {
                return txtKeyId.Text.Trim();
            }

            if (policy.IsPaper)
            {
                return string.Empty;
            }

            var selectedExistingKeyId = GetSelectedExistingKeyId();
            var hasCredentialEdits = HasAnyCredentialInputs();

            if (string.IsNullOrWhiteSpace(selectedExistingKeyId) && !hasCredentialEdits)
            {
                MessageBox.Show("Select an Existing API Key or enter new credentials before saving this non-paper account.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return null;
            }

            if (!string.IsNullOrWhiteSpace(selectedExistingKeyId) && !hasCredentialEdits)
            {
                txtKeyId.Text = selectedExistingKeyId;
                return selectedExistingKeyId;
            }

            if (!HasRequiredCredentialInputs(policy))
            {
                MessageBox.Show(
                    "Missing required credentials for " + service + ". Required: " + policy.RequiredSummary + ". Template: " + policy.TemplateSummary + ".",
                    "Validation",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return null;
            }

            var keyLabel = txtKeyLabel.Text.Trim();
            if (string.IsNullOrWhiteSpace(keyLabel)) keyLabel = accountLabel;
            if (string.IsNullOrWhiteSpace(keyLabel)) keyLabel = "Default";

            var preferredExistingId = !string.IsNullOrWhiteSpace(selectedExistingKeyId) ? selectedExistingKeyId : existingKeyId;
            var existing = !string.IsNullOrWhiteSpace(preferredExistingId) ? _keyService.Get(preferredExistingId) : null;
            if (existing == null) existing = _keyService.Get(service, keyLabel);
            if (existing == null)
            {
                var all = _keyService.GetAll();
                foreach (var key in all)
                {
                    if (key == null) continue;
                    if (!string.Equals(key.Label ?? string.Empty, keyLabel, StringComparison.OrdinalIgnoreCase)) continue;
                    if (!IsServiceCompatible(key.Broker ?? key.Service ?? string.Empty, service)) continue;
                    existing = key;
                    break;
                }
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
            var isCoinbaseService = service.IndexOf("coinbase", StringComparison.OrdinalIgnoreCase) >= 0;
            existing.ApiKey = _keyService.Protect(isCoinbaseService ? string.Empty : txtApiKey.Text.Trim());
            existing.ApiSecretBase64 = _keyService.Protect(isCoinbaseService ? string.Empty : txtSecret.Text.Trim());
            existing.Secret = existing.ApiSecretBase64;
            existing.Passphrase = _keyService.Protect(isCoinbaseService ? string.Empty : txtPassphrase.Text.Trim());
            existing.ApiKeyName = txtApiKeyName.Text.Trim();
            existing.ECPrivateKeyPem = _keyService.Protect(CoinbaseCredentialNormalizer.NormalizePem(txtPem.Text));

            _keyService.Upsert(existing);

            var keyId = KeyEntry.MakeId(service, keyLabel);
            txtKeyId.Text = keyId;
            return keyId;
        }

        private void UpdateCredentialFieldVisibility()
        {
            var service = cmbService != null && cmbService.SelectedItem != null ? cmbService.SelectedItem.ToString() : string.Empty;
            var policy = ExchangeCredentialPolicy.ForService(service);
            var isCoinbase = service.IndexOf("coinbase", StringComparison.OrdinalIgnoreCase) >= 0;
            var usingKeyService = _keyService != null;
            var showCredentialSection = usingKeyService && !policy.IsPaper;

            lblExistingKey.Visible = showCredentialSection;
            cmbExistingKey.Visible = showCredentialSection;

            lblCredHeader.Visible = showCredentialSection;

            lblKeyId.Visible = !usingKeyService || policy.IsPaper;
            txtKeyId.Visible = !usingKeyService || policy.IsPaper;

            lblKeyLabel.Visible = showCredentialSection;
            txtKeyLabel.Visible = showCredentialSection;

            lblApiKey.Visible = showCredentialSection && (policy.RequiresApiKey || isCoinbase);
            txtApiKey.Visible = showCredentialSection && (policy.RequiresApiKey || isCoinbase);
            if (pnlApiKeyImport != null) pnlApiKeyImport.Visible = showCredentialSection && (policy.RequiresApiKey || isCoinbase);
            if (btnImportCoinbaseJson != null) btnImportCoinbaseJson.Visible = showCredentialSection && isCoinbase;

            lblSecret.Visible = showCredentialSection && policy.RequiresApiSecret;
            txtSecret.Visible = showCredentialSection && policy.RequiresApiSecret;

            lblPassphrase.Visible = showCredentialSection && policy.RequiresPassphrase;
            txtPassphrase.Visible = showCredentialSection && policy.RequiresPassphrase;

            lblApiKeyName.Visible = showCredentialSection && policy.RequiresApiKeyName;
            txtApiKeyName.Visible = showCredentialSection && policy.RequiresApiKeyName;

            lblPem.Visible = showCredentialSection && policy.RequiresEcPrivateKeyPem;
            txtPem.Visible = showCredentialSection && policy.RequiresEcPrivateKeyPem;

            if (lblCredHeader != null)
            {
                var hint = GetGeoRoutingHint(service);
                var template = policy.TemplateSummary;
                lblCredHeader.Text = showCredentialSection
                    ? ("Credentials Required: " + policy.RequiredSummary + " | Template: " + template + (string.IsNullOrWhiteSpace(hint) ? string.Empty : " | " + hint))
                    : "Credentials";
            }

            if (lblApiKey != null)
            {
                lblApiKey.Text = isCoinbase ? "Coinbase Key JSON (optional)" : "API Key";
            }

            if (usingKeyService && !policy.IsPaper && string.IsNullOrWhiteSpace(txtKeyLabel.Text) && !string.IsNullOrWhiteSpace(txtLabel.Text))
            {
                txtKeyLabel.Text = txtLabel.Text.Trim();
            }

            UpdateSaveButtonState();
        }

        private void PopulateExistingKeys(string selectedKeyId = null)
        {
            if (cmbExistingKey == null) return;

            cmbExistingKey.BeginUpdate();
            try
            {
                cmbExistingKey.Items.Clear();
                cmbExistingKey.Items.Add(new KeyOption { Id = string.Empty, Label = "(Create / Update with fields below)" });

                var service = cmbService != null && cmbService.SelectedItem != null ? cmbService.SelectedItem.ToString() : string.Empty;
                if (_keyService != null && !string.IsNullOrWhiteSpace(service) && !ExchangeCredentialPolicy.ForService(service).IsPaper)
                {
                    var keys = _keyService.GetAll();
                    foreach (var key in keys)
                    {
                        if (key == null) continue;
                        var keyService = key.Broker ?? key.Service ?? string.Empty;
                        if (!IsServiceCompatible(keyService, service)) continue;
                        if (!key.Enabled) continue;

                        var keyId = KeyEntry.MakeId(keyService, key.Label ?? string.Empty);
                        cmbExistingKey.Items.Add(new KeyOption
                        {
                            Id = keyId,
                            Label = (key.Label ?? "(unnamed)") + " [" + keyService + "]"
                        });
                    }
                }

                var targetId = string.IsNullOrWhiteSpace(selectedKeyId) ? txtKeyId.Text.Trim() : selectedKeyId.Trim();
                var selectedIndex = 0;
                if (!string.IsNullOrWhiteSpace(targetId))
                {
                    for (var i = 0; i < cmbExistingKey.Items.Count; i++)
                    {
                        var opt = cmbExistingKey.Items[i] as KeyOption;
                        if (opt != null && string.Equals(opt.Id, targetId, StringComparison.OrdinalIgnoreCase))
                        {
                            selectedIndex = i;
                            break;
                        }
                    }
                }

                if (cmbExistingKey.Items.Count > 0)
                    cmbExistingKey.SelectedIndex = selectedIndex;
            }
            finally
            {
                cmbExistingKey.EndUpdate();
            }
        }

        private string GetSelectedExistingKeyId()
        {
            var selected = cmbExistingKey != null ? cmbExistingKey.SelectedItem as KeyOption : null;
            return selected != null ? (selected.Id ?? string.Empty).Trim() : string.Empty;
        }

        private bool HasAnyCredentialInputs()
        {
            if (!string.IsNullOrWhiteSpace(txtApiKey.Text)) return true;
            if (!string.IsNullOrWhiteSpace(txtSecret.Text)) return true;
            if (!string.IsNullOrWhiteSpace(txtPassphrase.Text)) return true;
            if (!string.IsNullOrWhiteSpace(txtApiKeyName.Text)) return true;
            if (!string.IsNullOrWhiteSpace(txtPem.Text)) return true;
            return false;
        }

        private bool HasRequiredCredentialInputs(ExchangeCredentialPolicy policy)
        {
            if (policy == null) return false;
            if (policy.IsPaper) return true;

            if (policy.RequiresApiKey && string.IsNullOrWhiteSpace(txtApiKey.Text)) return false;
            if (policy.RequiresApiSecret && string.IsNullOrWhiteSpace(txtSecret.Text)) return false;
            if (policy.RequiresPassphrase && string.IsNullOrWhiteSpace(txtPassphrase.Text)) return false;
            if (policy.RequiresApiKeyName && string.IsNullOrWhiteSpace(txtApiKeyName.Text)) return false;
            if (policy.RequiresEcPrivateKeyPem && string.IsNullOrWhiteSpace(txtPem.Text)) return false;

            return true;
        }

        private void NormalizeCoinbaseCredentialFieldsForService(string service)
        {
            if (string.IsNullOrWhiteSpace(service)) return;
            if (service.IndexOf("coinbase", StringComparison.OrdinalIgnoreCase) < 0) return;

            var apiKey = txtApiKey.Text;
            var apiSecret = txtSecret.Text;
            var apiKeyName = txtApiKeyName.Text;
            var pem = txtPem.Text;

            CoinbaseCredentialNormalizer.NormalizeCoinbaseAdvancedInputs(ref apiKey, ref apiSecret, ref apiKeyName, ref pem);

            if (string.IsNullOrWhiteSpace(txtApiKeyName.Text) && !string.IsNullOrWhiteSpace(apiKeyName))
            {
                txtApiKeyName.Text = apiKeyName;
            }

            if (!string.IsNullOrWhiteSpace(pem) && !string.Equals(txtPem.Text, pem, StringComparison.Ordinal))
            {
                txtPem.Text = pem;
            }

            txtSecret.Text = string.Empty;
            txtPassphrase.Text = string.Empty;
            txtApiKey.Text = string.Empty;
        }

        private void ImportCoinbaseJsonFromFile()
        {
            using (var dialog = new OpenFileDialog())
            {
                dialog.Title = "Import Coinbase API Key JSON";
                dialog.Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*";
                dialog.CheckFileExists = true;
                dialog.Multiselect = false;
                dialog.FileName = "cdp_api_key(2).json";

                var downloads = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");
                if (Directory.Exists(downloads))
                {
                    dialog.InitialDirectory = downloads;
                }

                if (dialog.ShowDialog(this) != DialogResult.OK)
                {
                    return;
                }

                try
                {
                    txtApiKey.Text = File.ReadAllText(dialog.FileName);
                    NormalizeCoinbaseCredentialFieldsForService(cmbService.SelectedItem == null ? string.Empty : cmbService.SelectedItem.ToString());
                    UpdateSaveButtonState();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Failed to import Coinbase JSON file: " + ex.Message, "Import Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private string SafeUnprotect(string value)
        {
            if (string.IsNullOrWhiteSpace(value) || _keyService == null) return string.Empty;
            try { return _keyService.Unprotect(value); }
            catch { return string.Empty; }
        }

        private bool IsServiceCompatible(string left, string right)
        {
            return string.Equals(CanonicalizeServiceAlias(left), CanonicalizeServiceAlias(right), StringComparison.OrdinalIgnoreCase);
        }

        private string CanonicalizeServiceAlias(string service)
        {
            var normalized = (service ?? string.Empty).Trim().ToLowerInvariant();
            switch (normalized)
            {
                case "binance-us":
                case "binance-global":
                case "binance":
                    return "binance";
                case "bybit-global":
                case "bybit":
                    return "bybit";
                case "okx-global":
                case "okx":
                    return "okx";
                default:
                    return normalized;
            }
        }

        private string GetGeoRoutingHint(string service)
        {
            var normalized = (service ?? string.Empty).Trim().ToLowerInvariant();
            switch (normalized)
            {
                case "binance-us":
                    return "Routing: Binance US endpoint (US-friendly)";
                case "binance-global":
                    return "Routing: Binance global endpoint";
                case "bybit-global":
                    return "Routing: Bybit global endpoint";
                case "okx-global":
                    return "Routing: OKX global endpoint";
                case "binance":
                    return "Routing: Binance default endpoint (env override supported)";
                case "bybit":
                    return "Routing: Bybit default endpoint (env override supported)";
                case "okx":
                    return "Routing: OKX default endpoint (env override supported)";
                default:
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
            var keyId = GetSelectedExistingKeyId();
            if (string.IsNullOrWhiteSpace(keyId)) return;
            if (_keyService == null) return;

            var key = _keyService.Get(keyId);
            if (key == null) return;

            txtKeyId.Text = keyId;
            txtKeyLabel.Text = key.Label ?? txtKeyLabel.Text;
            txtApiKey.Text = SafeUnprotect(key.ApiKey);
            txtSecret.Text = SafeUnprotect(!string.IsNullOrWhiteSpace(key.ApiSecretBase64) ? key.ApiSecretBase64 : key.Secret);
            txtPassphrase.Text = SafeUnprotect(key.Passphrase);
            txtApiKeyName.Text = key.ApiKeyName ?? string.Empty;
            txtPem.Text = SafeUnprotect(key.ECPrivateKeyPem);
            UpdateSaveButtonState();
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            SaveClicked();
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
        }

        private void btnImportCoinbaseJson_Click(object sender, EventArgs e)
        {
            ImportCoinbaseJsonFromFile();
        }
    }
}
