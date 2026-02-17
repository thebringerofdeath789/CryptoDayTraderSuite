using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using CryptoDayTraderSuite.Models;
using CryptoDayTraderSuite.Services;
using CryptoDayTraderSuite.Util;

namespace CryptoDayTraderSuite.UI
{
    public partial class KeysControl : UserControl
    {
        private IKeyService _service;
        private IAccountService _accountService;
        private IHistoryService _historyService;
        private BindingSource _bs = new BindingSource();

        public KeysControl()
        {
            InitializeComponent();
            gridKeys.AutoGenerateColumns = false;
            this.Dock = DockStyle.Fill;
        }

        public void Initialize(IKeyService service, IAccountService accountService = null, IHistoryService historyService = null)
        {
            _service = service;
            _accountService = accountService;
            _historyService = historyService;
            LoadData();
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            if (!DesignMode)
            {
                LoadData();
            }
        }

        private void LoadData()
        {
            if (_service == null || DesignMode) return;
            var infos = _service.GetAll();
            var entries = infos.Select(k =>
            {
                var e = (KeyEntry)k;
                e.Active = k.Active; 
                return e;
            }).ToList();

            _bs.DataSource = new SortableBindingList<KeyEntry>(entries);
            gridKeys.DataSource = _bs;
        }

        private void btnAdd_Click(object sender, EventArgs e)
        {
            if (_service == null) return;
            var dlg = new KeyEditDialog(null, _service, _accountService, _historyService);
            if (dlg.ShowDialog(this) == DialogResult.OK)
            {
                LoadData();
            }
        }

        private void btnEdit_Click(object sender, EventArgs e)
        {
            EditSelected();
        }

        private void btnDelete_Click(object sender, EventArgs e)
        {
            if (_service == null) return;
            var cur = Selected();
            if (cur == null) return;
            if (MessageBox.Show("Delete key " + cur.Label + "?", "Confirm", MessageBoxButtons.YesNo) == DialogResult.Yes)
            {
                var broker = string.IsNullOrWhiteSpace(cur.Broker) ? cur.Service : cur.Broker;
                if (!string.IsNullOrWhiteSpace(cur.Label))
                {
                    _service.Remove(broker, cur.Label);
                }
                else
                {
                    _service.Delete(cur.Id);
                }
                LoadData();
            }
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            if (_service == null) return;
            gridKeys.EndEdit();
            var data = _bs.DataSource as SortableBindingList<KeyEntry>;
            if (data == null) return;

            foreach (var item in data)
            {
                if (item.Active)
                {
                    _service.SetActive(item.Id);
                }
            }

            var infos = data.Select(entry => 
            {
                var k = (KeyInfo)entry;
                k.Active = entry.Active;
                return k;
            }).ToList();
            
            _service.ReplaceAll(infos);
            MessageBox.Show("API keys saved.", "Save", MessageBoxButtons.OK, MessageBoxIcon.Information);
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
            if (_service == null) return;
            var cur = Selected();
            if (cur == null) return;
            var dlg = new KeyEditDialog(cur.Id, _service, _accountService, _historyService);
            if (dlg.ShowDialog(this) == DialogResult.OK)
            {
                LoadData();
            }
        }
    }
}
