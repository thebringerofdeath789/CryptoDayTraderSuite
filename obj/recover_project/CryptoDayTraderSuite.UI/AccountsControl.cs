using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using CryptoDayTraderSuite.Models;
using CryptoDayTraderSuite.Services;
using CryptoDayTraderSuite.Util;

namespace CryptoDayTraderSuite.UI
{
	public class AccountsControl : UserControl
	{
		private BindingSource _bs = new BindingSource();

		private IAccountService _service;

		private IKeyService _keyService;

		private IContainer components = null;

		private TableLayoutPanel mainLayout;

		private FlowLayoutPanel topPanel;

		public Button btnAdd;

		public Button btnEdit;

		public Button btnDelete;

		public Button btnSave;

		public Button btnRefresh;

		public DataGridView gridAccounts;

		private DataGridViewTextBoxColumn colLabel;

		private DataGridViewTextBoxColumn colService;

		private DataGridViewTextBoxColumn colMode;

		private DataGridViewTextBoxColumn colRisk;

		private DataGridViewTextBoxColumn colMaxOpen;

		private DataGridViewTextBoxColumn colKeyId;

		private DataGridViewCheckBoxColumn colEnabled;

		public AccountsControl()
		{
			InitializeComponent();
			ConfigureGrid();
			WireEvents();
		}

		public void Initialize(IAccountService service, IKeyService keyService = null)
		{
			_service = service;
			_keyService = keyService;
			LoadData();
		}

		private void ConfigureGrid()
		{
			gridAccounts.AutoGenerateColumns = false;
			colLabel.DataPropertyName = "Label";
			colService.DataPropertyName = "Service";
			colMode.DataPropertyName = "Mode";
			colRisk.DataPropertyName = "RiskPerTradePct";
			colMaxOpen.DataPropertyName = "MaxConcurrentTrades";
			colKeyId.DataPropertyName = "KeyEntryId";
			colEnabled.DataPropertyName = "Enabled";
			gridAccounts.ReadOnly = false;
			foreach (DataGridViewColumn col in gridAccounts.Columns)
			{
				col.ReadOnly = true;
			}
			colEnabled.ReadOnly = false;
			gridAccounts.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
			colLabel.FillWeight = 180f;
			colService.FillWeight = 95f;
			colMode.FillWeight = 75f;
			colRisk.FillWeight = 70f;
			colMaxOpen.FillWeight = 85f;
			colKeyId.FillWeight = 200f;
			colEnabled.FillWeight = 60f;
			gridAccounts.DefaultCellStyle.WrapMode = DataGridViewTriState.False;
			gridAccounts.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.None;
			gridAccounts.RowTemplate.Height = 26;
		}

		private void WireEvents()
		{
			btnAdd.Click += delegate
			{
				AddAccount();
			};
			btnEdit.Click += delegate
			{
				EditSelected();
			};
			btnDelete.Click += delegate
			{
				DeleteSelected();
			};
			btnRefresh.Click += delegate
			{
				LoadData();
			};
			btnSave.Click += delegate
			{
				SaveChanges();
			};
			gridAccounts.DoubleClick += delegate
			{
				EditSelected();
			};
			gridAccounts.CurrentCellDirtyStateChanged += GridAccounts_CurrentCellDirtyStateChanged;
		}

		private void GridAccounts_CurrentCellDirtyStateChanged(object sender, EventArgs e)
		{
			if (gridAccounts.IsCurrentCellDirty)
			{
				gridAccounts.CommitEdit(DataGridViewDataErrorContexts.Commit);
			}
		}

		public void LoadData()
		{
			if (_service != null)
			{
				List<AccountInfo> accountInfos = _service.GetAll();
				List<AccountProfile> profiles = ((IEnumerable<AccountInfo>)accountInfos).Select((Func<AccountInfo, AccountProfile>)((AccountInfo info) => info)).ToList();
				_bs.DataSource = new SortableBindingList<AccountProfile>(profiles);
				gridAccounts.DataSource = _bs;
			}
		}

		private AccountProfile Selected()
		{
			return (gridAccounts.CurrentRow != null) ? (gridAccounts.CurrentRow.DataBoundItem as AccountProfile) : null;
		}

		private void AddAccount()
		{
			if (_service != null)
			{
				AccountEditDialog dlg = new AccountEditDialog(null, _service, _keyService);
				if (dlg.ShowDialog(this) == DialogResult.OK)
				{
					LoadData();
				}
			}
		}

		private void EditSelected()
		{
			if (_service == null)
			{
				return;
			}
			AccountProfile cur = Selected();
			if (cur != null)
			{
				AccountEditDialog dlg = new AccountEditDialog(cur.Id, _service, _keyService);
				if (dlg.ShowDialog(this) == DialogResult.OK)
				{
					LoadData();
				}
			}
		}

		private void DeleteSelected()
		{
			if (_service != null)
			{
				AccountProfile cur = Selected();
				if (cur != null && MessageBox.Show("Delete account " + cur.Label + "?", "Confirm", MessageBoxButtons.YesNo) == DialogResult.Yes)
				{
					_service.Delete(cur.Id);
					LoadData();
				}
			}
		}

		private void SaveChanges()
		{
			if (_service != null && _bs.DataSource is SortableBindingList<AccountProfile> data)
			{
				List<AccountInfo> accountInfos = ((IEnumerable<AccountProfile>)data).Select((Func<AccountProfile, AccountInfo>)((AccountProfile profile) => profile)).ToList();
				_service.ReplaceAll(accountInfos);
				MessageBox.Show("Accounts saved.", "Save", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
			}
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
			this.topPanel = new System.Windows.Forms.FlowLayoutPanel();
			this.btnAdd = new System.Windows.Forms.Button();
			this.btnEdit = new System.Windows.Forms.Button();
			this.btnDelete = new System.Windows.Forms.Button();
			this.btnSave = new System.Windows.Forms.Button();
			this.btnRefresh = new System.Windows.Forms.Button();
			this.gridAccounts = new System.Windows.Forms.DataGridView();
			this.colLabel = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.colService = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.colMode = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.colRisk = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.colMaxOpen = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.colKeyId = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.colEnabled = new System.Windows.Forms.DataGridViewCheckBoxColumn();
			this.mainLayout.SuspendLayout();
			this.topPanel.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)this.gridAccounts).BeginInit();
			base.SuspendLayout();
			this.mainLayout.ColumnCount = 1;
			this.mainLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100f));
			this.mainLayout.Controls.Add(this.topPanel, 0, 0);
			this.mainLayout.Controls.Add(this.gridAccounts, 0, 1);
			this.mainLayout.Dock = System.Windows.Forms.DockStyle.Fill;
			this.mainLayout.Location = new System.Drawing.Point(0, 0);
			this.mainLayout.Name = "mainLayout";
			this.mainLayout.RowCount = 2;
			this.mainLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 40f));
			this.mainLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100f));
			this.mainLayout.TabIndex = 0;
			this.topPanel.Controls.Add(this.btnAdd);
			this.topPanel.Controls.Add(this.btnEdit);
			this.topPanel.Controls.Add(this.btnDelete);
			this.topPanel.Controls.Add(this.btnSave);
			this.topPanel.Controls.Add(this.btnRefresh);
			this.topPanel.Dock = System.Windows.Forms.DockStyle.Fill;
			this.topPanel.Location = new System.Drawing.Point(3, 3);
			this.topPanel.Name = "topPanel";
			this.topPanel.Size = new System.Drawing.Size(634, 34);
			this.topPanel.TabIndex = 0;
			this.btnAdd.Location = new System.Drawing.Point(3, 3);
			this.btnAdd.Name = "btnAdd";
			this.btnAdd.Size = new System.Drawing.Size(75, 23);
			this.btnAdd.TabIndex = 0;
			this.btnAdd.Text = "Add";
			this.btnAdd.UseVisualStyleBackColor = true;
			this.btnEdit.Location = new System.Drawing.Point(84, 3);
			this.btnEdit.Name = "btnEdit";
			this.btnEdit.Size = new System.Drawing.Size(75, 23);
			this.btnEdit.TabIndex = 1;
			this.btnEdit.Text = "Edit";
			this.btnEdit.UseVisualStyleBackColor = true;
			this.btnDelete.Location = new System.Drawing.Point(165, 3);
			this.btnDelete.Name = "btnDelete";
			this.btnDelete.Size = new System.Drawing.Size(75, 23);
			this.btnDelete.TabIndex = 2;
			this.btnDelete.Text = "Delete";
			this.btnDelete.UseVisualStyleBackColor = true;
			this.btnSave.Location = new System.Drawing.Point(246, 3);
			this.btnSave.Name = "btnSave";
			this.btnSave.Size = new System.Drawing.Size(75, 23);
			this.btnSave.TabIndex = 3;
			this.btnSave.Text = "Save";
			this.btnSave.UseVisualStyleBackColor = true;
			this.btnRefresh.Location = new System.Drawing.Point(327, 3);
			this.btnRefresh.Name = "btnRefresh";
			this.btnRefresh.Size = new System.Drawing.Size(75, 23);
			this.btnRefresh.TabIndex = 4;
			this.btnRefresh.Text = "Refresh";
			this.btnRefresh.UseVisualStyleBackColor = true;
			this.gridAccounts.AllowUserToAddRows = false;
			this.gridAccounts.AllowUserToDeleteRows = false;
			this.gridAccounts.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
			this.gridAccounts.Columns.AddRange(this.colLabel, this.colService, this.colMode, this.colRisk, this.colMaxOpen, this.colKeyId, this.colEnabled);
			this.gridAccounts.Dock = System.Windows.Forms.DockStyle.Fill;
			this.gridAccounts.Location = new System.Drawing.Point(3, 43);
			this.gridAccounts.Name = "gridAccounts";
			this.gridAccounts.ReadOnly = true;
			this.gridAccounts.RowHeadersVisible = false;
			this.gridAccounts.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
			this.gridAccounts.Size = new System.Drawing.Size(634, 434);
			this.gridAccounts.TabIndex = 1;
			this.colLabel.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
			this.colLabel.HeaderText = "Label";
			this.colLabel.Name = "colLabel";
			this.colLabel.ReadOnly = true;
			this.colService.HeaderText = "Service";
			this.colService.Name = "colService";
			this.colService.ReadOnly = true;
			this.colMode.HeaderText = "Mode";
			this.colMode.Name = "colMode";
			this.colMode.ReadOnly = true;
			this.colRisk.HeaderText = "Risk %";
			this.colRisk.Name = "colRisk";
			this.colRisk.ReadOnly = true;
			this.colMaxOpen.HeaderText = "Max Open";
			this.colMaxOpen.Name = "colMaxOpen";
			this.colMaxOpen.ReadOnly = true;
			this.colKeyId.HeaderText = "Key Id";
			this.colKeyId.Name = "colKeyId";
			this.colKeyId.ReadOnly = true;
			this.colEnabled.HeaderText = "Enabled";
			this.colEnabled.Name = "colEnabled";
			this.colEnabled.ReadOnly = true;
			base.AutoScaleDimensions = new System.Drawing.SizeF(6f, 13f);
			base.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			base.Controls.Add(this.mainLayout);
			base.Name = "AccountsControl";
			base.Size = new System.Drawing.Size(640, 480);
			this.mainLayout.ResumeLayout(false);
			this.topPanel.ResumeLayout(false);
			((System.ComponentModel.ISupportInitialize)this.gridAccounts).EndInit();
			base.ResumeLayout(false);
		}
	}
}
