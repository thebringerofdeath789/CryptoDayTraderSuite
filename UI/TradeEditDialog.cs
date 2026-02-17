using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using CryptoDayTraderSuite.Models;
using CryptoDayTraderSuite.Services;

namespace CryptoDayTraderSuite.UI
{
    public partial class TradeEditDialog : Form
    {
        public TradeRecord Result { get; private set; }

        public TradeEditDialog() : this(null) { }

        public TradeEditDialog(TradeRecord rec)
        {
            InitializeComponent();
            DialogTheme.Apply(this);
            
            this.Text = rec == null ? "Add Planned Trade" : "Edit Planned Trade";
            
            // Wire up buttons
            btnOK.Click += (s, e) => { if (ValidateAndSetResult()) this.DialogResult = DialogResult.OK; };
            btnCancel.Click += (s, e) => { this.DialogResult = DialogResult.Cancel; };

            if (rec != null)
            {
                chkEnabled.Checked = rec.Enabled;
                txtExchange.Text = rec.Exchange;
                txtProduct.Text = rec.ProductId;
                txtStrategy.Text = rec.Strategy;
                txtSide.Text = rec.Side;
                txtQty.Text = rec.Quantity.ToString();
                txtPrice.Text = rec.Price.ToString();
                txtEdge.Text = rec.EstEdge.ToString();
                dtAtUtc.Value = rec.AtUtc == default(DateTime) ? DateTime.UtcNow : rec.AtUtc;
                chkExecuted.Checked = rec.Executed;
                txtFillPrice.Text = rec.FillPrice?.ToString() ?? "";
                txtPnL.Text = rec.PnL?.ToString() ?? "";
                txtNotes.Text = rec.Notes;
            }
            else
            {
                chkEnabled.Checked = true;
                txtExchange.Text = "";
                txtProduct.Text = "";
                txtStrategy.Text = "";
                txtSide.Text = "";
                txtQty.Text = "0";
                txtPrice.Text = "0";
                txtEdge.Text = "0";
                dtAtUtc.Value = DateTime.UtcNow;
                chkExecuted.Checked = false;
                txtFillPrice.Text = "";
                txtPnL.Text = "";
                txtNotes.Text = "";
            }
        }

        private bool ValidateAndSetResult()
        {
            decimal qty, price, edge;
            decimal? fillPrice = null, pnl = null;
            if (!decimal.TryParse(txtQty.Text, out qty) || !decimal.TryParse(txtPrice.Text, out price) || !decimal.TryParse(txtEdge.Text, out edge))
            {
                MessageBox.Show("Quantity, Price, and Est. Edge must be valid numbers.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }
            if (!string.IsNullOrWhiteSpace(txtFillPrice.Text))
            {
                decimal tmp;
                if (!decimal.TryParse(txtFillPrice.Text, out tmp))
                {
                    MessageBox.Show("Fill Price must be a valid number.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return false;
                }
                fillPrice = tmp;
            }
            if (!string.IsNullOrWhiteSpace(txtPnL.Text))
            {
                decimal tmp;
                if (!decimal.TryParse(txtPnL.Text, out tmp))
                {
                    MessageBox.Show("PnL must be a valid number.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return false;
                }
                pnl = tmp;
            }
            if (string.IsNullOrWhiteSpace(txtExchange.Text) || string.IsNullOrWhiteSpace(txtProduct.Text) || string.IsNullOrWhiteSpace(txtStrategy.Text) || string.IsNullOrWhiteSpace(txtSide.Text))
            {
                MessageBox.Show("Exchange, Product, Strategy, and Side are required.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }
            Result = new TradeRecord
            {
                Enabled = chkEnabled.Checked,
                Exchange = txtExchange.Text.Trim(),
                ProductId = txtProduct.Text.Trim(),
                Strategy = txtStrategy.Text.Trim(),
                Side = txtSide.Text.Trim(),
                Quantity = qty,
                Price = price,
                EstEdge = edge,
                AtUtc = dtAtUtc.Value.ToUniversalTime(),
                Executed = chkExecuted.Checked,
                FillPrice = fillPrice,
                PnL = pnl,
                Notes = txtNotes.Text.Trim()
            };
            return true;
        }
    }
}
