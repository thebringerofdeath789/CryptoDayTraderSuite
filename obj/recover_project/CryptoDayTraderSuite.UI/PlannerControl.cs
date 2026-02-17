using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using CryptoDayTraderSuite.Brokers;
using CryptoDayTraderSuite.Exchanges;
using CryptoDayTraderSuite.Models;
using CryptoDayTraderSuite.Services;
using CryptoDayTraderSuite.Util;

namespace CryptoDayTraderSuite.UI
{
	public class PlannerControl : UserControl
	{
		private IHistoryService _historyService;

		private AutoPlannerService _planner;

		private IExchangeClient _client;

		private IAccountService _accountService;

		private IKeyService _keyService;

		private List<TradeRecord> _planned = new List<TradeRecord>();

		private List<PredictionRecord> _preds = new List<PredictionRecord>();

		private List<AccountInfo> _accounts = new List<AccountInfo>();

		private List<ProjectionRow> _scanRows = new List<ProjectionRow>();

		private List<TradePlan> _queuedPlans = new List<TradePlan>();

		private IContainer components = null;

		private TableLayoutPanel mainLayout;

		private FlowLayoutPanel headerBar;

		private Label lblAccount;

		private ComboBox cmbAccount;

		private Label lblRunProduct;

		private ComboBox cmbRunProduct;

		private Label lblGran;

		private ComboBox cmbGran;

		private Label lblLookback;

		private NumericUpDown numLookback;

		private Label lblEquity;

		private NumericUpDown numEquity;

		private SplitContainer mainSplit;

		private Panel leftPanel;

		private FlowLayoutPanel topBar;

		private Button btnRefresh;

		private Button btnSave;

		private Button btnAdd;

		private Label lblProduct;

		private ComboBox cmbFilterProduct;

		private Label lblStrategy;

		private ComboBox cmbFilterStrategy;

		private TableLayoutPanel rightLayout;

		private FlowLayoutPanel actionBar;

		private Button btnScan;

		private Button btnPropose;

		private Button btnExecute;

		private Label lblPlannerStatus;

		private TabControl tabMain;

		private TabPage tabPlanned;

		private DataGridView gridPlanned;

		private TabPage tabPredictions;

		private DataGridView gridPreds;

		private ContextMenuStrip ctxMenu;

		private ToolStripMenuItem miEdit;

		private ToolStripMenuItem miDelete;

		private DataGridViewCheckBoxColumn colEnabled;

		private DataGridViewTextBoxColumn colExchange;

		private DataGridViewTextBoxColumn colProduct;

		private DataGridViewTextBoxColumn colStrategy;

		private DataGridViewTextBoxColumn colSide;

		private DataGridViewTextBoxColumn colQty;

		private DataGridViewTextBoxColumn colPrice;

		private DataGridViewTextBoxColumn colEstEdge;

		private DataGridViewTextBoxColumn colNotes;

		private DataGridViewTextBoxColumn colPredProduct;

		private DataGridViewTextBoxColumn colPredTime;

		private DataGridViewTextBoxColumn colPredHorizon;

		private DataGridViewTextBoxColumn colPredDir;

		private DataGridViewTextBoxColumn colPredProb;

		private DataGridViewTextBoxColumn colPredExpRet;

		private DataGridViewTextBoxColumn colPredExpVol;

		private DataGridViewTextBoxColumn colPredKnown;

		private DataGridViewTextBoxColumn colPredRDir;

		private DataGridViewTextBoxColumn colPredRRet;

		public PlannerControl()
		{
			InitializeComponent();
			Dock = DockStyle.Fill;
			UpdatePlannerStatus("Ready");
			if (btnRefresh != null)
			{
				btnRefresh.Click += delegate
				{
					LoadData();
				};
			}
			if (btnSave != null)
			{
				btnSave.Click += delegate
				{
					SaveData();
				};
			}
			if (btnAdd != null)
			{
				btnAdd.Click += delegate
				{
					AddTrade();
				};
			}
			if (btnScan != null)
			{
				btnScan.Click += delegate
				{
					DoScan();
				};
			}
			if (btnPropose != null)
			{
				btnPropose.Click += delegate
				{
					DoPropose();
				};
			}
			if (btnExecute != null)
			{
				btnExecute.Click += delegate
				{
					DoExecute();
				};
			}
			if (cmbFilterProduct != null)
			{
				cmbFilterProduct.SelectedIndexChanged += delegate
				{
					ApplyFilters();
				};
			}
			if (cmbFilterStrategy != null)
			{
				cmbFilterStrategy.SelectedIndexChanged += delegate
				{
					ApplyFilters();
				};
			}
			if (gridPlanned != null)
			{
				gridPlanned.CellDoubleClick += delegate(object s, DataGridViewCellEventArgs e)
				{
					if (e.RowIndex >= 0)
					{
						EditSelectedTrade();
					}
				};
			}
			if (miEdit != null)
			{
				miEdit.Click += delegate
				{
					EditSelectedTrade();
				};
			}
			if (miDelete != null)
			{
				miDelete.Click += delegate
				{
					DeleteSelectedTrade();
				};
			}
			if (cmbGran != null && cmbGran.Items.Count > 0 && cmbGran.SelectedIndex < 0)
			{
				cmbGran.SelectedIndex = 2;
			}
			if (numLookback != null && numLookback.Value < 5m)
			{
				numLookback.Value = 30m;
			}
			if (numEquity != null && numEquity.Value < 10m)
			{
				numEquity.Value = 1000m;
			}
		}

		public void Initialize(IHistoryService historyService)
		{
			Initialize(historyService, null, null, null, null);
		}

		public void Initialize(IHistoryService historyService, AutoPlannerService planner, IExchangeClient client, IAccountService accountService, IKeyService keyService)
		{
			_historyService = historyService;
			_planner = planner;
			_client = client;
			_accountService = accountService;
			_keyService = keyService;
			LoadAccounts();
			LoadProducts();
			LoadData();
		}

		private void LoadAccounts()
		{
			if (_accountService == null || cmbAccount == null)
			{
				return;
			}
			_accounts = (from a in _accountService.GetAll()
				where a.Enabled
				select a).ToList();
			cmbAccount.Items.Clear();
			foreach (AccountInfo account in _accounts)
			{
				cmbAccount.Items.Add(account.Label + " [" + account.Service + "]");
			}
			if (cmbAccount.Items.Count > 0 && cmbAccount.SelectedIndex < 0)
			{
				cmbAccount.SelectedIndex = 0;
			}
		}

		private async void LoadProducts()
		{
			if (cmbRunProduct == null)
			{
				return;
			}
			try
			{
				List<string> products = ((_client == null) ? (await new CoinbasePublicClient().GetProductsAsync()) : (await _client.ListProductsAsync()));
				cmbRunProduct.Items.Clear();
				foreach (string product in products.Where((string p) => p.Contains("USD")))
				{
					cmbRunProduct.Items.Add(product);
				}
				if (cmbRunProduct.Items.Count > 0 && cmbRunProduct.SelectedIndex < 0)
				{
					cmbRunProduct.SelectedIndex = 0;
				}
			}
			catch (Exception ex)
			{
				Log.Error("[Planner] Failed to load products", ex, "LoadProducts", "C:\\Users\\admin\\Documents\\Visual Studio 2022\\Projects\\CryptoDayTraderSuite\\UI\\PlannerControl.cs", 118);
			}
		}

		private void LoadData()
		{
			if (_historyService == null)
			{
				return;
			}
			_planned = _historyService.LoadPlannedTrades() ?? new List<TradeRecord>();
			_preds = _historyService.LoadPredictions() ?? new List<PredictionRecord>();
			List<string> products = (from x in _planned.Select((TradeRecord p) => p.ProductId).Distinct()
				orderby x
				select x).ToList();
			string currentProd = cmbFilterProduct.SelectedItem as string;
			cmbFilterProduct.Items.Clear();
			cmbFilterProduct.Items.Add("All");
			ComboBox.ObjectCollection items = cmbFilterProduct.Items;
			object[] items2 = products.ToArray();
			items.AddRange(items2);
			if (currentProd != null && cmbFilterProduct.Items.Contains(currentProd))
			{
				cmbFilterProduct.SelectedItem = currentProd;
			}
			else
			{
				cmbFilterProduct.SelectedIndex = 0;
			}
			List<string> strategies = (from x in _planned.Select((TradeRecord p) => p.Strategy).Distinct()
				orderby x
				select x).ToList();
			string currentStrat = cmbFilterStrategy.SelectedItem as string;
			cmbFilterStrategy.Items.Clear();
			cmbFilterStrategy.Items.Add("All");
			ComboBox.ObjectCollection items3 = cmbFilterStrategy.Items;
			items2 = strategies.ToArray();
			items3.AddRange(items2);
			if (currentStrat != null && cmbFilterStrategy.Items.Contains(currentStrat))
			{
				cmbFilterStrategy.SelectedItem = currentStrat;
			}
			else
			{
				cmbFilterStrategy.SelectedIndex = 0;
			}
			ApplyFilters();
			gridPreds.Rows.Clear();
			foreach (PredictionRecord r in _preds.OrderByDescending((PredictionRecord x) => x.AtUtc).Take(500))
			{
				gridPreds.Rows.Add(r.ProductId, r.AtUtc.ToLocalTime(), r.HorizonMinutes, r.Direction, r.Probability, r.ExpectedReturn, r.ExpectedVol, r.RealizedKnown, r.RealizedDirection, r.RealizedReturn);
			}
			UpdatePlannerStatus($"Loaded {_planned.Count} planned trade(s), {_preds.Count} prediction row(s)");
		}

		private List<TradeRecord> GetFilteredPlanned()
		{
			string prod = cmbFilterProduct.SelectedItem?.ToString();
			string strat = cmbFilterStrategy.SelectedItem?.ToString();
			return (from x in _planned
				where (prod == "All" || string.IsNullOrEmpty(prod) || x.ProductId == prod) && (strat == "All" || string.IsNullOrEmpty(strat) || x.Strategy == strat)
				orderby x.AtUtc descending
				select x).ToList();
		}

		private void ApplyFilters()
		{
			if (_planned == null)
			{
				return;
			}
			List<TradeRecord> filtered = GetFilteredPlanned();
			gridPlanned.Rows.Clear();
			foreach (TradeRecord p in filtered)
			{
				gridPlanned.Rows.Add(p.Enabled, p.Exchange, p.ProductId, p.Strategy, p.Side, p.Quantity, p.Price, p.EstEdge, p.Notes);
			}
		}

		private void SaveData()
		{
			if (_historyService == null)
			{
				return;
			}
			List<TradeRecord> filtered = GetFilteredPlanned();
			if (gridPlanned.Rows.Count != filtered.Count)
			{
				UpdatePlannerStatus("Save aborted: filter mismatch; data reloaded", warn: true);
				LoadData();
				return;
			}
			for (int i = 0; i < gridPlanned.Rows.Count; i++)
			{
				DataGridViewRow row = gridPlanned.Rows[i];
				TradeRecord p = filtered[i];
				p.Enabled = Convert.ToBoolean(row.Cells[0].Value ?? ((object)false));
				p.Quantity = Convert.ToDecimal(row.Cells[5].Value ?? ((object)0m));
				p.Price = Convert.ToDecimal(row.Cells[6].Value ?? ((object)0m));
				p.Notes = row.Cells[8].Value?.ToString();
			}
			_historyService.SavePlannedTrades(_planned);
			UpdatePlannerStatus("Planner updated");
		}

		private void AddTrade()
		{
			TradeEditDialog dlg = new TradeEditDialog();
			if (dlg.ShowDialog(this) == DialogResult.OK)
			{
				_planned.Add(dlg.Result);
				SaveData();
				LoadData();
			}
		}

		private void EditSelectedTrade()
		{
			if (gridPlanned.SelectedRows.Count == 0)
			{
				return;
			}
			int idx = gridPlanned.SelectedRows[0].Index;
			List<TradeRecord> filtered = GetFilteredPlanned();
			if (idx >= 0 && idx < filtered.Count)
			{
				TradeRecord rec = filtered[idx];
				TradeEditDialog dlg = new TradeEditDialog(rec);
				if (dlg.ShowDialog(this) == DialogResult.OK)
				{
					rec.Enabled = dlg.Result.Enabled;
					rec.Quantity = dlg.Result.Quantity;
					rec.Price = dlg.Result.Price;
					rec.Notes = dlg.Result.Notes;
					SaveData();
					LoadData();
				}
			}
		}

		private void DeleteSelectedTrade()
		{
			if (gridPlanned.SelectedRows.Count == 0)
			{
				return;
			}
			int idx = gridPlanned.SelectedRows[0].Index;
			List<TradeRecord> filtered = GetFilteredPlanned();
			if (idx >= 0 && idx < filtered.Count)
			{
				TradeRecord rec = filtered[idx];
				if (MessageBox.Show("Delete planned trade for " + rec.ProductId + " (" + rec.Strategy + ")?", "Confirm", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
				{
					_planned.Remove(rec);
					SaveData();
					LoadData();
				}
			}
		}

		private async void DoScan()
		{
			if (_planner == null)
			{
				UpdatePlannerStatus("Scan unavailable: planner service missing", warn: true);
				return;
			}
			if (cmbRunProduct == null || cmbRunProduct.SelectedItem == null)
			{
				UpdatePlannerStatus("Scan blocked: select a product first", warn: true);
				return;
			}
			if (!int.TryParse(cmbGran.SelectedItem?.ToString(), out var gran))
			{
				gran = 15;
			}
			int lookbackMins = (int)numLookback.Value * 1440;
			string symbol = cmbRunProduct.SelectedItem.ToString();
			btnScan.Enabled = false;
			try
			{
				_scanRows = await _planner.ProjectAsync(symbol, gran, lookbackMins, 0.006m, 0.004m);
				if (_scanRows == null || _scanRows.Count == 0)
				{
					UpdatePlannerStatus("Scan complete: no projection rows", warn: true);
					return;
				}
				gridPreds.Rows.Clear();
				foreach (ProjectionRow r in _scanRows.OrderByDescending((ProjectionRow x) => x.Expectancy))
				{
					gridPreds.Rows.Add(r.Symbol, DateTime.UtcNow.ToLocalTime(), r.GranMinutes, (r.Expectancy >= 0.0) ? "Bul" : "Bea", r.WinRate, r.Expectancy, r.Samples, 0, 0, 0);
				}
				ProjectionRow best = _scanRows.OrderByDescending((ProjectionRow projectionRow) => projectionRow.Expectancy).First();
				UpdatePlannerStatus($"Scan complete: {symbol} top {best.Strategy} (Exp {best.Expectancy:0.00}, Win {best.WinRate:0.0}%)");
			}
			catch (Exception ex)
			{
				Exception ex2 = ex;
				Log.Error("[Planner] Scan failed", ex2, "DoScan", "C:\\Users\\admin\\Documents\\Visual Studio 2022\\Projects\\CryptoDayTraderSuite\\UI\\PlannerControl.cs", 305);
				MessageBox.Show("Scan failed: " + ex2.Message, "Planner", MessageBoxButtons.OK, MessageBoxIcon.Hand);
				UpdatePlannerStatus("Scan failed: " + ex2.Message, warn: true);
			}
			finally
			{
				btnScan.Enabled = true;
			}
		}

		private async void DoPropose()
		{
			bool proposeDisabled = false;
			try
			{
				if (_planner == null)
				{
					UpdatePlannerStatus("Propose unavailable: planner service missing", warn: true);
					return;
				}
				if (_scanRows == null || _scanRows.Count == 0)
				{
					UpdatePlannerStatus("Propose blocked: run Scan first", warn: true);
					return;
				}
				if (_accounts == null || cmbAccount == null || cmbAccount.SelectedIndex < 0 || cmbAccount.SelectedIndex >= _accounts.Count)
				{
					UpdatePlannerStatus("Propose blocked: select an account first", warn: true);
					return;
				}
				if (cmbRunProduct == null || cmbRunProduct.SelectedItem == null)
				{
					UpdatePlannerStatus("Propose blocked: select a product first", warn: true);
					return;
				}
				AccountInfo account = _accounts[cmbAccount.SelectedIndex];
				if (!int.TryParse(cmbGran.SelectedItem?.ToString(), out var gran))
				{
					gran = 15;
				}
				string symbol = cmbRunProduct.SelectedItem.ToString();
				btnPropose.Enabled = false;
				proposeDisabled = true;
				List<TradePlan> plans = await _planner.ProposeAsync(account.Id, symbol, gran, numEquity.Value, account.RiskPerTradePct, _scanRows);
				_queuedPlans = plans ?? new List<TradePlan>();
				if (plans == null || plans.Count == 0)
				{
					UpdatePlannerStatus("Propose complete: no active signal (No Signal/Risk Guard/AI Veto)", warn: true);
					return;
				}
				int added = 0;
				foreach (TradePlan plan in plans)
				{
					if (!_planned.Any((TradeRecord p) => p.ProductId == plan.Symbol && p.Strategy == plan.Strategy && !p.Executed))
					{
						TradeRecord rec = new TradeRecord
						{
							Enabled = true,
							Exchange = account.Service,
							ProductId = (plan.Symbol ?? string.Empty).Replace("/", "-"),
							Strategy = plan.Strategy,
							Side = ((plan.Direction > 0) ? "Buy" : "Sell"),
							Quantity = plan.Qty,
							Price = plan.Entry,
							EstEdge = CalculateEdge(plan),
							AtUtc = DateTime.UtcNow,
							Executed = false,
							Notes = plan.Note
						};
						_planned.Add(rec);
						added++;
					}
				}
				if (added > 0)
				{
					_historyService?.SavePlannedTrades(_planned);
					ApplyFilters();
					UpdatePlannerStatus($"Propose complete: {added} plan(s) added; {_queuedPlans.Count} queued for execute");
				}
				else
				{
					UpdatePlannerStatus($"Propose complete: 0 added (duplicates); {_queuedPlans.Count} queued for execute", warn: true);
				}
			}
			catch (Exception ex)
			{
				Exception ex2 = ex;
				Log.Error("[Planner] Propose failed", ex2, "DoPropose", "C:\\Users\\admin\\Documents\\Visual Studio 2022\\Projects\\CryptoDayTraderSuite\\UI\\PlannerControl.cs", 399);
				MessageBox.Show("Propose failed: " + ex2.Message, "Planner", MessageBoxButtons.OK, MessageBoxIcon.Hand);
				UpdatePlannerStatus("Propose failed: " + ex2.Message, warn: true);
			}
			finally
			{
				if (proposeDisabled)
				{
					btnPropose.Enabled = true;
				}
			}
		}

		private async void DoExecute()
		{
			bool executeDisabled = false;
			try
			{
				if (_queuedPlans == null || _queuedPlans.Count == 0)
				{
					UpdatePlannerStatus("Execute blocked: no queued plans (run Propose first)", warn: true);
					return;
				}
				if (_accounts == null || cmbAccount == null || cmbAccount.SelectedIndex < 0 || cmbAccount.SelectedIndex >= _accounts.Count)
				{
					UpdatePlannerStatus("Execute blocked: select an account first", warn: true);
					return;
				}
				AccountInfo account = _accounts[cmbAccount.SelectedIndex];
				IBroker broker = BrokerFactory.GetBroker(account.Service, account.Mode, _keyService, _accountService);
				if (broker == null)
				{
					MessageBox.Show("Unsupported broker: " + account.Service, "Planner", MessageBoxButtons.OK, MessageBoxIcon.Hand);
					return;
				}
				BrokerCapabilities caps = broker.GetCapabilities();
				if (caps == null)
				{
					MessageBox.Show("Broker capabilities unavailable.", "Planner", MessageBoxButtons.OK, MessageBoxIcon.Hand);
					return;
				}
				if (!caps.SupportsMarketEntry)
				{
					MessageBox.Show("Selected broker does not support market entry.", "Planner", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
					return;
				}
				if (account.Mode != AccountMode.Paper && !caps.SupportsProtectiveExits)
				{
					string reason = ((!string.IsNullOrWhiteSpace(caps.Notes)) ? caps.Notes : "Protective exits are required for live execution.");
					MessageBox.Show("Execution blocked: " + reason, "Planner", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
					return;
				}
				btnExecute.Enabled = false;
				executeDisabled = true;
				foreach (TradePlan plan in _queuedPlans.Where((TradePlan p) => p.AccountId == account.Id))
				{
					(bool ok, string message) validation = broker.ValidateTradePlan(plan);
					if (!validation.ok)
					{
						Log.Warn("[Planner] Plan blocked by broker validation: " + validation.message + " | symbol=" + plan.Symbol + " | account=" + account.Id, "DoExecute", "C:\\Users\\admin\\Documents\\Visual Studio 2022\\Projects\\CryptoDayTraderSuite\\UI\\PlannerControl.cs", 464);
						continue;
					}
					(bool ok, string message) result = await broker.PlaceOrderAsync(plan);
					if (result.ok)
					{
						TradeRecord planned = _planned.LastOrDefault((TradeRecord t) => t.ProductId == (plan.Symbol ?? string.Empty).Replace("/", "-") && t.Strategy == plan.Strategy && !t.Executed);
						if (planned != null)
						{
							planned.Executed = true;
							planned.FillPrice = plan.Entry;
						}
					}
					else
					{
						Log.Warn("[Planner] Execute failed: " + result.message, "DoExecute", "C:\\Users\\admin\\Documents\\Visual Studio 2022\\Projects\\CryptoDayTraderSuite\\UI\\PlannerControl.cs", 482);
					}
				}
				_historyService?.SavePlannedTrades(_planned);
				ApplyFilters();
				UpdatePlannerStatus("Execution cycle complete");
			}
			catch (Exception ex)
			{
				Exception ex2 = ex;
				Log.Error("[Planner] Execute failed", ex2, "DoExecute", "C:\\Users\\admin\\Documents\\Visual Studio 2022\\Projects\\CryptoDayTraderSuite\\UI\\PlannerControl.cs", 492);
				MessageBox.Show("Execute failed: " + ex2.Message, "Planner", MessageBoxButtons.OK, MessageBoxIcon.Hand);
				UpdatePlannerStatus("Execute failed: " + ex2.Message, warn: true);
			}
			finally
			{
				if (executeDisabled)
				{
					btnExecute.Enabled = true;
				}
			}
		}

		private void UpdatePlannerStatus(string message, bool warn = false)
		{
			if (lblPlannerStatus != null)
			{
				string suffix = DateTime.Now.ToString("HH:mm:ss");
				lblPlannerStatus.Text = "Status: " + message + " · " + suffix;
				lblPlannerStatus.ForeColor = (warn ? Color.DarkOrange : Color.DarkGreen);
			}
		}

		private decimal CalculateEdge(TradePlan plan)
		{
			if (plan == null || plan.Entry <= 0m)
			{
				return 0m;
			}
			if (plan.Direction > 0)
			{
				return (plan.Target - plan.Entry) / plan.Entry;
			}
			return (plan.Entry - plan.Target) / plan.Entry;
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
			this.components = new System.ComponentModel.Container();
			this.mainLayout = new System.Windows.Forms.TableLayoutPanel();
			this.headerBar = new System.Windows.Forms.FlowLayoutPanel();
			this.lblAccount = new System.Windows.Forms.Label();
			this.cmbAccount = new System.Windows.Forms.ComboBox();
			this.lblRunProduct = new System.Windows.Forms.Label();
			this.cmbRunProduct = new System.Windows.Forms.ComboBox();
			this.lblGran = new System.Windows.Forms.Label();
			this.cmbGran = new System.Windows.Forms.ComboBox();
			this.lblLookback = new System.Windows.Forms.Label();
			this.numLookback = new System.Windows.Forms.NumericUpDown();
			this.lblEquity = new System.Windows.Forms.Label();
			this.numEquity = new System.Windows.Forms.NumericUpDown();
			this.mainSplit = new System.Windows.Forms.SplitContainer();
			this.leftPanel = new System.Windows.Forms.Panel();
			this.topBar = new System.Windows.Forms.FlowLayoutPanel();
			this.btnRefresh = new System.Windows.Forms.Button();
			this.btnSave = new System.Windows.Forms.Button();
			this.btnAdd = new System.Windows.Forms.Button();
			this.lblProduct = new System.Windows.Forms.Label();
			this.cmbFilterProduct = new System.Windows.Forms.ComboBox();
			this.lblStrategy = new System.Windows.Forms.Label();
			this.cmbFilterStrategy = new System.Windows.Forms.ComboBox();
			this.rightLayout = new System.Windows.Forms.TableLayoutPanel();
			this.actionBar = new System.Windows.Forms.FlowLayoutPanel();
			this.btnScan = new System.Windows.Forms.Button();
			this.btnPropose = new System.Windows.Forms.Button();
			this.btnExecute = new System.Windows.Forms.Button();
			this.lblPlannerStatus = new System.Windows.Forms.Label();
			this.tabMain = new System.Windows.Forms.TabControl();
			this.tabPlanned = new System.Windows.Forms.TabPage();
			this.gridPlanned = new System.Windows.Forms.DataGridView();
			this.colEnabled = new System.Windows.Forms.DataGridViewCheckBoxColumn();
			this.colExchange = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.colProduct = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.colStrategy = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.colSide = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.colQty = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.colPrice = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.colEstEdge = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.colNotes = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.ctxMenu = new System.Windows.Forms.ContextMenuStrip(this.components);
			this.miEdit = new System.Windows.Forms.ToolStripMenuItem();
			this.miDelete = new System.Windows.Forms.ToolStripMenuItem();
			this.tabPredictions = new System.Windows.Forms.TabPage();
			this.gridPreds = new System.Windows.Forms.DataGridView();
			this.colPredProduct = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.colPredTime = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.colPredHorizon = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.colPredDir = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.colPredProb = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.colPredExpRet = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.colPredExpVol = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.colPredKnown = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.colPredRDir = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.colPredRRet = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.mainLayout.SuspendLayout();
			this.headerBar.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)this.numLookback).BeginInit();
			((System.ComponentModel.ISupportInitialize)this.numEquity).BeginInit();
			((System.ComponentModel.ISupportInitialize)this.mainSplit).BeginInit();
			this.mainSplit.Panel1.SuspendLayout();
			this.mainSplit.Panel2.SuspendLayout();
			this.mainSplit.SuspendLayout();
			this.leftPanel.SuspendLayout();
			this.topBar.SuspendLayout();
			this.rightLayout.SuspendLayout();
			this.actionBar.SuspendLayout();
			this.tabMain.SuspendLayout();
			this.tabPlanned.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)this.gridPlanned).BeginInit();
			this.ctxMenu.SuspendLayout();
			this.tabPredictions.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)this.gridPreds).BeginInit();
			base.SuspendLayout();
			this.mainLayout.ColumnCount = 1;
			this.mainLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100f));
			this.mainLayout.Controls.Add(this.headerBar, 0, 0);
			this.mainLayout.Controls.Add(this.mainSplit, 0, 1);
			this.mainLayout.Dock = System.Windows.Forms.DockStyle.Fill;
			this.mainLayout.Location = new System.Drawing.Point(0, 0);
			this.mainLayout.Name = "mainLayout";
			this.mainLayout.RowCount = 2;
			this.mainLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
			this.mainLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100f));
			this.mainLayout.Size = new System.Drawing.Size(1200, 800);
			this.mainLayout.TabIndex = 0;
			this.headerBar.AutoSize = true;
			this.headerBar.Controls.Add(this.lblAccount);
			this.headerBar.Controls.Add(this.cmbAccount);
			this.headerBar.Controls.Add(this.lblRunProduct);
			this.headerBar.Controls.Add(this.cmbRunProduct);
			this.headerBar.Controls.Add(this.lblGran);
			this.headerBar.Controls.Add(this.cmbGran);
			this.headerBar.Controls.Add(this.lblLookback);
			this.headerBar.Controls.Add(this.numLookback);
			this.headerBar.Controls.Add(this.lblEquity);
			this.headerBar.Controls.Add(this.numEquity);
			this.headerBar.Dock = System.Windows.Forms.DockStyle.Fill;
			this.headerBar.Location = new System.Drawing.Point(3, 3);
			this.headerBar.Name = "headerBar";
			this.headerBar.Padding = new System.Windows.Forms.Padding(8);
			this.headerBar.Size = new System.Drawing.Size(1194, 40);
			this.headerBar.TabIndex = 0;
			this.headerBar.WrapContents = true;
			this.lblAccount.AutoSize = true;
			this.lblAccount.Location = new System.Drawing.Point(11, 14);
			this.lblAccount.Margin = new System.Windows.Forms.Padding(3, 6, 0, 0);
			this.lblAccount.Name = "lblAccount";
			this.lblAccount.Size = new System.Drawing.Size(50, 13);
			this.lblAccount.TabIndex = 0;
			this.lblAccount.Text = "Account:";
			this.cmbAccount.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.cmbAccount.FormattingEnabled = true;
			this.cmbAccount.Location = new System.Drawing.Point(64, 11);
			this.cmbAccount.Name = "cmbAccount";
			this.cmbAccount.Size = new System.Drawing.Size(180, 21);
			this.cmbAccount.TabIndex = 1;
			this.lblRunProduct.AutoSize = true;
			this.lblRunProduct.Location = new System.Drawing.Point(259, 14);
			this.lblRunProduct.Margin = new System.Windows.Forms.Padding(12, 6, 0, 0);
			this.lblRunProduct.Name = "lblRunProduct";
			this.lblRunProduct.Size = new System.Drawing.Size(44, 13);
			this.lblRunProduct.TabIndex = 2;
			this.lblRunProduct.Text = "Symbol:";
			this.cmbRunProduct.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.cmbRunProduct.FormattingEnabled = true;
			this.cmbRunProduct.Location = new System.Drawing.Point(306, 11);
			this.cmbRunProduct.Name = "cmbRunProduct";
			this.cmbRunProduct.Size = new System.Drawing.Size(160, 21);
			this.cmbRunProduct.TabIndex = 3;
			this.lblGran.AutoSize = true;
			this.lblGran.Location = new System.Drawing.Point(481, 14);
			this.lblGran.Margin = new System.Windows.Forms.Padding(12, 6, 0, 0);
			this.lblGran.Name = "lblGran";
			this.lblGran.Size = new System.Drawing.Size(37, 13);
			this.lblGran.TabIndex = 4;
			this.lblGran.Text = "Gran:";
			this.cmbGran.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.cmbGran.FormattingEnabled = true;
			this.cmbGran.Items.AddRange(new object[6] { "1", "5", "15", "30", "60", "240" });
			this.cmbGran.Location = new System.Drawing.Point(521, 11);
			this.cmbGran.Name = "cmbGran";
			this.cmbGran.Size = new System.Drawing.Size(70, 21);
			this.cmbGran.TabIndex = 5;
			this.lblLookback.AutoSize = true;
			this.lblLookback.Location = new System.Drawing.Point(606, 14);
			this.lblLookback.Margin = new System.Windows.Forms.Padding(12, 6, 0, 0);
			this.lblLookback.Name = "lblLookback";
			this.lblLookback.Size = new System.Drawing.Size(76, 13);
			this.lblLookback.TabIndex = 6;
			this.lblLookback.Text = "Lookback (d):";
			this.numLookback.Location = new System.Drawing.Point(685, 11);
			this.numLookback.Maximum = new decimal(new int[4] { 120, 0, 0, 0 });
			this.numLookback.Minimum = new decimal(new int[4] { 5, 0, 0, 0 });
			this.numLookback.Name = "numLookback";
			this.numLookback.Size = new System.Drawing.Size(70, 20);
			this.numLookback.TabIndex = 7;
			this.numLookback.Value = new decimal(new int[4] { 30, 0, 0, 0 });
			this.lblEquity.AutoSize = true;
			this.lblEquity.Location = new System.Drawing.Point(770, 14);
			this.lblEquity.Margin = new System.Windows.Forms.Padding(12, 6, 0, 0);
			this.lblEquity.Name = "lblEquity";
			this.lblEquity.Size = new System.Drawing.Size(42, 13);
			this.lblEquity.TabIndex = 8;
			this.lblEquity.Text = "Equity:";
			this.numEquity.Location = new System.Drawing.Point(815, 11);
			this.numEquity.Maximum = new decimal(new int[4] { 1000000, 0, 0, 0 });
			this.numEquity.Minimum = new decimal(new int[4] { 10, 0, 0, 0 });
			this.numEquity.Name = "numEquity";
			this.numEquity.Size = new System.Drawing.Size(90, 20);
			this.numEquity.TabIndex = 9;
			this.numEquity.Value = new decimal(new int[4] { 1000, 0, 0, 0 });
			this.mainSplit.Dock = System.Windows.Forms.DockStyle.Fill;
			this.mainSplit.Location = new System.Drawing.Point(3, 49);
			this.mainSplit.Name = "mainSplit";
			this.mainSplit.Panel1.Controls.Add(this.leftPanel);
			this.mainSplit.Panel2.Controls.Add(this.rightLayout);
			this.mainSplit.Size = new System.Drawing.Size(1194, 748);
			this.mainSplit.SplitterDistance = 320;
			this.mainSplit.TabIndex = 1;
			this.leftPanel.Controls.Add(this.topBar);
			this.leftPanel.Dock = System.Windows.Forms.DockStyle.Fill;
			this.leftPanel.Location = new System.Drawing.Point(0, 0);
			this.leftPanel.Name = "leftPanel";
			this.leftPanel.Padding = new System.Windows.Forms.Padding(8);
			this.leftPanel.Size = new System.Drawing.Size(320, 748);
			this.leftPanel.TabIndex = 0;
			this.topBar.AutoScroll = true;
			this.topBar.Controls.Add(this.btnRefresh);
			this.topBar.Controls.Add(this.btnSave);
			this.topBar.Controls.Add(this.btnAdd);
			this.topBar.Controls.Add(this.lblProduct);
			this.topBar.Controls.Add(this.cmbFilterProduct);
			this.topBar.Controls.Add(this.lblStrategy);
			this.topBar.Controls.Add(this.cmbFilterStrategy);
			this.topBar.Dock = System.Windows.Forms.DockStyle.Fill;
			this.topBar.FlowDirection = System.Windows.Forms.FlowDirection.TopDown;
			this.topBar.Location = new System.Drawing.Point(8, 8);
			this.topBar.Name = "topBar";
			this.topBar.Size = new System.Drawing.Size(304, 732);
			this.topBar.TabIndex = 0;
			this.topBar.WrapContents = false;
			this.btnRefresh.Location = new System.Drawing.Point(3, 3);
			this.btnRefresh.Name = "btnRefresh";
			this.btnRefresh.Size = new System.Drawing.Size(140, 26);
			this.btnRefresh.TabIndex = 0;
			this.btnRefresh.Text = "Refresh";
			this.btnRefresh.UseVisualStyleBackColor = true;
			this.btnSave.Location = new System.Drawing.Point(3, 35);
			this.btnSave.Name = "btnSave";
			this.btnSave.Size = new System.Drawing.Size(140, 26);
			this.btnSave.TabIndex = 1;
			this.btnSave.Text = "Save";
			this.btnSave.UseVisualStyleBackColor = true;
			this.btnAdd.Location = new System.Drawing.Point(3, 67);
			this.btnAdd.Name = "btnAdd";
			this.btnAdd.Size = new System.Drawing.Size(140, 26);
			this.btnAdd.TabIndex = 2;
			this.btnAdd.Text = "Add Trade";
			this.btnAdd.UseVisualStyleBackColor = true;
			this.lblProduct.AutoSize = true;
			this.lblProduct.Location = new System.Drawing.Point(3, 104);
			this.lblProduct.Margin = new System.Windows.Forms.Padding(3, 8, 3, 0);
			this.lblProduct.Name = "lblProduct";
			this.lblProduct.Size = new System.Drawing.Size(68, 13);
			this.lblProduct.TabIndex = 3;
			this.lblProduct.Text = "Filter product";
			this.cmbFilterProduct.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.cmbFilterProduct.FormattingEnabled = true;
			this.cmbFilterProduct.Location = new System.Drawing.Point(3, 120);
			this.cmbFilterProduct.Name = "cmbFilterProduct";
			this.cmbFilterProduct.Size = new System.Drawing.Size(220, 21);
			this.cmbFilterProduct.TabIndex = 4;
			this.lblStrategy.AutoSize = true;
			this.lblStrategy.Location = new System.Drawing.Point(3, 152);
			this.lblStrategy.Margin = new System.Windows.Forms.Padding(3, 8, 3, 0);
			this.lblStrategy.Name = "lblStrategy";
			this.lblStrategy.Size = new System.Drawing.Size(66, 13);
			this.lblStrategy.TabIndex = 5;
			this.lblStrategy.Text = "Filter strategy";
			this.cmbFilterStrategy.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.cmbFilterStrategy.FormattingEnabled = true;
			this.cmbFilterStrategy.Location = new System.Drawing.Point(3, 168);
			this.cmbFilterStrategy.Name = "cmbFilterStrategy";
			this.cmbFilterStrategy.Size = new System.Drawing.Size(220, 21);
			this.cmbFilterStrategy.TabIndex = 6;
			this.rightLayout.ColumnCount = 1;
			this.rightLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100f));
			this.rightLayout.Controls.Add(this.actionBar, 0, 0);
			this.rightLayout.Controls.Add(this.tabMain, 0, 1);
			this.rightLayout.Dock = System.Windows.Forms.DockStyle.Fill;
			this.rightLayout.Location = new System.Drawing.Point(0, 0);
			this.rightLayout.Name = "rightLayout";
			this.rightLayout.RowCount = 2;
			this.rightLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
			this.rightLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100f));
			this.rightLayout.Size = new System.Drawing.Size(870, 748);
			this.rightLayout.TabIndex = 0;
			this.actionBar.AutoSize = true;
			this.actionBar.Controls.Add(this.btnScan);
			this.actionBar.Controls.Add(this.btnPropose);
			this.actionBar.Controls.Add(this.btnExecute);
			this.actionBar.Controls.Add(this.lblPlannerStatus);
			this.actionBar.Dock = System.Windows.Forms.DockStyle.Fill;
			this.actionBar.Location = new System.Drawing.Point(3, 3);
			this.actionBar.Name = "actionBar";
			this.actionBar.Padding = new System.Windows.Forms.Padding(4);
			this.actionBar.Size = new System.Drawing.Size(864, 35);
			this.actionBar.TabIndex = 0;
			this.btnScan.Location = new System.Drawing.Point(7, 7);
			this.btnScan.Name = "btnScan";
			this.btnScan.Size = new System.Drawing.Size(90, 23);
			this.btnScan.TabIndex = 0;
			this.btnScan.Text = "Scan";
			this.btnScan.UseVisualStyleBackColor = true;
			this.btnPropose.Location = new System.Drawing.Point(103, 7);
			this.btnPropose.Name = "btnPropose";
			this.btnPropose.Size = new System.Drawing.Size(90, 23);
			this.btnPropose.TabIndex = 1;
			this.btnPropose.Text = "Propose";
			this.btnPropose.UseVisualStyleBackColor = true;
			this.btnExecute.Location = new System.Drawing.Point(199, 7);
			this.btnExecute.Name = "btnExecute";
			this.btnExecute.Size = new System.Drawing.Size(90, 23);
			this.btnExecute.TabIndex = 2;
			this.btnExecute.Text = "Execute";
			this.btnExecute.UseVisualStyleBackColor = true;
			this.lblPlannerStatus.AutoSize = true;
			this.lblPlannerStatus.ForeColor = System.Drawing.Color.DimGray;
			this.lblPlannerStatus.Location = new System.Drawing.Point(295, 12);
			this.lblPlannerStatus.Margin = new System.Windows.Forms.Padding(3, 8, 3, 0);
			this.lblPlannerStatus.Name = "lblPlannerStatus";
			this.lblPlannerStatus.Size = new System.Drawing.Size(98, 13);
			this.lblPlannerStatus.TabIndex = 3;
			this.lblPlannerStatus.Text = "Status: Not started";
			this.tabMain.Controls.Add(this.tabPlanned);
			this.tabMain.Controls.Add(this.tabPredictions);
			this.tabMain.Dock = System.Windows.Forms.DockStyle.Fill;
			this.tabMain.Location = new System.Drawing.Point(3, 44);
			this.tabMain.Name = "tabMain";
			this.tabMain.SelectedIndex = 0;
			this.tabMain.Size = new System.Drawing.Size(864, 701);
			this.tabMain.TabIndex = 1;
			this.tabPlanned.Controls.Add(this.gridPlanned);
			this.tabPlanned.Location = new System.Drawing.Point(4, 22);
			this.tabPlanned.Name = "tabPlanned";
			this.tabPlanned.Padding = new System.Windows.Forms.Padding(3);
			this.tabPlanned.Size = new System.Drawing.Size(856, 675);
			this.tabPlanned.TabIndex = 0;
			this.tabPlanned.Text = "Planned Trades";
			this.tabPlanned.UseVisualStyleBackColor = true;
			this.gridPlanned.AllowUserToAddRows = false;
			this.gridPlanned.AllowUserToDeleteRows = false;
			this.gridPlanned.AutoGenerateColumns = false;
			this.gridPlanned.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
			this.gridPlanned.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
			this.gridPlanned.Columns.AddRange(this.colEnabled, this.colExchange, this.colProduct, this.colStrategy, this.colSide, this.colQty, this.colPrice, this.colEstEdge, this.colNotes);
			this.gridPlanned.ContextMenuStrip = this.ctxMenu;
			this.gridPlanned.Dock = System.Windows.Forms.DockStyle.Fill;
			this.gridPlanned.Location = new System.Drawing.Point(3, 3);
			this.gridPlanned.MultiSelect = false;
			this.gridPlanned.Name = "gridPlanned";
			this.gridPlanned.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
			this.gridPlanned.Size = new System.Drawing.Size(850, 669);
			this.gridPlanned.TabIndex = 0;
			this.colEnabled.DataPropertyName = "Enabled";
			this.colEnabled.HeaderText = "Enabled";
			this.colEnabled.Name = "colEnabled";
			this.colExchange.DataPropertyName = "Exchange";
			this.colExchange.HeaderText = "Exchange";
			this.colExchange.Name = "colExchange";
			this.colExchange.ReadOnly = true;
			this.colProduct.DataPropertyName = "ProductId";
			this.colProduct.HeaderText = "Product";
			this.colProduct.Name = "colProduct";
			this.colProduct.ReadOnly = true;
			this.colStrategy.DataPropertyName = "Strategy";
			this.colStrategy.HeaderText = "Strategy";
			this.colStrategy.Name = "colStrategy";
			this.colStrategy.ReadOnly = true;
			this.colSide.DataPropertyName = "Side";
			this.colSide.HeaderText = "Side";
			this.colSide.Name = "colSide";
			this.colSide.ReadOnly = true;
			this.colQty.DataPropertyName = "Quantity";
			this.colQty.HeaderText = "Qty";
			this.colQty.Name = "colQty";
			this.colPrice.DataPropertyName = "Price";
			this.colPrice.HeaderText = "Price";
			this.colPrice.Name = "colPrice";
			this.colEstEdge.DataPropertyName = "EstEdge";
			this.colEstEdge.HeaderText = "Est. Edge";
			this.colEstEdge.Name = "colEstEdge";
			this.colEstEdge.ReadOnly = true;
			this.colNotes.DataPropertyName = "Notes";
			this.colNotes.HeaderText = "Notes";
			this.colNotes.Name = "colNotes";
			this.ctxMenu.Items.AddRange(new System.Windows.Forms.ToolStripItem[2] { this.miEdit, this.miDelete });
			this.ctxMenu.Name = "ctxMenu";
			this.ctxMenu.Size = new System.Drawing.Size(108, 48);
			this.miEdit.Name = "miEdit";
			this.miEdit.Size = new System.Drawing.Size(107, 22);
			this.miEdit.Text = "Edit";
			this.miDelete.Name = "miDelete";
			this.miDelete.Size = new System.Drawing.Size(107, 22);
			this.miDelete.Text = "Delete";
			this.tabPredictions.Controls.Add(this.gridPreds);
			this.tabPredictions.Location = new System.Drawing.Point(4, 22);
			this.tabPredictions.Name = "tabPredictions";
			this.tabPredictions.Padding = new System.Windows.Forms.Padding(3);
			this.tabPredictions.Size = new System.Drawing.Size(856, 675);
			this.tabPredictions.TabIndex = 1;
			this.tabPredictions.Text = "Predictions";
			this.tabPredictions.UseVisualStyleBackColor = true;
			this.gridPreds.AllowUserToAddRows = false;
			this.gridPreds.AllowUserToDeleteRows = false;
			this.gridPreds.AutoGenerateColumns = false;
			this.gridPreds.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
			this.gridPreds.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
			this.gridPreds.Columns.AddRange(this.colPredProduct, this.colPredTime, this.colPredHorizon, this.colPredDir, this.colPredProb, this.colPredExpRet, this.colPredExpVol, this.colPredKnown, this.colPredRDir, this.colPredRRet);
			this.gridPreds.Dock = System.Windows.Forms.DockStyle.Fill;
			this.gridPreds.Location = new System.Drawing.Point(3, 3);
			this.gridPreds.Name = "gridPreds";
			this.gridPreds.ReadOnly = true;
			this.gridPreds.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
			this.gridPreds.Size = new System.Drawing.Size(850, 669);
			this.gridPreds.TabIndex = 0;
			this.colPredProduct.DataPropertyName = "ProductId";
			this.colPredProduct.HeaderText = "Product";
			this.colPredProduct.Name = "colPredProduct";
			this.colPredProduct.ReadOnly = true;
			this.colPredTime.DataPropertyName = "AtUtc";
			this.colPredTime.HeaderText = "Time";
			this.colPredTime.Name = "colPredTime";
			this.colPredTime.ReadOnly = true;
			this.colPredHorizon.DataPropertyName = "HorizonMinutes";
			this.colPredHorizon.HeaderText = "Horizon";
			this.colPredHorizon.Name = "colPredHorizon";
			this.colPredHorizon.ReadOnly = true;
			this.colPredDir.DataPropertyName = "Direction";
			this.colPredDir.HeaderText = "Dir";
			this.colPredDir.Name = "colPredDir";
			this.colPredDir.ReadOnly = true;
			this.colPredProb.DataPropertyName = "Probability";
			this.colPredProb.HeaderText = "Prob";
			this.colPredProb.Name = "colPredProb";
			this.colPredProb.ReadOnly = true;
			this.colPredExpRet.DataPropertyName = "ExpectedReturn";
			this.colPredExpRet.HeaderText = "ExpRet";
			this.colPredExpRet.Name = "colPredExpRet";
			this.colPredExpRet.ReadOnly = true;
			this.colPredExpVol.DataPropertyName = "ExpectedVol";
			this.colPredExpVol.HeaderText = "ExpVol";
			this.colPredExpVol.Name = "colPredExpVol";
			this.colPredExpVol.ReadOnly = true;
			this.colPredKnown.DataPropertyName = "RealizedKnown";
			this.colPredKnown.HeaderText = "Known";
			this.colPredKnown.Name = "colPredKnown";
			this.colPredKnown.ReadOnly = true;
			this.colPredRDir.DataPropertyName = "RealizedDirection";
			this.colPredRDir.HeaderText = "RDir";
			this.colPredRDir.Name = "colPredRDir";
			this.colPredRDir.ReadOnly = true;
			this.colPredRRet.DataPropertyName = "RealizedReturn";
			this.colPredRRet.HeaderText = "RRet";
			this.colPredRRet.Name = "colPredRRet";
			this.colPredRRet.ReadOnly = true;
			base.AutoScaleDimensions = new System.Drawing.SizeF(6f, 13f);
			base.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			base.Controls.Add(this.mainLayout);
			base.Name = "PlannerControl";
			base.Size = new System.Drawing.Size(1200, 800);
			this.mainLayout.ResumeLayout(false);
			this.mainLayout.PerformLayout();
			this.headerBar.ResumeLayout(false);
			this.headerBar.PerformLayout();
			((System.ComponentModel.ISupportInitialize)this.numLookback).EndInit();
			((System.ComponentModel.ISupportInitialize)this.numEquity).EndInit();
			this.mainSplit.Panel1.ResumeLayout(false);
			this.mainSplit.Panel2.ResumeLayout(false);
			((System.ComponentModel.ISupportInitialize)this.mainSplit).EndInit();
			this.mainSplit.ResumeLayout(false);
			this.leftPanel.ResumeLayout(false);
			this.topBar.ResumeLayout(false);
			this.topBar.PerformLayout();
			this.rightLayout.ResumeLayout(false);
			this.rightLayout.PerformLayout();
			this.actionBar.ResumeLayout(false);
			this.tabMain.ResumeLayout(false);
			this.tabPlanned.ResumeLayout(false);
			((System.ComponentModel.ISupportInitialize)this.gridPlanned).EndInit();
			this.ctxMenu.ResumeLayout(false);
			this.tabPredictions.ResumeLayout(false);
			((System.ComponentModel.ISupportInitialize)this.gridPreds).EndInit();
			base.ResumeLayout(false);
		}
	}
}
