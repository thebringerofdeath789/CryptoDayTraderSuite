# Backtesting Engine

The **Backtester** is a simplified simulation engine used to evaluate strategy performance on historical data.

## Core Logic
**Source**: `Backtest/Backtester.cs` (Class: `Backtester`)

The engine iterates through a list of historical candles, simulating a "Play-by-Play" execution of a strategy.

### Simulation Loop
1.  **Warm Preamble**: Skips the first 50 candles to allow indicators (SMA, ATR) to stabilize.
2.  **Signal Generation**: Calls the provided strategy delegate (`Func<List<Candle>, OrderRequest>`) for every time step `i`.
3.  **Position Management**:
    - **Entry**: If `Quarter != 0` (Neutral) and a Signal arrives, a position is opened at `Close[i]`.
    - **Exit**: If `Quarter != 0` (In Position) and a counter-signal (or exit signal) arrives, the position is closed at `Close[i]`.
    - **Cost Simulation**: Deducts `feeRoundTripRate / 2` on both Entry and Exit.

## Limitations
- **Fill Assumptions**: Assumes perfect fills at the **Close** price of the signal candle.
- **No Limit Orders**: Does not simulate limit order handling or order book depth.
- **Single Position**: Only supports one active position at a time (Flip-and-reverse logic).

## Usage
The backtester typically runs inside the `AutoPlanner` or `StrategyEngine` diagnostics to producing a `Result` object containing:
- **Trades**: Count of executions.
- **PnL**: Net profit/loss.
- **WinRate**: Percentage of profitable trades.
- **MaxDrawdown**: Largest peak-to-trough decline.
