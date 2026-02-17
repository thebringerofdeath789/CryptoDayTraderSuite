/* File: UI/PlannerForm_Responsive.cs */
/* makes PlannerForm resizable with a split grid layout */
/* compatible with .net framework 4.8.x and c# 7.3 */

using System;
using System.Drawing;
using System.Windows.Forms;

namespace CryptoDayTraderSuite.UI
{
    public partial class PlannerForm : Form
    {
        private SplitContainer _split; /* grids split */

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            BuildResponsivePlanner(); /* build */
            this.MinimumSize = new Size(700, 400); /* min */
        }

        private void BuildResponsivePlanner()
        {
            if (gridPlanned == null || gridPreds == null) return; /* guard */

            /* split container */
            _split = new SplitContainer();
            _split.Dock = DockStyle.Fill;
            _split.Orientation = Orientation.Vertical; /* left planned, right preds */
            _split.SplitterWidth = 6;
            _split.Panel1MinSize = 300;
            _split.Panel2MinSize = 300;
            _split.SplitterDistance = this.ClientSize.Width / 2;

            /* move controls */
            gridPlanned.Parent = _split.Panel1;
            gridPreds.Parent = _split.Panel2;
            gridPlanned.Dock = DockStyle.Fill;
            gridPreds.Dock = DockStyle.Fill;

            /* autosizing */
            gridPlanned.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            gridPreds.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;

            /* top buttons dock to top of left panel if present */
            if (btnRefresh != null)
            {
                btnRefresh.Parent = _split.Panel1;
                btnRefresh.Dock = DockStyle.Top;
            }
            if (btnSave != null)
            {
                btnSave.Parent = _split.Panel1;
                btnSave.Dock = DockStyle.Top;
            }

            /* add to form */
            this.Controls.Add(_split);
        }
    }
}