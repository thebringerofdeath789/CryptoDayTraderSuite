# Auto Planner & Scoring

The **Auto Planner** module scans historical market data to rank strategies by their "Expectancy" (risk-adjusted return).

## Workflow
**Source**: `Strategy/AutoPlanner.cs` (Class: `AutoPlanner`)

1.  **Project (Scan Mode)**
    - Fetches public candles for the `lookbackDays` period.
    - Applies indicators (ATR, VWAP) to the entire dataset.
    - Runs backtests on three core signals: mainly `Donchian`, `VWAPPullback`, and `RangeReversion`.
    - Returns a list of `ProjectionRow` sorted by Expectancy.

2.  **Propose (Plan Mode)**
    - Takes the top-ranked strategies.
    - Fetches the *most recent* data (last 12-24 hours).
    - Checks if a valid signal exists **Right Now**.
    - If yes, creates a `TradePlan` sized by risk (`equity * riskPct`).

## Scoring Metrics
The `Measure` method calculates the performance of a strategy over the lookback period.

### 1. Expectancy
The primary ranking metric.
```
Expectancy = (AvgPnL - Fees) / TradeCount
```
*Note: This calculation considers Maker/Taker fees on both entry and exit.*

### 2. Sharpe Approximation
```
Sharpe = MeanPnL / StdDevPnL
```
Used to break ties or filter out highly volatile strategies.

## Signals Used
The planner currently tests specific configurations:
1.  **Donchian**: 20-period, 2x ATR exits.
2.  **VWAP Pullback**: 20-period EMA, 1.5x ATR gap.
3.  **Range Reversion**: 20-period Bollinger, active in 15% quietest regimes.

*Note: There is a secondary `Services/AutoPlanner.cs` file which appears to be a legacy or alternative implementation using `ORBStrategy`. The system currently prefers `Strategy/AutoPlanner.cs` in the UI.*
