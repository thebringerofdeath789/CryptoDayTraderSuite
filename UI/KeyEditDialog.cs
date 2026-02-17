using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using CryptoDayTraderSuite.Services;
using CryptoDayTraderSuite.Models;
using CryptoDayTraderSuite.Util;

namespace CryptoDayTraderSuite.UI
{
    public partial class KeyEditDialog : Form
    {
        private string _id;
        private IKeyService _service;
        private IAccountService _accountService;
        private IHistoryService _historyService;
        private ComboBox cmbService;
        private TextBox txtLabel, txtApiKey, txtSecret, txtPass, txtKeyName, txtPem;
        private TextBox txtCoinbaseJson;
        private CheckBox chkActive;
        private Label lblRoutingHint;
        private Label lblGeneralHeader, lblApiKey, lblApiSecret, lblPass;
        private Label lblAdvancedHeader, lblCoinbaseJson, lblKeyName, lblPem;
        private Button btnImportCoinbaseJson;

        public KeyEditDialog(string id, IKeyService service, IAccountService accountService = null, IHistoryService historyService = null)
        {
            _id = id;
            _service = service;
            _accountService = accountService;
            _historyService = historyService;
            BuildUi();
            if (!string.IsNullOrEmpty(id)) LoadExisting();
        }

        private void BuildUi()
        {
            this.Text = string.IsNullOrEmpty(_id) ? "Add Key" : "Edit Key";
            this.StartPosition = FormStartPosition.CenterParent;
            this.Width = 740; this.Height = 600;
            this.FormBorderStyle = FormBorderStyle.FixedDialog; this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Padding = new Padding(0);

            var tl = new TableLayoutPanel(); tl.Dock = DockStyle.Fill; tl.ColumnCount = 2; tl.Padding = new Padding(14);
            tl.AutoScroll = true;
            tl.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 170f));
            tl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            this.Controls.Add(tl);

            tl.Controls.Add(new Label { Text = "Service", AutoSize = true, Margin = new Padding(0,8,8,8) }, 0, 0);
            cmbService = new ComboBox(); cmbService.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbService.Items.AddRange(new object[] { "coinbase-advanced", "binance", "binance-us", "binance-global", "bybit", "bybit-global", "okx", "okx-global", "kraken", "bitstamp" });
            cmbService.Width = 280;
            cmbService.Margin = new Padding(3, 4, 3, 8);
            tl.Controls.Add(cmbService, 1, 0);
            if (cmbService.Items.Count > 0) cmbService.SelectedIndex = 0;

            tl.Controls.Add(new Label { Text = "Routing Hint", AutoSize = true, Margin = new Padding(0,8,8,8) }, 0, 1);
            lblRoutingHint = new Label { AutoSize = true, Margin = new Padding(3, 8, 3, 8), ForeColor = System.Drawing.SystemColors.GrayText };
            tl.Controls.Add(lblRoutingHint, 1, 1);

            tl.Controls.Add(new Label { Text = "Label", AutoSize = true, Margin = new Padding(0,8,8,8) }, 0, 2);
            txtLabel = new TextBox(); txtLabel.Dock = DockStyle.Fill; txtLabel.Margin = new Padding(3, 3, 3, 8); tl.Controls.Add(txtLabel, 1, 2);

            tl.Controls.Add(new Label { Text = "Active", AutoSize = true, Margin = new Padding(0,8,8,8) }, 0, 3);
            chkActive = new CheckBox(); chkActive.Margin = new Padding(3, 5, 3, 8); tl.Controls.Add(chkActive, 1, 3);

            lblGeneralHeader = new Label { Text = "General API Credentials", AutoSize = true, Margin = new Padding(0,16,8,8) };
            lblGeneralHeader.Font = new System.Drawing.Font(lblGeneralHeader.Font, System.Drawing.FontStyle.Bold);
            tl.Controls.Add(lblGeneralHeader, 0, 4);
            tl.SetColumnSpan(lblGeneralHeader, 2);

            lblApiKey = new Label { Text = "API Key", AutoSize = true, Margin = new Padding(0,8,8,8) };
            tl.Controls.Add(lblApiKey, 0, 5);
            txtApiKey = new TextBox(); txtApiKey.Dock = DockStyle.Fill; txtApiKey.Margin = new Padding(3, 3, 3, 8); tl.Controls.Add(txtApiKey, 1, 5);

            lblApiSecret = new Label { Text = "API Secret", AutoSize = true, Margin = new Padding(0,8,8,8) };
            tl.Controls.Add(lblApiSecret, 0, 6);
            txtSecret = new TextBox(); txtSecret.Dock = DockStyle.Fill; txtSecret.Margin = new Padding(3, 3, 3, 8); tl.Controls.Add(txtSecret, 1, 6);

            lblPass = new Label { Text = "Passphrase", AutoSize = true, Margin = new Padding(0,8,8,8) };
            tl.Controls.Add(lblPass, 0, 7);
            txtPass = new TextBox(); txtPass.Dock = DockStyle.Fill; txtPass.Margin = new Padding(3, 3, 3, 8); tl.Controls.Add(txtPass, 1, 7);

            lblAdvancedHeader = new Label { Text = "Coinbase Advanced Credentials", AutoSize = true, Margin = new Padding(0,16,8,8) };
            lblAdvancedHeader.Font = new System.Drawing.Font(lblAdvancedHeader.Font, System.Drawing.FontStyle.Bold);
            tl.Controls.Add(lblAdvancedHeader, 0, 8);
            tl.SetColumnSpan(lblAdvancedHeader, 2);

            lblCoinbaseJson = new Label { Text = "Coinbase Key JSON (optional)", AutoSize = true, Margin = new Padding(0,8,8,8) };
            tl.Controls.Add(lblCoinbaseJson, 0, 9);
            var coinbaseImportPanel = new TableLayoutPanel();
            coinbaseImportPanel.ColumnCount = 2;
            coinbaseImportPanel.Dock = DockStyle.Fill;
            coinbaseImportPanel.Margin = new Padding(0);
            coinbaseImportPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            coinbaseImportPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

            txtCoinbaseJson = new TextBox(); txtCoinbaseJson.Multiline = true; txtCoinbaseJson.Height = 72; txtCoinbaseJson.Dock = DockStyle.Fill; txtCoinbaseJson.ScrollBars = ScrollBars.Vertical; txtCoinbaseJson.WordWrap = false; txtCoinbaseJson.Margin = new Padding(3, 3, 8, 8);
            btnImportCoinbaseJson = new Button(); btnImportCoinbaseJson.Text = "Import JSON..."; btnImportCoinbaseJson.AutoSize = true; btnImportCoinbaseJson.Margin = new Padding(0, 3, 3, 8); btnImportCoinbaseJson.Click += (s, e) => ImportCoinbaseJsonFromFile();
            coinbaseImportPanel.Controls.Add(txtCoinbaseJson, 0, 0);
            coinbaseImportPanel.Controls.Add(btnImportCoinbaseJson, 1, 0);
            tl.Controls.Add(coinbaseImportPanel, 1, 9);

            lblKeyName = new Label { Text = "API Key Name", AutoSize = true, Margin = new Padding(0,8,8,8) };
            tl.Controls.Add(lblKeyName, 0, 10);
            txtKeyName = new TextBox(); txtKeyName.Dock = DockStyle.Fill; txtKeyName.Margin = new Padding(3, 3, 3, 8); tl.Controls.Add(txtKeyName, 1, 10);

            lblPem = new Label { Text = "EC Private Key (PEM)", AutoSize = true, Margin = new Padding(0,8,8,8) };
            tl.Controls.Add(lblPem, 0, 11);
            txtPem = new TextBox(); txtPem.Multiline = true; txtPem.Height = 140; txtPem.Dock = DockStyle.Fill; txtPem.ScrollBars = ScrollBars.Vertical; txtPem.WordWrap = false; txtPem.Margin = new Padding(3, 3, 3, 10); tl.Controls.Add(txtPem, 1, 11);

            tl.RowStyles.Clear();
            for (var row = 0; row < 11; row++) tl.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            tl.RowStyles.Add(new RowStyle(SizeType.Absolute, 156f));

            cmbService.SelectedIndexChanged += (s, e) => UpdateCredentialFieldVisibility();

            var bar = new FlowLayoutPanel(); bar.Dock = DockStyle.Bottom; bar.FlowDirection = FlowDirection.RightToLeft; bar.Padding = new Padding(12, 10, 12, 10); bar.Height = 54;
            var btnSave = new Button { Text = "Save" }; btnSave.Click += (s, e) => SaveClicked();
            var btnCancel = new Button { Text = "Cancel" }; btnCancel.Click += (s, e) => this.DialogResult = DialogResult.Cancel;
            btnSave.Size = new System.Drawing.Size(84, 28);
            btnCancel.Size = new System.Drawing.Size(84, 28);
            bar.Controls.Add(btnSave); bar.Controls.Add(btnCancel);
            this.Controls.Add(bar);

            DialogTheme.Apply(this);
            UpdateCredentialFieldVisibility();
        }

        private void LoadExisting()
        {
            if (_service == null) return;
            var k = _service.Get(_id);
            if (k == null) return;
            cmbService.SelectedItem = k.Service;
            txtLabel.Text = k.Label;
            chkActive.Checked = k.Active;
            string v;
            try { if (k.Data.TryGetValue("ApiKey", out v)) txtApiKey.Text = _service.Unprotect(v); } catch { txtApiKey.Text = ""; }
            try { if (k.Data.TryGetValue("ApiSecretBase64", out v)) txtSecret.Text = _service.Unprotect(v); } catch { txtSecret.Text = ""; }
            try { if (k.Data.TryGetValue("Passphrase", out v)) txtPass.Text = _service.Unprotect(v); } catch { txtPass.Text = ""; }
            if (k.Data.TryGetValue("ApiKeyName", out v)) txtKeyName.Text = v;
            try { if (k.Data.TryGetValue("ECPrivateKeyPem", out v)) txtPem.Text = _service.Unprotect(v); } catch { txtPem.Text = ""; }
            UpdateCredentialFieldVisibility();
        }

        private void SaveClicked()
        {
            if (_service == null) { MessageBox.Show("service unavailable"); return; }
            if (cmbService.SelectedItem == null) { MessageBox.Show("service required"); return; }
            if (string.IsNullOrWhiteSpace(txtLabel.Text)) { MessageBox.Show("label required"); return; }

            var selectedService = cmbService.SelectedItem.ToString();
            NormalizeCoinbaseCredentialFieldsForService(selectedService);
            var policy = ExchangeCredentialPolicy.ForService(selectedService);
            if (!HasRequiredCredentialInputs(policy))
            {
                MessageBox.Show("Missing required credentials for " + selectedService + ". Required: " + policy.RequiredSummary + ".", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var info = _service.Get(_id);
            KeyEntry k;
            
            if (!string.IsNullOrEmpty(_id) && info != null)
                k = info;
            else
                k = new KeyEntry { Id = Guid.NewGuid().ToString() };

            if (k.CreatedUtc == default(DateTime)) k.CreatedUtc = DateTime.UtcNow;

            k.Service = selectedService;
            k.Label = txtLabel.Text.Trim();
            k.Active = chkActive.Checked;
            k.Enabled = true;
            k.UpdatedUtc = DateTime.UtcNow;

            k.Data["ApiKey"] = _service.Protect(txtApiKey.Text.Trim());
            k.Data["ApiSecretBase64"] = _service.Protect(txtSecret.Text.Trim());
            k.Data["Passphrase"] = _service.Protect(txtPass.Text.Trim());
            k.Data["ApiKeyName"] = txtKeyName.Text.Trim();
            k.Data["ECPrivateKeyPem"] = _service.Protect(CoinbaseCredentialNormalizer.NormalizePem(txtPem.Text));

            _service.Upsert((KeyInfo)k);

            if (k.Active) _service.SetActive(k.Service, k.Label);
            TryAutoImportCoinbaseReadOnly(k.Service, k.Label);
            this.DialogResult = DialogResult.OK;
        }

        private void TryAutoImportCoinbaseReadOnly(string service, string label)
        {
            if (_service == null || _accountService == null)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(service) || service.IndexOf("coinbase", StringComparison.OrdinalIgnoreCase) < 0)
            {
                return;
            }

            Task.Run(async () =>
            {
                try
                {
                    var canonicalService = "coinbase-advanced";
                    var keyId = KeyEntry.MakeId(canonicalService, label ?? string.Empty);
                    var importService = new CoinbaseReadOnlyImportService(_service, _accountService, _historyService);
                    var result = await importService.ValidateAndImportForKeyAsync(keyId).ConfigureAwait(false);
                    Log.Info("[Keys] Coinbase auto-import complete. Key=" + result.KeyId
                        + " Products=" + result.ProductCount.ToString(CultureInfo.InvariantCulture)
                        + " Holdings=" + result.NonZeroBalanceCount.ToString(CultureInfo.InvariantCulture)
                        + " HoldingsInQuote=" + result.TotalBalanceInQuote.ToString(CultureInfo.InvariantCulture)
                        + " Quote=" + (string.IsNullOrWhiteSpace(result.TotalBalanceQuoteCurrency) ? "USD" : result.TotalBalanceQuoteCurrency)
                        + " ExcludedHoldings=" + result.TotalBalanceExcludedCount.ToString(CultureInfo.InvariantCulture)
                        + " Fills=" + result.TotalFillCount.ToString(CultureInfo.InvariantCulture)
                        + " ImportedTrades=" + result.ImportedTradeCount.ToString(CultureInfo.InvariantCulture)
                        + " Fees=" + result.TotalFeesPaid.ToString(CultureInfo.InvariantCulture)
                        + " NetProfitEst=" + result.NetProfitEstimate.ToString(CultureInfo.InvariantCulture));
                }
                catch (Exception ex)
                {
                    Log.Warn("[Keys] Coinbase auto-import failed after key save: " + ex.Message);
                }
            });
        }

        private void NormalizeCoinbaseCredentialFieldsForService(string service)
        {
            if (string.IsNullOrWhiteSpace(service)) return;
            if (service.IndexOf("coinbase", StringComparison.OrdinalIgnoreCase) < 0) return;

            var raw = txtCoinbaseJson == null ? string.Empty : txtCoinbaseJson.Text;
            string parsedName;
            string parsedPem;
            if (CoinbaseCredentialNormalizer.TryExtractNameAndPrivateKey(raw, out parsedName, out parsedPem))
            {
                if (string.IsNullOrWhiteSpace(txtKeyName.Text)) txtKeyName.Text = parsedName;
                if (string.IsNullOrWhiteSpace(txtPem.Text)) txtPem.Text = parsedPem;
            }

            var apiKey = txtApiKey.Text;
            var apiSecret = txtSecret.Text;
            var apiKeyName = txtKeyName.Text;
            var pem = txtPem.Text;

            CoinbaseCredentialNormalizer.NormalizeCoinbaseAdvancedInputs(ref apiKey, ref apiSecret, ref apiKeyName, ref pem);

            if (string.IsNullOrWhiteSpace(txtKeyName.Text) && !string.IsNullOrWhiteSpace(apiKeyName))
            {
                txtKeyName.Text = apiKeyName;
            }

            if (!string.IsNullOrWhiteSpace(pem) && !string.Equals(txtPem.Text, pem, StringComparison.Ordinal))
            {
                txtPem.Text = pem;
            }
        }

        private void UpdateCredentialFieldVisibility()
        {
            var service = cmbService != null && cmbService.SelectedItem != null ? cmbService.SelectedItem.ToString() : string.Empty;
            var isCoinbase = !string.IsNullOrWhiteSpace(service) && service.IndexOf("coinbase", StringComparison.OrdinalIgnoreCase) >= 0;

            if (lblGeneralHeader != null) lblGeneralHeader.Visible = !isCoinbase;
            if (lblApiKey != null) lblApiKey.Visible = !isCoinbase;
            if (txtApiKey != null) txtApiKey.Visible = !isCoinbase;
            if (lblApiSecret != null) lblApiSecret.Visible = !isCoinbase;
            if (txtSecret != null) txtSecret.Visible = !isCoinbase;
            if (lblPass != null) lblPass.Visible = !isCoinbase;
            if (txtPass != null) txtPass.Visible = !isCoinbase;

            if (lblAdvancedHeader != null) lblAdvancedHeader.Visible = isCoinbase;
            if (lblCoinbaseJson != null) lblCoinbaseJson.Visible = isCoinbase;
            if (txtCoinbaseJson != null) txtCoinbaseJson.Visible = isCoinbase;
            if (btnImportCoinbaseJson != null) btnImportCoinbaseJson.Visible = isCoinbase;
            if (lblKeyName != null) lblKeyName.Visible = isCoinbase;
            if (txtKeyName != null) txtKeyName.Visible = isCoinbase;
            if (lblPem != null) lblPem.Visible = isCoinbase;
            if (txtPem != null) txtPem.Visible = isCoinbase;

            if (lblRoutingHint != null)
            {
                lblRoutingHint.Text = GetGeoRoutingHint(service);
            }
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
                    var raw = File.ReadAllText(dialog.FileName);
                    txtCoinbaseJson.Text = raw;
                    NormalizeCoinbaseCredentialFieldsForService(cmbService.SelectedItem == null ? string.Empty : cmbService.SelectedItem.ToString());
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Failed to import Coinbase JSON file: " + ex.Message, "Import Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private string GetGeoRoutingHint(string service)
        {
            var normalized = (service ?? string.Empty).Trim().ToLowerInvariant();
            switch (normalized)
            {
                case "binance-us": return "Uses Binance US endpoint (recommended for US jurisdictions).";
                case "binance-global": return "Uses Binance global endpoint.";
                case "bybit-global": return "Uses Bybit global endpoint.";
                case "okx-global": return "Uses OKX global endpoint.";
                case "binance": return "Uses Binance default endpoint (can be overridden by environment).";
                case "bybit": return "Uses Bybit default endpoint (can be overridden by environment).";
                case "okx": return "Uses OKX default endpoint (can be overridden by environment).";
                default: return "Endpoint routing follows selected service policy.";
            }
        }

        private bool HasRequiredCredentialInputs(ExchangeCredentialPolicy policy)
        {
            if (policy == null) return false;
            if (policy.IsPaper) return true;

            if (policy.RequiresApiKey && string.IsNullOrWhiteSpace(txtApiKey.Text)) return false;
            if (policy.RequiresApiSecret && string.IsNullOrWhiteSpace(txtSecret.Text)) return false;
            if (policy.RequiresPassphrase && string.IsNullOrWhiteSpace(txtPass.Text)) return false;
            if (policy.RequiresApiKeyName && string.IsNullOrWhiteSpace(txtKeyName.Text)) return false;
            if (policy.RequiresEcPrivateKeyPem && string.IsNullOrWhiteSpace(txtPem.Text)) return false;

            return true;
        }
    }
}