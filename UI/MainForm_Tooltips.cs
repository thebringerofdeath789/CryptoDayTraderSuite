/* file: UI/MainForm_Tooltips.cs */
using System;
using System.Windows.Forms;

namespace CryptoDayTraderSuite
{
	public partial class MainForm : Form
	{
		/* single tooltip instance for the form */
		private ToolTip _tt;

		/* hook from wherever you initialize form events (e.g., constructor or your existing load hook) */
		public void OnShown_Tooltips(object sender, EventArgs e)
		{
			try
			{
				BuildTooltips(); /* build tooltips once the form is shown */
			}
			catch
			{
				/* swallow tooltip errors so the app never crashes here */
			}
		}

		/* build and assign tooltips for known controls; safe if controls are missing */
		private void BuildTooltips()
		{
			if (_tt == null)
			{
				_tt = new ToolTip();
				_tt.AutoPopDelay = 8000; /* how long to show tooltip */
				_tt.InitialDelay = 500;  /* delay before first show */
				_tt.ReshowDelay = 200;   /* delay for subsequent shows */
				_tt.ShowAlways = true;   /* show even when form inactive */
			}

			/* helper to set a tooltip if the control exists */
			void tip(string fieldName, string text)
			{
				var c = FindField<Control>(fieldName); /* uses UI/MainForm_Helpers.cs */
				if (c != null) _tt.SetToolTip(c, text);
			}

			/* common data actions */
			tip("btnLoadProducts", "Fetch tradable products from the selected exchange.");
			tip("btnFees", "Query current trading fees for the selected account/exchange.");

			/* trading actions */
			tip("btnBacktest", "Run strategy backtests on historical data.");
			tip("btnPaper", "Start paper trading with no real orders placed.");
			tip("btnLive", "Start live trading using the active API keys and account.");

			/* product / market selection */
			tip("cmbExchange", "Select an exchange/broker (e.g., Coinbase, Kraken, Bitstamp, Binance-US, Binance-Global, Bybit-Global, OKX-Global). Use US/global aliases for geo-specific routing.");
			tip("cmbProduct", "Select the market/pair to trade (e.g., BTC-USD).");
			tip("cmbGranularity", "Candle interval for charts and analysis (minutes).");

			/* sizing and risk */
			tip("numQty", "Order size in base units or quote currency depending on mode.");
			tip("numRisk", "Percent of equity to risk per trade.");
			tip("cmbOrderType", "Choose between market, limit, and other supported order types.");

			/* API keys and account management */
			tip("btnKeys", "Open API Key Manager to add, edit, or delete keys.");
			tip("btnAccounts", "Manage linked accounts and their trading settings.");
			tip("btnSaveKeys", "Save entered API key details securely to your profile.");
			tip("txtApiKeyId", "For Coinbase: Enter API key name (organizations/.../apiKeys/...). For others: API key ID.");
			tip("txtApiSecret", "For Coinbase: Enter EC private key PEM (-----BEGIN EC PRIVATE KEY-----...). For others: API secret.");
			tip("txtApiPassphrase", "Venue-specific passphrase/customer field (e.g., Bitstamp customer ID). Not used for Coinbase Advanced.");

			/* auto mode and proposals */
			tip("btnAutoMode", "Open Auto Mode to scan markets and propose trades by risk.");
			tip("btnPropose", "Build proposed trades from current strategy and settings.");
			tip("btnExecute", "Place the currently proposed trades on the selected account.");

			/* charts and view */
			tip("chart1", "Price chart with overlays and indicators.");
			tip("btnIndicators", "Toggle indicators and overlays (VWAP, MA, ORB, etc.).");

			/* status and logging */
			tip("txtLog", "Execution log. Use the Logs menu to open logs folder.");
			tip("btnStatus", "Open status page to compare projected vs actual PnL.");
		}
	}
}
