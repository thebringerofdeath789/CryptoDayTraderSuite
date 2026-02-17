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
	public class KeysControl : UserControl
	{
		private IKeyService _service;

		private BindingSource _bs = new BindingSource();

		private IContainer components = null;

		private TableLayoutPanel tableLayoutPanel1;

		private FlowLayoutPanel flowLayoutPanel1;

		private Button btnAdd;

		private Button btnEdit;

		private Button btnDelete;

		private Button btnSave;

		private Button btnRefresh;

		private DataGridView gridKeys;

		private DataGridViewTextBoxColumn colLabel;

		private DataGridViewTextBoxColumn colService;

		private DataGridViewTextBoxColumn colKeyId;

		private DataGridViewCheckBoxColumn colActive;

		private DataGridViewCheckBoxColumn colEnabled;

		public KeysControl()
		{
			InitializeComponent();
			gridKeys.AutoGenerateColumns = false;
			Dock = DockStyle.Fill;
		}

		public void Initialize(IKeyService service)
		{
			_service = service;
			LoadData();
		}

		protected override void OnLoad(EventArgs e)
		{
			base.OnLoad(e);
			if (!base.DesignMode)
			{
				LoadData();
			}
		}

		private void LoadData()
		{
			if (_service != null && !base.DesignMode)
			{
				List<KeyInfo> infos = _service.GetAll();
				List<KeyEntry> entries = infos.Select(delegate(KeyInfo k)
				{
					KeyEntry keyEntry = k;
					keyEntry.Active = k.Active;
					return keyEntry;
				}).ToList();
				_bs.DataSource = new SortableBindingList<KeyEntry>(entries);
				gridKeys.DataSource = _bs;
			}
		}

		private void btnAdd_Click(object sender, EventArgs e)
		{
			if (_service != null)
			{
				KeyEditDialog dlg = new KeyEditDialog(null, _service);
				if (dlg.ShowDialog(this) == DialogResult.OK)
				{
					LoadData();
				}
			}
		}

		private void btnEdit_Click(object sender, EventArgs e)
		{
			EditSelected();
		}

		private void btnDelete_Click(object sender, EventArgs e)
		{
			if (_service != null)
			{
				KeyEntry cur = Selected();
				if (cur != null && MessageBox.Show("Delete key " + cur.Label + "?", "Confirm", MessageBoxButtons.YesNo) == DialogResult.Yes)
				{
					_service.Delete(cur.Id);
					LoadData();
				}
			}
		}

		private void btnSave_Click(object sender, EventArgs e)
		{
			if (_service == null)
			{
				return;
			}
			gridKeys.EndEdit();
			if (!(_bs.DataSource is SortableBindingList<KeyEntry> data))
			{
				return;
			}
			foreach (KeyEntry item in data)
			{
				if (item.Active)
				{
					_service.SetActive(item.Id);
				}
			}
			List<KeyInfo> infos = data.Select(delegate(KeyEntry entry)
			{
				KeyInfo keyInfo = (KeyInfo)entry;
				keyInfo.Active = entry.Active;
				return keyInfo;
			}).ToList();
			_service.ReplaceAll(infos);
			MessageBox.Show("API keys saved.", "Save", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
			LoadData();
		}

		private void btnRefresh_Click(object sender, EventArgs e)
		{
			LoadData();
		}

		private void gridKeys_DoubleClick(object sender, EventArgs e)
		{
			EditSelected();
		}

		private KeyEntry Selected()
		{
			return gridKeys.CurrentRow?.DataBoundItem as KeyEntry;
		}

		private void EditSelected()
		{
			if (_service == null)
			{
				return;
			}
			KeyEntry cur = Selected();
			if (cur != null)
			{
				KeyEditDialog dlg = new KeyEditDialog(cur.Id, _service);
				if (dlg.ShowDialog(this) == DialogResult.OK)
				{
					LoadData();
				}
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
			this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
			this.flowLayoutPanel1 = new System.Windows.Forms.FlowLayoutPanel();
			this.btnAdd = new System.Windows.Forms.Button();
			this.btnEdit = new System.Windows.Forms.Button();
			this.btnDelete = new System.Windows.Forms.Button();
			this.btnSave = new System.Windows.Forms.Button();
			this.btnRefresh = new System.Windows.Forms.Button();
			this.gridKeys = new System.Windows.Forms.DataGridView();
			this.colLabel = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.colService = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.colKeyId = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.colActive = new System.Windows.Forms.DataGridViewCheckBoxColumn();
			this.colEnabled = new System.Windows.Forms.DataGridViewCheckBoxColumn();
			this.tableLayoutPanel1.SuspendLayout();
			this.flowLayoutPanel1.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)this.gridKeys).BeginInit();
			base.SuspendLayout();
			this.tableLayoutPanel1.ColumnCount = 1;
			this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100f));
			this.tableLayoutPanel1.Controls.Add(this.flowLayoutPanel1, 0, 0);
			this.tableLayoutPanel1.Controls.Add(this.gridKeys, 0, 1);
			this.tableLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Fill;
			this.tableLayoutPanel1.Location = new System.Drawing.Point(0, 0);
			this.tableLayoutPanel1.Name = "tableLayoutPanel1";
			this.tableLayoutPanel1.RowCount = 2;
			this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
			this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100f));
			this.tableLayoutPanel1.Size = new System.Drawing.Size(800, 500);
			this.tableLayoutPanel1.TabIndex = 0;
			this.flowLayoutPanel1.AutoSize = true;
			this.flowLayoutPanel1.Controls.Add(this.btnAdd);
			this.flowLayoutPanel1.Controls.Add(this.btnEdit);
			this.flowLayoutPanel1.Controls.Add(this.btnDelete);
			this.flowLayoutPanel1.Controls.Add(this.btnSave);
			this.flowLayoutPanel1.Controls.Add(this.btnRefresh);
			this.flowLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Fill;
			this.flowLayoutPanel1.Location = new System.Drawing.Point(3, 3);
			this.flowLayoutPanel1.Name = "flowLayoutPanel1";
			this.flowLayoutPanel1.Padding = new System.Windows.Forms.Padding(4);
			this.flowLayoutPanel1.Size = new System.Drawing.Size(794, 37);
			this.flowLayoutPanel1.TabIndex = 0;
			this.btnAdd.Location = new System.Drawing.Point(7, 7);
			this.btnAdd.Name = "btnAdd";
			this.btnAdd.Size = new System.Drawing.Size(75, 23);
			this.btnAdd.TabIndex = 0;
			this.btnAdd.Text = "Add";
			this.btnAdd.UseVisualStyleBackColor = true;
			this.btnAdd.Click += new System.EventHandler(btnAdd_Click);
			this.btnEdit.Location = new System.Drawing.Point(88, 7);
			this.btnEdit.Name = "btnEdit";
			this.btnEdit.Size = new System.Drawing.Size(75, 23);
			this.btnEdit.TabIndex = 1;
			this.btnEdit.Text = "Edit";
			this.btnEdit.UseVisualStyleBackColor = true;
			this.btnEdit.Click += new System.EventHandler(btnEdit_Click);
			this.btnDelete.Location = new System.Drawing.Point(169, 7);
			this.btnDelete.Name = "btnDelete";
			this.btnDelete.Size = new System.Drawing.Size(75, 23);
			this.btnDelete.TabIndex = 2;
			this.btnDelete.Text = "Delete";
			this.btnDelete.UseVisualStyleBackColor = true;
			this.btnDelete.Click += new System.EventHandler(btnDelete_Click);
			this.btnSave.Location = new System.Drawing.Point(250, 7);
			this.btnSave.Name = "btnSave";
			this.btnSave.Size = new System.Drawing.Size(75, 23);
			this.btnSave.TabIndex = 3;
			this.btnSave.Text = "Save";
			this.btnSave.UseVisualStyleBackColor = true;
			this.btnSave.Click += new System.EventHandler(btnSave_Click);
			this.btnRefresh.Location = new System.Drawing.Point(331, 7);
			this.btnRefresh.Name = "btnRefresh";
			this.btnRefresh.Size = new System.Drawing.Size(75, 23);
			this.btnRefresh.TabIndex = 4;
			this.btnRefresh.Text = "Refresh";
			this.btnRefresh.UseVisualStyleBackColor = true;
			this.btnRefresh.Click += new System.EventHandler(btnRefresh_Click);
			this.gridKeys.AllowUserToAddRows = false;
			this.gridKeys.AllowUserToDeleteRows = false;
			this.gridKeys.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
			this.gridKeys.Columns.AddRange(this.colLabel, this.colService, this.colKeyId, this.colActive, this.colEnabled);
			this.gridKeys.Dock = System.Windows.Forms.DockStyle.Fill;
			this.gridKeys.Location = new System.Drawing.Point(3, 46);
			this.gridKeys.Name = "gridKeys";
			this.gridKeys.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
			this.gridKeys.Size = new System.Drawing.Size(794, 451);
			this.gridKeys.TabIndex = 1;
			this.gridKeys.DoubleClick += new System.EventHandler(gridKeys_DoubleClick);
			this.colLabel.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
			this.colLabel.DataPropertyName = "Label";
			this.colLabel.FillWeight = 180f;
			this.colLabel.HeaderText = "Label";
			this.colLabel.Name = "colLabel";
			this.colLabel.ReadOnly = true;
			this.colService.DataPropertyName = "Service";
			this.colService.HeaderText = "Service";
			this.colService.Name = "colService";
			this.colService.ReadOnly = true;
			this.colService.Width = 120;
			this.colKeyId.DataPropertyName = "Id";
			this.colKeyId.HeaderText = "Key ID";
			this.colKeyId.Name = "colKeyId";
			this.colKeyId.ReadOnly = true;
			this.colKeyId.Width = 180;
			this.colActive.DataPropertyName = "Active";
			this.colActive.HeaderText = "Active";
			this.colActive.Name = "colActive";
			this.colActive.Width = 60;
			this.colEnabled.DataPropertyName = "Enabled";
			this.colEnabled.HeaderText = "Enabled";
			this.colEnabled.Name = "colEnabled";
			this.colEnabled.Width = 60;
			base.AutoScaleDimensions = new System.Drawing.SizeF(6f, 13f);
			base.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			base.Controls.Add(this.tableLayoutPanel1);
			base.Name = "KeysControl";
			base.Size = new System.Drawing.Size(800, 500);
			this.tableLayoutPanel1.ResumeLayout(false);
			this.tableLayoutPanel1.PerformLayout();
			this.flowLayoutPanel1.ResumeLayout(false);
			((System.ComponentModel.ISupportInitialize)this.gridKeys).EndInit();
			base.ResumeLayout(false);
		}
	}
}
