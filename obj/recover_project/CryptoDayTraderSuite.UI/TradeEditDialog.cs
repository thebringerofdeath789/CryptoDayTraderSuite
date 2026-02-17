using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using CryptoDayTraderSuite.Models;

namespace CryptoDayTraderSuite.UI
{
	public class TradeEditDialog : Form
	{
		private IContainer components = null;

		private TableLayoutPanel tlMain;

		private Label lblEnabled;

		private CheckBox chkEnabled;

		private Label lblExchange;

		private TextBox txtExchange;

		private Label lblProduct;

		private TextBox txtProduct;

		private Label lblStrategy;

		private TextBox txtStrategy;

		private Label lblSide;

		private TextBox txtSide;

		private Label lblQty;

		private TextBox txtQty;

		private Label lblPrice;

		private TextBox txtPrice;

		private Label lblEdge;

		private TextBox txtEdge;

		private Label lblAtUtc;

		private DateTimePicker dtAtUtc;

		private Label lblExecuted;

		private CheckBox chkExecuted;

		private Label lblFillPrice;

		private TextBox txtFillPrice;

		private Label lblPnL;

		private TextBox txtPnL;

		private Label lblNotes;

		private TextBox txtNotes;

		private FlowLayoutPanel flowLayoutPanel1;

		private Button btnOK;

		private Button btnCancel;

		public TradeRecord Result { get; private set; }

		public TradeEditDialog()
			: this(null)
		{
		}

		public TradeEditDialog(TradeRecord rec)
		{
			InitializeComponent();
			DialogTheme.Apply(this);
			Text = ((rec == null) ? "Add Planned Trade" : "Edit Planned Trade");
			btnOK.Click += delegate
			{
				if (ValidateAndSetResult())
				{
					base.DialogResult = DialogResult.OK;
				}
			};
			btnCancel.Click += delegate
			{
				base.DialogResult = DialogResult.Cancel;
			};
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
				dtAtUtc.Value = ((rec.AtUtc == default(DateTime)) ? DateTime.UtcNow : rec.AtUtc);
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
			decimal? fillPrice = null;
			decimal? pnl = null;
			if (!decimal.TryParse(txtQty.Text, out var qty) || !decimal.TryParse(txtPrice.Text, out var price) || !decimal.TryParse(txtEdge.Text, out var edge))
			{
				MessageBox.Show("Quantity, Price, and Est. Edge must be valid numbers.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
				return false;
			}
			if (!string.IsNullOrWhiteSpace(txtFillPrice.Text))
			{
				if (!decimal.TryParse(txtFillPrice.Text, out var tmp))
				{
					MessageBox.Show("Fill Price must be a valid number.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
					return false;
				}
				fillPrice = tmp;
			}
			if (!string.IsNullOrWhiteSpace(txtPnL.Text))
			{
				if (!decimal.TryParse(txtPnL.Text, out var tmp2))
				{
					MessageBox.Show("PnL must be a valid number.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
					return false;
				}
				pnl = tmp2;
			}
			if (string.IsNullOrWhiteSpace(txtExchange.Text) || string.IsNullOrWhiteSpace(txtProduct.Text) || string.IsNullOrWhiteSpace(txtStrategy.Text) || string.IsNullOrWhiteSpace(txtSide.Text))
			{
				MessageBox.Show("Exchange, Product, Strategy, and Side are required.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
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
			this.tlMain = new System.Windows.Forms.TableLayoutPanel();
			this.lblEnabled = new System.Windows.Forms.Label();
			this.chkEnabled = new System.Windows.Forms.CheckBox();
			this.lblExchange = new System.Windows.Forms.Label();
			this.txtExchange = new System.Windows.Forms.TextBox();
			this.lblProduct = new System.Windows.Forms.Label();
			this.txtProduct = new System.Windows.Forms.TextBox();
			this.lblStrategy = new System.Windows.Forms.Label();
			this.txtStrategy = new System.Windows.Forms.TextBox();
			this.lblSide = new System.Windows.Forms.Label();
			this.txtSide = new System.Windows.Forms.TextBox();
			this.lblQty = new System.Windows.Forms.Label();
			this.txtQty = new System.Windows.Forms.TextBox();
			this.lblPrice = new System.Windows.Forms.Label();
			this.txtPrice = new System.Windows.Forms.TextBox();
			this.lblEdge = new System.Windows.Forms.Label();
			this.txtEdge = new System.Windows.Forms.TextBox();
			this.lblAtUtc = new System.Windows.Forms.Label();
			this.dtAtUtc = new System.Windows.Forms.DateTimePicker();
			this.lblExecuted = new System.Windows.Forms.Label();
			this.chkExecuted = new System.Windows.Forms.CheckBox();
			this.lblFillPrice = new System.Windows.Forms.Label();
			this.txtFillPrice = new System.Windows.Forms.TextBox();
			this.lblPnL = new System.Windows.Forms.Label();
			this.txtPnL = new System.Windows.Forms.TextBox();
			this.lblNotes = new System.Windows.Forms.Label();
			this.txtNotes = new System.Windows.Forms.TextBox();
			this.flowLayoutPanel1 = new System.Windows.Forms.FlowLayoutPanel();
			this.btnCancel = new System.Windows.Forms.Button();
			this.btnOK = new System.Windows.Forms.Button();
			this.tlMain.SuspendLayout();
			this.flowLayoutPanel1.SuspendLayout();
			base.SuspendLayout();
			this.tlMain.ColumnCount = 2;
			this.tlMain.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 132f));
			this.tlMain.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100f));
			this.tlMain.Controls.Add(this.lblEnabled, 0, 0);
			this.tlMain.Controls.Add(this.chkEnabled, 1, 0);
			this.tlMain.Controls.Add(this.lblExchange, 0, 1);
			this.tlMain.Controls.Add(this.txtExchange, 1, 1);
			this.tlMain.Controls.Add(this.lblProduct, 0, 2);
			this.tlMain.Controls.Add(this.txtProduct, 1, 2);
			this.tlMain.Controls.Add(this.lblStrategy, 0, 3);
			this.tlMain.Controls.Add(this.txtStrategy, 1, 3);
			this.tlMain.Controls.Add(this.lblSide, 0, 4);
			this.tlMain.Controls.Add(this.txtSide, 1, 4);
			this.tlMain.Controls.Add(this.lblQty, 0, 5);
			this.tlMain.Controls.Add(this.txtQty, 1, 5);
			this.tlMain.Controls.Add(this.lblPrice, 0, 6);
			this.tlMain.Controls.Add(this.txtPrice, 1, 6);
			this.tlMain.Controls.Add(this.lblEdge, 0, 7);
			this.tlMain.Controls.Add(this.txtEdge, 1, 7);
			this.tlMain.Controls.Add(this.lblAtUtc, 0, 8);
			this.tlMain.Controls.Add(this.dtAtUtc, 1, 8);
			this.tlMain.Controls.Add(this.lblExecuted, 0, 9);
			this.tlMain.Controls.Add(this.chkExecuted, 1, 9);
			this.tlMain.Controls.Add(this.lblFillPrice, 0, 10);
			this.tlMain.Controls.Add(this.txtFillPrice, 1, 10);
			this.tlMain.Controls.Add(this.lblPnL, 0, 11);
			this.tlMain.Controls.Add(this.txtPnL, 1, 11);
			this.tlMain.Controls.Add(this.lblNotes, 0, 12);
			this.tlMain.Controls.Add(this.txtNotes, 1, 12);
			this.tlMain.Controls.Add(this.flowLayoutPanel1, 0, 13);
			this.tlMain.Dock = System.Windows.Forms.DockStyle.Fill;
			this.tlMain.Location = new System.Drawing.Point(0, 0);
			this.tlMain.Name = "tlMain";
			this.tlMain.Padding = new System.Windows.Forms.Padding(14);
			this.tlMain.RowCount = 14;
			this.tlMain.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
			this.tlMain.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
			this.tlMain.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
			this.tlMain.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
			this.tlMain.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
			this.tlMain.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
			this.tlMain.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
			this.tlMain.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
			this.tlMain.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
			this.tlMain.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
			this.tlMain.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
			this.tlMain.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
			this.tlMain.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
			this.tlMain.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 84f));
			this.tlMain.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 50f));
			this.tlMain.Size = new System.Drawing.Size(560, 626);
			this.tlMain.TabIndex = 0;
			this.lblEnabled.AutoSize = true;
			this.lblEnabled.Location = new System.Drawing.Point(17, 14);
			this.lblEnabled.Name = "lblEnabled";
			this.lblEnabled.Size = new System.Drawing.Size(46, 13);
			this.lblEnabled.TabIndex = 0;
			this.lblEnabled.Text = "Enabled";
			this.chkEnabled.AutoSize = true;
			this.chkEnabled.Location = new System.Drawing.Point(149, 17);
			this.chkEnabled.Margin = new System.Windows.Forms.Padding(3, 3, 3, 8);
			this.chkEnabled.Name = "chkEnabled";
			this.chkEnabled.Size = new System.Drawing.Size(15, 14);
			this.chkEnabled.TabIndex = 1;
			this.chkEnabled.UseVisualStyleBackColor = true;
			this.lblExchange.AutoSize = true;
			this.lblExchange.Location = new System.Drawing.Point(17, 42);
			this.lblExchange.Name = "lblExchange";
			this.lblExchange.Padding = new System.Windows.Forms.Padding(0, 6, 0, 0);
			this.lblExchange.Size = new System.Drawing.Size(55, 19);
			this.lblExchange.TabIndex = 2;
			this.lblExchange.Text = "Exchange";
			this.txtExchange.Dock = System.Windows.Forms.DockStyle.Fill;
			this.txtExchange.Location = new System.Drawing.Point(149, 45);
			this.txtExchange.Margin = new System.Windows.Forms.Padding(3, 3, 3, 8);
			this.txtExchange.Name = "txtExchange";
			this.txtExchange.Size = new System.Drawing.Size(394, 20);
			this.txtExchange.TabIndex = 3;
			this.lblProduct.AutoSize = true;
			this.lblProduct.Location = new System.Drawing.Point(17, 71);
			this.lblProduct.Name = "lblProduct";
			this.lblProduct.Padding = new System.Windows.Forms.Padding(0, 6, 0, 0);
			this.lblProduct.Size = new System.Drawing.Size(44, 19);
			this.lblProduct.TabIndex = 4;
			this.lblProduct.Text = "Product";
			this.txtProduct.Dock = System.Windows.Forms.DockStyle.Fill;
			this.txtProduct.Location = new System.Drawing.Point(149, 74);
			this.txtProduct.Margin = new System.Windows.Forms.Padding(3, 3, 3, 8);
			this.txtProduct.Name = "txtProduct";
			this.txtProduct.Size = new System.Drawing.Size(394, 20);
			this.txtProduct.TabIndex = 5;
			this.lblStrategy.AutoSize = true;
			this.lblStrategy.Location = new System.Drawing.Point(17, 100);
			this.lblStrategy.Name = "lblStrategy";
			this.lblStrategy.Padding = new System.Windows.Forms.Padding(0, 6, 0, 0);
			this.lblStrategy.Size = new System.Drawing.Size(46, 19);
			this.lblStrategy.TabIndex = 6;
			this.lblStrategy.Text = "Strategy";
			this.txtStrategy.Dock = System.Windows.Forms.DockStyle.Fill;
			this.txtStrategy.Location = new System.Drawing.Point(149, 103);
			this.txtStrategy.Margin = new System.Windows.Forms.Padding(3, 3, 3, 8);
			this.txtStrategy.Name = "txtStrategy";
			this.txtStrategy.Size = new System.Drawing.Size(394, 20);
			this.txtStrategy.TabIndex = 7;
			this.lblSide.AutoSize = true;
			this.lblSide.Location = new System.Drawing.Point(17, 129);
			this.lblSide.Name = "lblSide";
			this.lblSide.Padding = new System.Windows.Forms.Padding(0, 6, 0, 0);
			this.lblSide.Size = new System.Drawing.Size(28, 19);
			this.lblSide.TabIndex = 8;
			this.lblSide.Text = "Side";
			this.txtSide.Dock = System.Windows.Forms.DockStyle.Fill;
			this.txtSide.Location = new System.Drawing.Point(149, 132);
			this.txtSide.Margin = new System.Windows.Forms.Padding(3, 3, 3, 8);
			this.txtSide.Name = "txtSide";
			this.txtSide.Size = new System.Drawing.Size(394, 20);
			this.txtSide.TabIndex = 9;
			this.lblQty.AutoSize = true;
			this.lblQty.Location = new System.Drawing.Point(17, 158);
			this.lblQty.Name = "lblQty";
			this.lblQty.Padding = new System.Windows.Forms.Padding(0, 6, 0, 0);
			this.lblQty.Size = new System.Drawing.Size(46, 19);
			this.lblQty.TabIndex = 10;
			this.lblQty.Text = "Quantity";
			this.txtQty.Dock = System.Windows.Forms.DockStyle.Fill;
			this.txtQty.Location = new System.Drawing.Point(149, 161);
			this.txtQty.Margin = new System.Windows.Forms.Padding(3, 3, 3, 8);
			this.txtQty.Name = "txtQty";
			this.txtQty.Size = new System.Drawing.Size(394, 20);
			this.txtQty.TabIndex = 11;
			this.lblPrice.AutoSize = true;
			this.lblPrice.Location = new System.Drawing.Point(17, 187);
			this.lblPrice.Name = "lblPrice";
			this.lblPrice.Padding = new System.Windows.Forms.Padding(0, 6, 0, 0);
			this.lblPrice.Size = new System.Drawing.Size(31, 19);
			this.lblPrice.TabIndex = 12;
			this.lblPrice.Text = "Price";
			this.txtPrice.Dock = System.Windows.Forms.DockStyle.Fill;
			this.txtPrice.Location = new System.Drawing.Point(149, 190);
			this.txtPrice.Margin = new System.Windows.Forms.Padding(3, 3, 3, 8);
			this.txtPrice.Name = "txtPrice";
			this.txtPrice.Size = new System.Drawing.Size(394, 20);
			this.txtPrice.TabIndex = 13;
			this.lblEdge.AutoSize = true;
			this.lblEdge.Location = new System.Drawing.Point(17, 216);
			this.lblEdge.Name = "lblEdge";
			this.lblEdge.Padding = new System.Windows.Forms.Padding(0, 6, 0, 0);
			this.lblEdge.Size = new System.Drawing.Size(52, 19);
			this.lblEdge.TabIndex = 14;
			this.lblEdge.Text = "Est. Edge";
			this.txtEdge.Dock = System.Windows.Forms.DockStyle.Fill;
			this.txtEdge.Location = new System.Drawing.Point(149, 219);
			this.txtEdge.Margin = new System.Windows.Forms.Padding(3, 3, 3, 8);
			this.txtEdge.Name = "txtEdge";
			this.txtEdge.Size = new System.Drawing.Size(394, 20);
			this.txtEdge.TabIndex = 15;
			this.lblAtUtc.AutoSize = true;
			this.lblAtUtc.Location = new System.Drawing.Point(17, 245);
			this.lblAtUtc.Name = "lblAtUtc";
			this.lblAtUtc.Padding = new System.Windows.Forms.Padding(0, 6, 0, 0);
			this.lblAtUtc.Size = new System.Drawing.Size(50, 19);
			this.lblAtUtc.TabIndex = 16;
			this.lblAtUtc.Text = "At (UTC)";
			this.dtAtUtc.CustomFormat = "yyyy-MM-dd HH:mm:ss";
			this.dtAtUtc.Dock = System.Windows.Forms.DockStyle.Fill;
			this.dtAtUtc.Format = System.Windows.Forms.DateTimePickerFormat.Custom;
			this.dtAtUtc.Location = new System.Drawing.Point(149, 248);
			this.dtAtUtc.Margin = new System.Windows.Forms.Padding(3, 3, 3, 8);
			this.dtAtUtc.Name = "dtAtUtc";
			this.dtAtUtc.Size = new System.Drawing.Size(394, 20);
			this.dtAtUtc.TabIndex = 17;
			this.lblExecuted.AutoSize = true;
			this.lblExecuted.Location = new System.Drawing.Point(17, 274);
			this.lblExecuted.Name = "lblExecuted";
			this.lblExecuted.Padding = new System.Windows.Forms.Padding(0, 6, 0, 0);
			this.lblExecuted.Size = new System.Drawing.Size(52, 19);
			this.lblExecuted.TabIndex = 18;
			this.lblExecuted.Text = "Executed";
			this.chkExecuted.AutoSize = true;
			this.chkExecuted.Location = new System.Drawing.Point(149, 277);
			this.chkExecuted.Margin = new System.Windows.Forms.Padding(3, 3, 3, 8);
			this.chkExecuted.Name = "chkExecuted";
			this.chkExecuted.Size = new System.Drawing.Size(15, 14);
			this.chkExecuted.TabIndex = 19;
			this.chkExecuted.UseVisualStyleBackColor = true;
			this.lblFillPrice.AutoSize = true;
			this.lblFillPrice.Location = new System.Drawing.Point(17, 303);
			this.lblFillPrice.Name = "lblFillPrice";
			this.lblFillPrice.Padding = new System.Windows.Forms.Padding(0, 6, 0, 0);
			this.lblFillPrice.Size = new System.Drawing.Size(47, 19);
			this.lblFillPrice.TabIndex = 20;
			this.lblFillPrice.Text = "Fill Price";
			this.txtFillPrice.Dock = System.Windows.Forms.DockStyle.Fill;
			this.txtFillPrice.Location = new System.Drawing.Point(149, 306);
			this.txtFillPrice.Margin = new System.Windows.Forms.Padding(3, 3, 3, 8);
			this.txtFillPrice.Name = "txtFillPrice";
			this.txtFillPrice.Size = new System.Drawing.Size(394, 20);
			this.txtFillPrice.TabIndex = 21;
			this.lblPnL.AutoSize = true;
			this.lblPnL.Location = new System.Drawing.Point(17, 332);
			this.lblPnL.Name = "lblPnL";
			this.lblPnL.Padding = new System.Windows.Forms.Padding(0, 6, 0, 0);
			this.lblPnL.Size = new System.Drawing.Size(26, 19);
			this.lblPnL.TabIndex = 22;
			this.lblPnL.Text = "PnL";
			this.txtPnL.Dock = System.Windows.Forms.DockStyle.Fill;
			this.txtPnL.Location = new System.Drawing.Point(149, 335);
			this.txtPnL.Margin = new System.Windows.Forms.Padding(3, 3, 3, 8);
			this.txtPnL.Name = "txtPnL";
			this.txtPnL.Size = new System.Drawing.Size(394, 20);
			this.txtPnL.TabIndex = 23;
			this.lblNotes.AutoSize = true;
			this.lblNotes.Location = new System.Drawing.Point(17, 361);
			this.lblNotes.Name = "lblNotes";
			this.lblNotes.Padding = new System.Windows.Forms.Padding(0, 6, 0, 0);
			this.lblNotes.Size = new System.Drawing.Size(35, 19);
			this.lblNotes.TabIndex = 24;
			this.lblNotes.Text = "Notes";
			this.txtNotes.Dock = System.Windows.Forms.DockStyle.Fill;
			this.txtNotes.Location = new System.Drawing.Point(149, 364);
			this.txtNotes.Margin = new System.Windows.Forms.Padding(3, 3, 3, 8);
			this.txtNotes.Multiline = true;
			this.txtNotes.Name = "txtNotes";
			this.txtNotes.Size = new System.Drawing.Size(394, 73);
			this.txtNotes.TabIndex = 25;
			this.flowLayoutPanel1.Controls.Add(this.btnCancel);
			this.flowLayoutPanel1.Controls.Add(this.btnOK);
			this.tlMain.SetColumnSpan(this.flowLayoutPanel1, 2);
			this.flowLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Fill;
			this.flowLayoutPanel1.FlowDirection = System.Windows.Forms.FlowDirection.RightToLeft;
			this.flowLayoutPanel1.Location = new System.Drawing.Point(17, 448);
			this.flowLayoutPanel1.Name = "flowLayoutPanel1";
			this.flowLayoutPanel1.Padding = new System.Windows.Forms.Padding(0, 8, 0, 0);
			this.flowLayoutPanel1.Size = new System.Drawing.Size(526, 47);
			this.flowLayoutPanel1.TabIndex = 26;
			this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.btnCancel.Location = new System.Drawing.Point(448, 11);
			this.btnCancel.Name = "btnCancel";
			this.btnCancel.Size = new System.Drawing.Size(75, 23);
			this.btnCancel.TabIndex = 1;
			this.btnCancel.Text = "Cancel";
			this.btnCancel.UseVisualStyleBackColor = true;
			this.btnOK.Location = new System.Drawing.Point(367, 11);
			this.btnOK.Name = "btnOK";
			this.btnOK.Size = new System.Drawing.Size(75, 23);
			this.btnOK.TabIndex = 0;
			this.btnOK.Text = "OK";
			this.btnOK.UseVisualStyleBackColor = true;
			base.AutoScaleDimensions = new System.Drawing.SizeF(6f, 13f);
			base.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			base.ClientSize = new System.Drawing.Size(560, 626);
			base.Controls.Add(this.tlMain);
			base.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
			base.MaximizeBox = false;
			base.MinimizeBox = false;
			base.Name = "TradeEditDialog";
			base.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
			this.Text = "Trade Details";
			this.tlMain.ResumeLayout(false);
			this.tlMain.PerformLayout();
			this.flowLayoutPanel1.ResumeLayout(false);
			base.ResumeLayout(false);
		}
	}
}
