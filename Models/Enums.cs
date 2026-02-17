namespace CryptoDayTraderSuite.Models
{
    public enum OrderSide { Buy, Sell } /* side */
    public enum OrderType { Market, Limit } /* type */
    public enum TimeInForce { GTC, IOC, FOK } /* tif */
    public enum Exchange { Coinbase, Kraken, Bitstamp } /* exchange */
    public enum StrategyKind { ORB, VWAPTrend, RSIReversion, Donchian } /* strategy */
    public enum Mode { Backtest, Paper, Live } /* mode */
	public enum AccountMode
	{
		Live = 0,
		Paper = 1
	}
    public enum MarketBias { Neutral, Bullish, Bearish }
}