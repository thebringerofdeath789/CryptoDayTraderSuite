using System;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using CryptoDayTraderSuite.Services;
using CryptoDayTraderSuite.Themes;

namespace CryptoDayTraderSuite.UI
{
    public partial class ProfilesControl : UserControl
    {
        private IProfileService _service;
        private string _profilesDir;

        public ProfilesControl()
        {
             InitializeComponent();
             Theme.Apply(this);
             
             // Ensure grid styling 
             gridProfiles.BackgroundColor = Theme.ContentBg;
             gridProfiles.GridColor = Theme.PanelBg;
             
             _profilesDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                                         "CryptoDayTraderSuite", "Profiles");
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
            if (!Directory.Exists(_profilesDir)) return;

            var files = Directory.GetFiles(_profilesDir, "*.cdtp");
            foreach (var f in files)
            {
                var info = new FileInfo(f);
                gridProfiles.Rows.Add(Path.GetFileNameWithoutExtension(f), info.LastWriteTime.ToString("g"));
            }
        }

        private string GetSelectedPath()
        {
            if (gridProfiles.SelectedRows.Count == 0) return null;
            var name = gridProfiles.SelectedRows[0].Cells[0].Value.ToString();
            return Path.Combine(_profilesDir, name + ".cdtp");
        }

        private void btnLoad_Click(object sender, EventArgs e)
        {
            var path = GetSelectedPath();
            if (path == null) return;

            try
            {
                _service.Import(path, txtPassphrase.Text);
                MessageBox.Show("Profile loaded successfully.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading profile: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            var path = GetSelectedPath();
            if (path == null) 
            {
                btnSaveAs_Click(sender, e);
                return;
            }

            if (MessageBox.Show("Overwrite selected profile?", "Confirm", MessageBoxButtons.YesNo) != DialogResult.Yes) return;

            try
            {
                _service.Export(path, txtPassphrase.Text);
                MessageBox.Show("Profile saved.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                RefreshList();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error saving profile: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnSaveAs_Click(object sender, EventArgs e)
        {
            using (var dlg = new SaveFileDialog())
            {
                dlg.InitialDirectory = _profilesDir;
                dlg.Filter = "Crypto Profile (*.cdtp)|*.cdtp";
                dlg.DefaultExt = "cdtp";
                if (dlg.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        _service.Export(dlg.FileName, txtPassphrase.Text);
                        MessageBox.Show("Profile saved.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        RefreshList();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Error saving profile: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private void btnDelete_Click(object sender, EventArgs e)
        {
            var path = GetSelectedPath();
            if (path == null) return;

            if (MessageBox.Show("Are you sure you want to delete this profile?", "Confirm", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
            {
                File.Delete(path);
                RefreshList();
            }
        }
    }
}