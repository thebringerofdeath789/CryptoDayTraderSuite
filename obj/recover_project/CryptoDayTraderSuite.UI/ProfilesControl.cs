using System;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using CryptoDayTraderSuite.Services;
using CryptoDayTraderSuite.Themes;

namespace CryptoDayTraderSuite.UI
{
	public class ProfilesControl : UserControl
	{
		private IProfileService _service;

		private string _profilesDir;

		private IContainer components = null;

		private DataGridView gridProfiles;

		private DataGridViewTextBoxColumn colName;

		private DataGridViewTextBoxColumn colDate;

		private Panel pnlControls;

		private Button btnLoad;

		private Button btnSave;

		private Button btnSaveAs;

		private Button btnDelete;

		private Label lblPass;

		private TextBox txtPassphrase;

		public ProfilesControl()
		{
			InitializeComponent();
			Theme.Apply(this);
			gridProfiles.BackgroundColor = Theme.ContentBg;
			gridProfiles.GridColor = Theme.PanelBg;
			_profilesDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "CryptoDayTraderSuite", "Profiles");
		}

		public void Initialize(IProfileService service)
		{
			_service = service;
			if (!Directory.Exists(_profilesDir))
			{
				Directory.CreateDirectory(_profilesDir);
			}
			RefreshList();
		}

		private void RefreshList()
		{
			gridProfiles.Rows.Clear();
			if (Directory.Exists(_profilesDir))
			{
				string[] files = Directory.GetFiles(_profilesDir, "*.cdtp");
				string[] array = files;
				foreach (string f in array)
				{
					FileInfo info = new FileInfo(f);
					gridProfiles.Rows.Add(Path.GetFileNameWithoutExtension(f), info.LastWriteTime.ToString("g"));
				}
			}
		}

		private string GetSelectedPath()
		{
			if (gridProfiles.SelectedRows.Count == 0)
			{
				return null;
			}
			string name = gridProfiles.SelectedRows[0].Cells[0].Value.ToString();
			return Path.Combine(_profilesDir, name + ".cdtp");
		}

		private void btnLoad_Click(object sender, EventArgs e)
		{
			string path = GetSelectedPath();
			if (path == null)
			{
				return;
			}
			try
			{
				_service.Import(path, txtPassphrase.Text);
				MessageBox.Show("Profile loaded successfully.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
			}
			catch (Exception ex)
			{
				MessageBox.Show("Error loading profile: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Hand);
			}
		}

		private void btnSave_Click(object sender, EventArgs e)
		{
			string path = GetSelectedPath();
			if (path == null)
			{
				btnSaveAs_Click(sender, e);
			}
			else if (MessageBox.Show("Overwrite selected profile?", "Confirm", MessageBoxButtons.YesNo) == DialogResult.Yes)
			{
				try
				{
					_service.Export(path, txtPassphrase.Text);
					MessageBox.Show("Profile saved.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
					RefreshList();
				}
				catch (Exception ex)
				{
					MessageBox.Show("Error saving profile: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Hand);
				}
			}
		}

		private void btnSaveAs_Click(object sender, EventArgs e)
		{
			using (SaveFileDialog dlg = new SaveFileDialog())
			{
				dlg.InitialDirectory = _profilesDir;
				dlg.Filter = "Crypto Profile (*.cdtp)|*.cdtp";
				dlg.DefaultExt = "cdtp";
				if (dlg.ShowDialog() == DialogResult.OK)
				{
					try
					{
						_service.Export(dlg.FileName, txtPassphrase.Text);
						MessageBox.Show("Profile saved.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
						RefreshList();
						return;
					}
					catch (Exception ex)
					{
						MessageBox.Show("Error saving profile: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Hand);
						return;
					}
				}
			}
		}

		private void btnDelete_Click(object sender, EventArgs e)
		{
			string path = GetSelectedPath();
			if (path != null && MessageBox.Show("Are you sure you want to delete this profile?", "Confirm", MessageBoxButtons.YesNo, MessageBoxIcon.Exclamation) == DialogResult.Yes)
			{
				File.Delete(path);
				RefreshList();
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
			this.gridProfiles = new System.Windows.Forms.DataGridView();
			this.colName = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.colDate = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.pnlControls = new System.Windows.Forms.Panel();
			this.btnLoad = new System.Windows.Forms.Button();
			this.btnSave = new System.Windows.Forms.Button();
			this.btnSaveAs = new System.Windows.Forms.Button();
			this.btnDelete = new System.Windows.Forms.Button();
			this.lblPass = new System.Windows.Forms.Label();
			this.txtPassphrase = new System.Windows.Forms.TextBox();
			((System.ComponentModel.ISupportInitialize)this.gridProfiles).BeginInit();
			this.pnlControls.SuspendLayout();
			base.SuspendLayout();
			this.gridProfiles.AllowUserToAddRows = false;
			this.gridProfiles.AllowUserToDeleteRows = false;
			this.gridProfiles.AllowUserToResizeRows = false;
			this.gridProfiles.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
			this.gridProfiles.Columns.AddRange(this.colName, this.colDate);
			this.gridProfiles.Dock = System.Windows.Forms.DockStyle.Fill;
			this.gridProfiles.Location = new System.Drawing.Point(0, 0);
			this.gridProfiles.MultiSelect = false;
			this.gridProfiles.Name = "gridProfiles";
			this.gridProfiles.ReadOnly = true;
			this.gridProfiles.RowHeadersVisible = false;
			this.gridProfiles.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
			this.gridProfiles.Size = new System.Drawing.Size(600, 350);
			this.gridProfiles.TabIndex = 0;
			this.colName.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
			this.colName.HeaderText = "Profile Name";
			this.colName.Name = "colName";
			this.colName.ReadOnly = true;
			this.colDate.HeaderText = "Last Modified";
			this.colDate.Name = "colDate";
			this.colDate.ReadOnly = true;
			this.colDate.Width = 150;
			this.pnlControls.Controls.Add(this.txtPassphrase);
			this.pnlControls.Controls.Add(this.lblPass);
			this.pnlControls.Controls.Add(this.btnDelete);
			this.pnlControls.Controls.Add(this.btnSaveAs);
			this.pnlControls.Controls.Add(this.btnSave);
			this.pnlControls.Controls.Add(this.btnLoad);
			this.pnlControls.Dock = System.Windows.Forms.DockStyle.Bottom;
			this.pnlControls.Location = new System.Drawing.Point(0, 350);
			this.pnlControls.Name = "pnlControls";
			this.pnlControls.Size = new System.Drawing.Size(600, 50);
			this.pnlControls.TabIndex = 1;
			this.btnLoad.Location = new System.Drawing.Point(12, 12);
			this.btnLoad.Name = "btnLoad";
			this.btnLoad.Size = new System.Drawing.Size(90, 26);
			this.btnLoad.TabIndex = 0;
			this.btnLoad.Text = "Load Profile";
			this.btnLoad.UseVisualStyleBackColor = true;
			this.btnLoad.Click += new System.EventHandler(btnLoad_Click);
			this.btnSave.Location = new System.Drawing.Point(108, 12);
			this.btnSave.Name = "btnSave";
			this.btnSave.Size = new System.Drawing.Size(90, 26);
			this.btnSave.TabIndex = 1;
			this.btnSave.Text = "Save";
			this.btnSave.UseVisualStyleBackColor = true;
			this.btnSave.Click += new System.EventHandler(btnSave_Click);
			this.btnSaveAs.Location = new System.Drawing.Point(204, 12);
			this.btnSaveAs.Name = "btnSaveAs";
			this.btnSaveAs.Size = new System.Drawing.Size(90, 26);
			this.btnSaveAs.TabIndex = 2;
			this.btnSaveAs.Text = "Save As...";
			this.btnSaveAs.UseVisualStyleBackColor = true;
			this.btnSaveAs.Click += new System.EventHandler(btnSaveAs_Click);
			this.btnDelete.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right;
			this.btnDelete.Location = new System.Drawing.Point(498, 12);
			this.btnDelete.Name = "btnDelete";
			this.btnDelete.Size = new System.Drawing.Size(90, 26);
			this.btnDelete.TabIndex = 3;
			this.btnDelete.Text = "Delete";
			this.btnDelete.UseVisualStyleBackColor = true;
			this.btnDelete.Click += new System.EventHandler(btnDelete_Click);
			this.lblPass.Location = new System.Drawing.Point(308, 17);
			this.lblPass.Name = "lblPass";
			this.lblPass.Size = new System.Drawing.Size(65, 17);
			this.lblPass.TabIndex = 4;
			this.lblPass.Text = "Passphrase:";
			this.txtPassphrase.Location = new System.Drawing.Point(379, 15);
			this.txtPassphrase.Name = "txtPassphrase";
			this.txtPassphrase.PasswordChar = '*';
			this.txtPassphrase.Size = new System.Drawing.Size(100, 20);
			this.txtPassphrase.TabIndex = 5;
			base.AutoScaleDimensions = new System.Drawing.SizeF(6f, 13f);
			base.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			base.Controls.Add(this.gridProfiles);
			base.Controls.Add(this.pnlControls);
			base.Name = "ProfilesControl";
			base.Size = new System.Drawing.Size(600, 400);
			((System.ComponentModel.ISupportInitialize)this.gridProfiles).EndInit();
			this.pnlControls.ResumeLayout(false);
			this.pnlControls.PerformLayout();
			base.ResumeLayout(false);
		}
	}
}
