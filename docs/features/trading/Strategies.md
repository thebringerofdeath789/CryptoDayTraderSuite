# Trading Strategies

This document details the algorithmic logic for the trading strategies implemented in `Strategy/`.

## 1. Donchian Breakout
**Source**: `Strategy/DonchianStrategy.cs`

A momentum strategy that enters when price exceeds the highest high or lowest low of a lookback period, conditioned on volume.

### Logic
- **Channel**: Calculates the Max High and Min Low over `lookback` periods (default: 20).
- **Volume Filter**: Calculates a 20-period Volume Moving Average (`vma20`).
  - Signal is valid only if `CurrentVolume / vma20 >= minVolRatio` (default: 1.2).
- **Entry**:
  - **Long**: Close > Previous High (`highs[i-1]`).
  - **Short**: Close < Previous Low (`lows[i-1]`).
- **Risk Management**:
  - **Stop Loss**: `Close` ± `atrMult * ATR` (default: 2.0).
  - **Target**: `Close` ± `atrMult * ATR` (default: 2.0).

---

## 2. Opening Range Breakout (ORB)
**Source**: `Strategy/ORBStrategy.cs`

Captures momentum validation after the market open.

### Logic
- **Opening Range**: Tracks High/Low for the first 15 minutes (default) of the session.
- **Entry**:
  - **Long**: Close > OR High + Buffer (0.25 * ATR).
  - **Short**: Close < OR Low - Buffer (0.25 * ATR).
- **Risk Management**:
  - **Stop**: Entry ± 0.75 * ATR.
  - **Target**: Entry ± 1.5 * ATR.

---

## 3. VWAP Trend
**Source**: `Strategy/VWAPTrendStrategy.cs`

A simplified trend follower using VWAP position, filtered by Market Regime (Choppiness Index).

### Logic
- **Trend**:
  - **Bullish**: Current Close > VWAP AND Previous Close > VWAP.
  - **Bearish**: Current Close < VWAP AND Previous Close < VWAP.
- **Chop-Block Filter**:
  - Calculates the **Choppiness Index (CHOP)** over 14 periods.
  - **Rejection**: If `CHOP > 61.8` (Golden Ratio), market is considered "Ranging/Choppy", and trend signals are ignored.
- **Exit Logic**:
  - **Stop Loss**: Entry ± 0.5%.
  - **Take Profit**: Entry ± 1.0%.

---

## 4. RSI Mean Reversion
**Source**: `Strategy/RSIReversionStrategy.cs`

A counter-trend strategy that enters when the Relative Strength Index (RSI) reaches extreme levels, targeting a reversion to the mean (SMA).

### Logic
- **Indicators**:
  - **RSI**: 14-period Simple RSI.
  - **ATR**: 14-period Average True Range.
  - **SMA**: 20-period Simple Moving Average (The "Mean").
- **Entry**:
  - **Long (Oversold)**: RSI < 30.
  - **Short (Overbought)**: RSI > 70.
- **Risk Management**:
  - **Stop Loss**: Entry ± 2 * ATR.
  - **Take Profit**: Dynamic target at the SMA(20) level.
- **Constraints**:
  - Requires at least 20 periods of history.

---

## Risk Guards
**Source**: `Strategy/RiskGuards.cs`

Global safety checks applied before any trade is proposed.

1.  **Fee Edge Killer**:
    - Calculates the `Gross Profit` (Target - Entry).
    - Calculates `Total Fees` (Entry Fee + Exit Fee).
    - **Rejection**: If `Fees / Gross Profit >= 0.25` (Fees eat >25% of the edge).
2.  **Spread Check**:
    - **Rejection**: If `Spread / ATR > maxSpreadToAtr` (e.g., spread is too wide relative to volatility).
3.  **Volatility Shock**:
    - **Rejection**: If `CurrentATR >= spikeFactor * MedianATR` (e.g., 1.5x spike indicates instability).
