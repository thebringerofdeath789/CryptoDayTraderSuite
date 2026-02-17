/* File: UI/PlannerForm.cs */
/* Author: Gregory King */
/* Date: 2025-08-10 */
/* Refactored: 2026-02-08 */
/* Description: planner window to show planned and past trades, allow toggles and filters */
/* Functions: ctor, SetData, LoadGrids, btnRefresh_Click, btnSave_Click */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using CryptoDayTraderSuite.Models;

namespace CryptoDayTraderSuite.UI
{
    public partial class PlannerForm : Form
    {
        private List<TradeRecord> _planned = new List<TradeRecord>();
        private List<PredictionRecord> _preds = new List<PredictionRecord>();

        public event EventHandler RequestRefresh;

        public PlannerForm()
        {
            InitializeComponent();
        }

        public void SetData(List<TradeRecord> planned, List<PredictionRecord> predictions)
        {
            _planned = planned ?? new List<TradeRecord>();
            if (predictions != null) _preds = predictions;
            LoadGrids();
        }

        private void LoadGrids()
        {
            gridPlanned.Rows.Clear();
            foreach (var p in _planned)
            {
                gridPlanned.Rows.Add(new object[] { p.Enabled, p.Exchange, p.ProductId, p.Strategy, p.Side, p.Quantity, p.Price, p.EstEdge, p.Notes });
            }

            gridPreds.Rows.Clear();
            foreach (var r in _preds.OrderByDescending(x => x.AtUtc).Take(500))
            {
                gridPreds.Rows.Add(new object[] { r.ProductId, r.AtUtc.ToLocalTime(), r.HorizonMinutes, r.Direction, r.Probability, r.ExpectedReturn, r.ExpectedVol, r.RealizedKnown, r.RealizedDirection, r.RealizedReturn });
            }
        }

        private void btnRefresh_Click(object sender, EventArgs e)
        {
            RequestRefresh?.Invoke(this, EventArgs.Empty);
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            for (int i = 0; i < gridPlanned.Rows.Count; i++)
            {
                var row = gridPlanned.Rows[i];
                if (row.Cells[0].Value != null)
                {
                    bool en = Convert.ToBoolean(row.Cells[0].Value);
                    if (i < _planned.Count) _planned[i].Enabled = en;
                }
            }
            MessageBox.Show("Planner updated. (Memory only until re-plan).", "Updated", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }
}