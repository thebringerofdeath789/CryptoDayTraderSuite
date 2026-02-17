using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using CryptoDayTraderSuite.Brokers;
using CryptoDayTraderSuite.Exchanges;
using CryptoDayTraderSuite.Models;
using CryptoDayTraderSuite.Services;

namespace CryptoDayTraderSuite.UI
{
	public class AutoModeForm : Form
	{
		protected ComboBox cmbProduct;

		protected ComboBox cmbGran;

		protected ComboBox cmbAccount;

		protected NumericUpDown numLookback;

		protected NumericUpDown numEquity;

		protected DataGridView grid;

		protected Button btnScan;

		protected Button btnPropose;

		protected Button btnExecute;

		private List<ProjectionRow> _last;

		private List<AccountInfo> _accounts;

		private List<TradePlan> _queued;

		private readonly AutoPlannerService _planner;

		private readonly IExchangeClient _client;

		private readonly IKeyService _keyService;

		private readonly IAccountService _accountService;

		public AutoModeForm(AutoPlannerService planner, IExchangeClient client, IKeyService keyService, IAccountService accountService)
		{
			_planner = planner ?? throw new ArgumentNullException("planner");
			_client = client;
			_keyService = keyService ?? throw new ArgumentNullException("keyService");
			_accountService = accountService ?? throw new ArgumentNullException("accountService");
			InitializeComponent();
			BuildUi();
			LoadAccounts();
			LoadProducts();
		}

		private void BuildUi()
		{
			Text = "Automatic Mode";
			base.StartPosition = FormStartPosition.CenterParent;
			base.Width = 980;
			base.Height = 640;
			TableLayoutPanel tl = new TableLayoutPanel
			{
				Dock = DockStyle.Fill,
				ColumnCount = 1,
				RowCount = 2
			};
			tl.RowStyles.Add(new RowStyle(SizeType.AutoSize));
			tl.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
			base.Controls.Add(tl);
			FlowLayoutPanel top = new FlowLayoutPanel
			{
				Dock = DockStyle.Top,
				AutoSize = true,
				Padding = new Padding(8)
			};
			tl.Controls.Add(top, 0, 0);
			top.Controls.Add(new Label
			{
				Text = "Account",
				AutoSize = true,
				Padding = new Padding(0, 6, 8, 0)
			});
			cmbAccount = new ComboBox
			{
				DropDownStyle = ComboBoxStyle.DropDownList,
				Width = 200
			};
			top.Controls.Add(cmbAccount);
			top.Controls.Add(new Label
			{
				Text = "Product",
				AutoSize = true,
				Padding = new Padding(12, 6, 8, 0)
			});
			cmbProduct = new ComboBox
			{
				DropDownStyle = ComboBoxStyle.DropDownList,
				Width = 180
			};
			top.Controls.Add(cmbProduct);
			top.Controls.Add(new Label
			{
				Text = "Gran (min)",
				AutoSize = true,
				Padding = new Padding(12, 6, 8, 0)
			});
			cmbGran = new ComboBox
			{
				DropDownStyle = ComboBoxStyle.DropDownList
			};
			cmbGran.Items.AddRange(new object[6] { "1", "5", "15", "30", "60", "240" });
			cmbGran.SelectedIndex = 2;
			top.Controls.Add(cmbGran);
			top.Controls.Add(new Label
			{
				Text = "Lookback (days)",
				AutoSize = true,
				Padding = new Padding(12, 6, 8, 0)
			});
			numLookback = new NumericUpDown
			{
				Minimum = 5m,
				Maximum = 120m,
				Value = 30m
			};
			top.Controls.Add(numLookback);
			top.Controls.Add(new Label
			{
				Text = "Equity ($)",
				AutoSize = true,
				Padding = new Padding(12, 6, 8, 0)
			});
			numEquity = new NumericUpDown
			{
				Minimum = 10m,
				Maximum = 1000000m,
				Value = 1000m
			};
			top.Controls.Add(numEquity);
			btnScan = new Button
			{
				Text = "Scan"
			};
			btnScan.Click += delegate
			{
				DoScan();
			};
			top.Controls.Add(btnScan);
			btnPropose = new Button
			{
				Text = "Propose"
			};
			btnPropose.Click += delegate
			{
				DoPropose();
			};
			top.Controls.Add(btnPropose);
			btnExecute = new Button
			{
				Text = "Execute"
			};
			btnExecute.Click += delegate
			{
				DoExecute();
			};
			top.Controls.Add(btnExecute);
			grid = new DataGridView
			{
				Dock = DockStyle.Fill,
				ReadOnly = true,
				AutoGenerateColumns = false,
				AllowUserToAddRows = false,
				SelectionMode = DataGridViewSelectionMode.FullRowSelect
			};
			grid.Columns.Add(new DataGridViewTextBoxColumn
			{
				DataPropertyName = "Strategy",
				HeaderText = "Strategy",
				AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
				FillWeight = 200f
			});
			grid.Columns.Add(new DataGridViewTextBoxColumn
			{
				DataPropertyName = "Symbol",
				HeaderText = "Symbol",
				Width = 120
			});
			grid.Columns.Add(new DataGridViewTextBoxColumn
			{
				DataPropertyName = "GranMinutes",
				HeaderText = "Granularity",
				Width = 80
			});
			grid.Columns.Add(new DataGridViewTextBoxColumn
			{
				DataPropertyName = "Expectancy",
				HeaderText = "Expectancy",
				Width = 110
			});
			grid.Columns.Add(new DataGridViewTextBoxColumn
			{
				DataPropertyName = "WinRate",
				HeaderText = "Win%",
				Width = 80
			});
			grid.Columns.Add(new DataGridViewTextBoxColumn
			{
				DataPropertyName = "AvgWin",
				HeaderText = "AvgWin",
				Width = 90
			});
			grid.Columns.Add(new DataGridViewTextBoxColumn
			{
				DataPropertyName = "AvgLoss",
				HeaderText = "AvgLoss",
				Width = 90
			});
			grid.Columns.Add(new DataGridViewTextBoxColumn
			{
				DataPropertyName = "SharpeApprox",
				HeaderText = "Sharpe",
				Width = 80
			});
			grid.Columns.Add(new DataGridViewTextBoxColumn
			{
				DataPropertyName = "Samples",
				HeaderText = "Samples",
				Width = 80
			});
			tl.Controls.Add(grid, 0, 1);
		}

		private void LoadAccounts()
		{
			_accounts = (from accountInfo in _accountService.GetAll()
				where accountInfo.Enabled
				select accountInfo).ToList();
			cmbAccount.Items.Clear();
			foreach (AccountInfo a in _accounts)
			{
				cmbAccount.Items.Add(a.Label + " [" + a.Service + "]");
			}
			if (cmbAccount.Items.Count > 0)
			{
				cmbAccount.SelectedIndex = 0;
			}
		}

		private async void LoadProducts()
		{
			try
			{
				List<string> prods = ((_client == null) ? (await new CoinbasePublicClient().GetProductsAsync()) : (await _client.ListProductsAsync()));
				cmbProduct.Items.Clear();
				foreach (string p in prods)
				{
					cmbProduct.Items.Add(p);
				}
				if (cmbProduct.Items.Count > 0)
				{
					cmbProduct.SelectedIndex = 0;
				}
			}
			catch (Exception ex)
			{
				Exception ex2 = ex;
				if (!base.IsDisposed)
				{
					MessageBox.Show("Failed to load products: " + ex2.Message, "Auto Mode", MessageBoxButtons.OK, MessageBoxIcon.Hand);
				}
			}
		}

		private async void DoScan()
		{
			if (cmbProduct.SelectedItem == null)
			{
				MessageBox.Show("Select a product.", "Auto Mode", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
				return;
			}
			string symbol = cmbProduct.SelectedItem.ToString();
			int gran = int.Parse(cmbGran.SelectedItem.ToString());
			btnScan.Enabled = false;
			try
			{
				if (_planner != null)
				{
					int lookbackMins = (int)numLookback.Value * 1440;
					List<ProjectionRow> rows = (_last = await _planner.ProjectAsync(symbol, gran, lookbackMins, 0.006m, 0.004m));
					grid.DataSource = rows;
					return;
				}
				throw new InvalidOperationException("AutoPlannerService not initialized. Please restart via Program.cs composition root.");
			}
			catch (Exception ex)
			{
				Exception ex2 = ex;
				MessageBox.Show("Scan error: " + ex2.Message, "Auto Mode", MessageBoxButtons.OK, MessageBoxIcon.Hand);
			}
			finally
			{
				btnScan.Enabled = true;
			}
		}

		private async void DoPropose()
		{
			if (_last == null || _last.Count == 0)
			{
				MessageBox.Show("Scan first.", "Auto Mode", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
				return;
			}
			if (cmbAccount.SelectedIndex < 0)
			{
				MessageBox.Show("Select account.", "Auto Mode", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
				return;
			}
			AccountInfo acc = _accounts[cmbAccount.SelectedIndex];
			string symbol = cmbProduct.SelectedItem.ToString();
			int gran = int.Parse(cmbGran.SelectedItem.ToString());
			btnPropose.Enabled = false;
			try
			{
				if (_planner != null)
				{
					List<TradePlan> plans = await _planner.ProposeAsync(acc.Id, symbol, gran, numEquity.Value, acc.RiskPerTradePct, _last);
					if (plans.Count == 0)
					{
						MessageBox.Show("No trades proposed.", "Auto Mode", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
						return;
					}
					string msg = string.Join(Environment.NewLine, plans.Select((TradePlan p) => string.Format("{0} {1} dir={2} qty={3} entry={4} stop={5} target={6} Note={7}", p.Strategy, p.Symbol, (p.Direction > 0) ? "LONG" : "SHORT", p.Qty, p.Entry, p.Stop, p.Target, p.Note)));
					MessageBox.Show("Proposed:\n" + msg, "Auto Mode", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
					_queued = plans;
					return;
				}
				throw new InvalidOperationException("AutoPlannerService not initialized.");
			}
			catch (Exception ex)
			{
				Exception ex2 = ex;
				MessageBox.Show("Propose error: " + ex2.Message, "Auto Mode", MessageBoxButtons.OK, MessageBoxIcon.Hand);
			}
			finally
			{
				btnPropose.Enabled = true;
			}
		}

		private async void DoExecute()
		{
			if (_queued == null || _queued.Count == 0)
			{
				MessageBox.Show("Nothing to execute.", "Auto Mode", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
				return;
			}
			if (cmbAccount.SelectedIndex < 0)
			{
				MessageBox.Show("Select account.", "Auto Mode", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
				return;
			}
			AccountInfo acc = _accounts[cmbAccount.SelectedIndex];
			if (acc.Mode != AccountMode.Paper)
			{
				string keyId = acc.KeyEntryId;
				if (string.IsNullOrEmpty(keyId) || _keyService.Get(keyId) == null)
				{
					MessageBox.Show("No valid API key for selected account.", "Auto Mode", MessageBoxButtons.OK, MessageBoxIcon.Hand);
					return;
				}
			}
			IBroker broker = BrokerFactory.GetBroker(acc.Service, acc.Mode, _keyService, _accountService);
			if (broker == null)
			{
				MessageBox.Show("Unsupported broker: " + acc.Service, "Auto Mode", MessageBoxButtons.OK, MessageBoxIcon.Hand);
				return;
			}
			btnExecute.Enabled = false;
			try
			{
				foreach (TradePlan p in _queued)
				{
					if (!(p.AccountId != acc.Id))
					{
						(bool ok, string message) r = await broker.PlaceOrderAsync(p);
						MessageBox.Show((r.ok ? "ok: " : "err: ") + r.message, "Auto Mode", MessageBoxButtons.OK, r.ok ? MessageBoxIcon.Asterisk : MessageBoxIcon.Hand);
					}
				}
			}
			catch (Exception ex)
			{
				Exception ex2 = ex;
				MessageBox.Show("Execution error: " + ex2.Message, "Auto Mode", MessageBoxButtons.OK, MessageBoxIcon.Hand);
			}
			finally
			{
				btnExecute.Enabled = true;
			}
		}

		private void InitializeComponent()
		{
			System.Windows.Forms.TableLayoutPanel root = new System.Windows.Forms.TableLayoutPanel();
			root.Dock = System.Windows.Forms.DockStyle.Fill;
			root.ColumnCount = 1;
			root.RowCount = 3;
			root.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
			root.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100f));
			root.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
			base.Controls.Add(root);
			System.Windows.Forms.FlowLayoutPanel top = new System.Windows.Forms.FlowLayoutPanel();
			top.Dock = System.Windows.Forms.DockStyle.Fill;
			top.AutoSize = true;
			top.Padding = new System.Windows.Forms.Padding(8);
			top.Controls.Add(new System.Windows.Forms.Label
			{
				Text = "Product",
				AutoSize = true,
				Padding = new System.Windows.Forms.Padding(0, 6, 8, 0)
			});
			this.cmbProduct = this.cmbProduct ?? new System.Windows.Forms.ComboBox();
			this.cmbProduct.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.cmbProduct.Width = 200;
			top.Controls.Add(this.cmbProduct);
			top.Controls.Add(new System.Windows.Forms.Label
			{
				Text = "Gran (min)",
				AutoSize = true,
				Padding = new System.Windows.Forms.Padding(12, 6, 8, 0)
			});
			this.cmbGran = this.cmbGran ?? new System.Windows.Forms.ComboBox();
			this.cmbGran.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.cmbGran.Items.AddRange(new object[6] { "1", "5", "15", "30", "60", "240" });
			this.cmbGran.SelectedIndex = 2;
			this.cmbGran.Width = 80;
			top.Controls.Add(this.cmbGran);
			top.Controls.Add(new System.Windows.Forms.Label
			{
				Text = "Lookback (days)",
				AutoSize = true,
				Padding = new System.Windows.Forms.Padding(12, 6, 8, 0)
			});
			this.numLookback = this.numLookback ?? new System.Windows.Forms.NumericUpDown();
			this.numLookback.Minimum = 7m;
			this.numLookback.Maximum = 365m;
			this.numLookback.Value = 30m;
			this.numLookback.Width = 90;
			top.Controls.Add(this.numLookback);
			top.Controls.Add(new System.Windows.Forms.Label
			{
				Text = "Account",
				AutoSize = true,
				Padding = new System.Windows.Forms.Padding(12, 6, 8, 0)
			});
			this.cmbAccount = this.cmbAccount ?? new System.Windows.Forms.ComboBox();
			this.cmbAccount.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.cmbAccount.Width = 200;
			top.Controls.Add(this.cmbAccount);
			top.Controls.Add(new System.Windows.Forms.Label
			{
				Text = "Equity ($)",
				AutoSize = true,
				Padding = new System.Windows.Forms.Padding(12, 6, 8, 0)
			});
			this.numEquity = this.numEquity ?? new System.Windows.Forms.NumericUpDown();
			this.numEquity.DecimalPlaces = 2;
			this.numEquity.Minimum = 10m;
			this.numEquity.Maximum = 10000000m;
			this.numEquity.Value = 1000m;
			this.numEquity.Width = 110;
			top.Controls.Add(this.numEquity);
			this.btnScan = this.btnScan ?? new System.Windows.Forms.Button();
			this.btnScan.Text = "Scan";
			this.btnScan.Width = 90;
			this.btnScan.Height = 28;
			this.btnScan.Margin = new System.Windows.Forms.Padding(24, 4, 4, 4);
			this.btnPropose = this.btnPropose ?? new System.Windows.Forms.Button();
			this.btnPropose.Text = "Propose";
			this.btnPropose.Width = 90;
			this.btnPropose.Height = 28;
			this.btnPropose.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
			this.btnExecute = this.btnExecute ?? new System.Windows.Forms.Button();
			this.btnExecute.Text = "Execute";
			this.btnExecute.Width = 90;
			this.btnExecute.Height = 28;
			this.btnExecute.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
			top.Controls.Add(this.btnScan);
			top.Controls.Add(this.btnPropose);
			top.Controls.Add(this.btnExecute);
			root.Controls.Add(top, 0, 0);
			this.grid = this.grid ?? new System.Windows.Forms.DataGridView();
			this.grid.Dock = System.Windows.Forms.DockStyle.Fill;
			this.grid.ReadOnly = true;
			this.grid.AutoGenerateColumns = true;
			this.grid.AllowUserToAddRows = false;
			this.grid.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
			root.Controls.Add(this.grid, 0, 1);
			System.Windows.Forms.FlowLayoutPanel bottom = new System.Windows.Forms.FlowLayoutPanel();
			bottom.Dock = System.Windows.Forms.DockStyle.Fill;
			bottom.AutoSize = true;
			bottom.Padding = new System.Windows.Forms.Padding(8);
			root.Controls.Add(bottom, 0, 2);
			this.Text = "Auto Mode";
			base.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
			base.Width = 1100;
			base.Height = 700;
			try
			{
				System.Reflection.MethodInfo mi = base.GetType().GetMethod("AfterBuildUi", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
				if (mi != null)
				{
					mi.Invoke(this, null);
				}
			}
			catch
			{
			}
		}
	}
}
