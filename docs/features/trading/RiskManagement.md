# Risk Management

The system employs a multi-layered risk management architecture designed to prioritize capital preservation over profit maximization.

## 1. Pre-Trade Risk Guards
**Class**: `Strategy/RiskGuards.cs`

Before any trade is generated or executed, it passes through a series of "Guards". If any guard fails, the trade is aborted.

### Fee Impact Guard (`FeesKillEdge`)
- **Logic**: Calculates the ratio of `Total Round Trip Fees` to `Gross Expected Profit`.
- **Threshold**: If fees consume > **25%** of the expected move, the trade is rejected.
- **Purpose**: prevents trading on low-volatility assets where the edge is eaten by costs.

### Spread Guard (`SpreadTooWide`)
- **Logic**: Calculates `Spread / ATR`.
- **Purpose**: Ensures we don't enter when the bid-ask spread is huge relative to the volatility (slippage risk).

### Volatility Shock Guard (`VolatilityShock`)
- **Logic**: Compares current ATR to the median ATR.
- **Threshold**: Rejects if Current > `SpikeFactor * Median`.
- **Purpose**: Avoids trading during news events or flash crashes where volatility becomes unpredictable.

## 2. Position Sizing
**Implementation**: `Strategy/AutoPlanner.cs` (in `Propose` method)

The system uses **Volatility-Adjusted Sizing** rather than fixed quantity or fixed capital sizing.

### Formula
$$ Qty = \frac{Equity \times Risk\%}{ATR_{14}} $$

- **Equity**: The capital allocated to this basket/trade.
- **Risk%**: Percentage of equity willing to lose (e.g., 1%).
- **ATR**: Average True Range (14-period).

**Rationale**: This standardizes risk across assets. A highly volatile asset (High ATR) gets a smaller position size, while a stable asset gets a larger size, ensuring the *dollar risk* remains constant.

## 3. Governance (Post-Trade)
**File**: `Strategy/TradePlanner.cs` (and `Blacklist` logic)

Governance rules monitor performance and disable malfunctioning components.

### Streak Breakers
- If a strategy incurs `N` consecutive losses, it is temporarily paused.
- **Status**: Currently implemented in the Planner's "Enable" toggles (manual intervention required based on Review).

### Blacklisting
- Strategies or Pairs that fall below a certain `Expectancy` or `Win Rate` over a rolling window are flagged for removal in the `AutoPlanner` ranking.
