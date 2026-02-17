/* File: UI/MainForm_Responsive.cs */
/* responsive layout without compile-time Chart dependency */
/* clamps splitter distance safely and defers until size is valid */
/* compatible with .net framework 4.8.x and c# 7.3 */

using System;
using System.Drawing;
using System.Windows.Forms;

namespace CryptoDayTraderSuite
{
	public partial class MainForm : Form
	{
		private TableLayoutPanel _tl; /* main table */
		private FlowLayoutPanel _top; /* top controls row */
		private SplitContainer _split; /* top/bottom split */
		private Control _chartArea; /* chart or panel */

		/* OnLoad moved to MainForm.cs to avoid duplicate override */

		private Control FindTradeHost()
		{
			/* find a TabPage if present; else use the form */
			TabControl tc = FindFirstTab(this);
			if (tc != null && tc.TabPages.Count > 0) return tc.SelectedTab ?? tc.TabPages[0];
			return this;
		}

		private TabControl FindFirstTab(Control root)
		{
			if (root == null) return null;
			if (root is TabControl) return (TabControl)root;
			foreach (Control c in root.Controls)
			{
				var r = FindFirstTab(c);
				if (r != null) return r;
			}
			return null;
		}

		private Control FindChartLike(Control root)
		{
			/* detect an existing control whose type name contains "Chart" */
			Control found = FindByTypeName(this, "Chart");
			if (found != null) return found;
			/* fallback: create a chart host panel */
			var pnl = new Panel();
			pnl.Name = "autoChartArea";
			pnl.BackColor = Color.FromArgb(32, 34, 40);
			return pnl;
		}

		private Control FindByTypeName(Control root, string typeNameContains)
		{
			if (root == null) return null;
			foreach (Control c in root.Controls)
			{
				var t = c.GetType().Name;
				if (t.IndexOf(typeNameContains, StringComparison.OrdinalIgnoreCase) >= 0)
					return c;
				var r = FindByTypeName(c, typeNameContains);
				if (r != null) return r;
			}
			return null;
		}

		private TextBox FindLogBox()
		{
			/* prefer an existing multiline TextBox; else create one */
			var tb = FindMultilineTextBox(this);
			if (tb != null) return tb;
			tb = new TextBox();
			tb.Multiline = true;
			tb.ScrollBars = ScrollBars.Vertical;
			tb.Name = "autoLog";
			tb.Font = new Font("Consolas", 9f, FontStyle.Regular);
			return tb;
		}

		private TextBox FindMultilineTextBox(Control root)
		{
			if (root == null) return null;
			foreach (Control c in root.Controls)
			{
				var t = c as TextBox;
				if (t != null && t.Multiline) return t;
				var r = FindMultilineTextBox(c);
				if (r != null) return r;
			}
			return null;
		}

		private void BuildResponsiveLayout()
		{
			var host = FindTradeHost(); /* host to place the layout */
			if (host == null) host = this; /* fallback */

			_chartArea = FindChartLike(host); /* resolve chart-like area */
			var logBox = FindLogBox(); /* resolve log */

			_tl = new TableLayoutPanel();
			_tl.Dock = DockStyle.Fill;
			_tl.ColumnCount = 1;
			_tl.RowCount = 2;
			_tl.RowStyles.Add(new RowStyle(SizeType.AutoSize));
			_tl.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

			_top = new FlowLayoutPanel();
			_top.Dock = DockStyle.Top;
			_top.WrapContents = true;
			_top.AutoSize = true;
			_top.AutoSizeMode = AutoSizeMode.GrowAndShrink;
			_top.Padding = new Padding(6, 6, 6, 6);

			int h = 24; /* std height */
			Action<Control> addTop = c => { if (c != null) { c.Height = h; _top.Controls.Add(c); } }; /* helper */
			addTop(cmbExchange);
			addTop(cmbProduct);
			addTop(btnLoadProducts);
			addTop(btnFees);
			addTop(cmbStrategy);
			addTop(numRisk);
			addTop(numEquity);
			addTop(btnBacktest);
			addTop(btnPaper);
			addTop(btnLive);
			if (lblProj100 != null) _top.Controls.Add(lblProj100);
			if (lblProj1000 != null) _top.Controls.Add(lblProj1000);

			_split = new SplitContainer();
			_split.Dock = DockStyle.Fill;
			_split.Orientation = Orientation.Horizontal; /* chart area on top */
			_split.SplitterWidth = 6;
			_split.Panel1MinSize = 100; /* keep small so range is wide */
			_split.Panel2MinSize = 80;

			/* hook size events so we only set SplitterDistance when valid */
			_split.SizeChanged += (s, e) => SetSafeSplitterDistance();
			this.Shown += (s, e) => SetSafeSplitterDistance();

			_chartArea.Parent = _split.Panel1;
			_chartArea.Dock = DockStyle.Fill;

			logBox.Parent = _split.Panel2;
			logBox.Dock = DockStyle.Fill;

			host.Controls.Clear();
			host.Controls.Add(_tl);
			_tl.Controls.Add(_top, 0, 0);
			_tl.Controls.Add(_split, 0, 1);

			/* defer initial distance until after layout completes */
			this.BeginInvoke(new Action(SetSafeSplitterDistance));
		}

		private void SetSafeSplitterDistance()
		{
			try
			{
				if (_split == null) return;
				int total = (_split.Orientation == Orientation.Horizontal) ? _split.Height : _split.Width;
				if (total <= 0) return;

				int min = _split.Panel1MinSize;
				int max = Math.Max(min, total - _split.Panel2MinSize - _split.SplitterWidth);
				int desired = Math.Max(min, Math.Min(max, (int)(total * 0.65))); /* aim for ~65 percent to top */

				if (_split.SplitterDistance != desired)
					_split.SplitterDistance = desired;
			}
			catch
			{
				/* ignore invalid states during early layout */
			}
		}

		private bool _isFull = false; /* fullscreen state */
		private FormWindowState _prevState; /* prev state */
		private FormBorderStyle _prevBorder; /* prev border */
		private Rectangle _prevBounds; /* prev bounds */

		private void ToggleFullScreen()
		{
			if (!_isFull)
			{
				/* save previous */
				_prevState = this.WindowState;
				_prevBorder = this.FormBorderStyle;
				_prevBounds = this.Bounds;
				/* go full */
				this.FormBorderStyle = FormBorderStyle.None;
				this.WindowState = FormWindowState.Maximized;
				_isFull = true;
			}
			else
			{
				/* restore */
				this.FormBorderStyle = _prevBorder;
				this.WindowState = _prevState;
				this.Bounds = _prevBounds;
				_isFull = false;
			}
		}
	}
}
