# Trading Techniques: Regime Playbook

**Exhaustive Examples & Scenarios**  
*When they work, when they fail, and how to read conditions.*

> **DISCLAIMERS**: No strategy works in all markets. Examples are illustrative, not guarantees. Execution, fees, and risk controls decide outcomes.

## 1. Trend Following
**Core Idea**: Capture sustained directional moves driven by slow capital reallocation.

### Good Scenarios (When it Works)
*   **A) Post-breakout continuation**
    *   *Scenario*: BTC breaks a multi-week resistance with rising volume.
    *   *Condition Signals*: ADX rises above 25 and keeps rising; higher highs and higher lows on 4H and 1H; pullbacks respect rising moving average.
    *   *Why*: Funds and large traders scale in over time; pullbacks are buying opportunities.
*   **B) Macro-driven trend**
    *   *Scenario*: Dovish macro news causes sustained crypto risk-on move.
    *   *Condition Signals*: Trend persists across sessions; volatility remains elevated but directional.
    *   *Why*: Macro flows dominate short-term noise.
*   **C) Strong downtrend after distribution**
    *   *Scenario*: Asset tops, breaks support, rallies fail.
    *   *Condition Signals*: Lower highs on rallies; volume spikes on selloffs.
    *   *Why*: Trapped longs unwind gradually.

### Bad Scenarios (When it Fails)
*   **A) Range-bound chop**
    *   *Scenario*: Price oscillates in a tight box.
    *   *Condition Signals*: ADX below 20; frequent VWAP crossings.
    *   *Why*: Stop losses get hit repeatedly.
*   **B) News whipsaw**
    *   *Scenario*: Sudden regulatory headline.
    *   *Condition Signals*: Huge wicks; direction reverses within minutes.
    *   *Why*: Trend signals lag fast reversals.
*   **C) Late trend exhaustion**
    *   *Scenario*: Parabolic rise followed by distribution.
    *   *Condition Signals*: Momentum divergence; volume drops on new highs.
    *   *Why*: Trend is ending, not starting.

## 2. Mean Reversion
**Core Idea**: Exploit overreaction and snap-back to equilibrium.

### Good Scenarios (When it Works)
*   **A) Quiet range trading**
    *   *Scenario*: ETH trades sideways for days.
    *   *Condition Signals*: ADX below 20; narrow Bollinger bands.
    *   *Why*: Liquidity providers fade extremes.
*   **B) Temporary panic in stable regime**
    *   *Scenario*: Sudden 2% drop with no follow-through.
    *   *Condition Signals*: Volume spike but quick stabilization.
    *   *Why*: Emotional overreaction reverses.
*   **C) VWAP anchoring**
    *   *Scenario*: Intraday move stretches far from VWAP.
    *   *Condition Signals*: Price reverts toward VWAP repeatedly.
    *   *Why*: Institutional benchmarks cluster near VWAP.

### Bad Scenarios (When it Fails)
*   **A) Strong trend days**
    *   *Scenario*: Breakout day with heavy volume.
    *   *Condition Signals*: Price holds outside bands.
    *   *Why*: Fading strength fights momentum.
*   **B) Volatility expansion**
    *   *Scenario*: Squeeze resolves violently.
    *   *Condition Signals*: ATR jumps sharply.
    *   *Why*: Equilibrium shifts, no snap-back.
*   **C) Liquidation cascades**
    *   *Scenario*: Forced selling snowballs.
    *   *Condition Signals*: Funding spikes; open interest collapses.
    *   *Why*: Price overshoots far past prior mean.

## 3. Volatility Breakout
**Core Idea**: Trade expansion after compression.

### Good Scenarios (When it Works)
*   **A) Pre-session open**
    *   *Scenario*: Asian session quiet, EU open approaches.
    *   *Condition Signals*: Tight range before open.
    *   *Why*: Liquidity influx triggers expansion.
*   **B) Pre-news compression**
    *   *Scenario*: Market waits for CPI data.
    *   *Condition Signals*: Declining ATR.
    *   *Why*: Uncertainty resolves into movement.
*   **C) Range coil**
    *   *Scenario*: Price compresses between converging levels.
    *   *Condition Signals*: Shrinking candle size.
    *   *Why*: Energy builds then releases.

### Bad Scenarios (When it Fails)
*   **A) False breakouts**
    *   *Scenario*: Low volume push past range.
    *   *Condition Signals*: No follow-through.
    *   *Why*: Lack of participation.
*   **B) Already expanded volatility**
    *   *Scenario*: ATR already elevated.
    *   *Condition Signals*: Wide candles persist.
    *   *Why*: Expansion already priced in.

## 4. Order Book / Microstructure
**Core Idea**: Exploit short-term liquidity behavior.

### Good Scenarios (When it Works)
*   **A) Sustained bid pressure**
    *   *Scenario*: Bids stack faster than asks disappear.
    *   *Condition Signals*: Imbalance persists for seconds to minutes.
    *   *Why*: Real demand, not spoofing.
*   **B) Liquidity vacuum**
    *   *Scenario*: Thin book above price.
    *   *Condition Signals*: Few resting orders.
    *   *Why*: Price jumps quickly into void.
*   **C) Aggressive taker dominance**
    *   *Scenario*: Repeated market buys lift offers.
    *   *Condition Signals*: Taker buy volume exceeds sell.
    *   *Why*: Momentum continuation.

### Bad Scenarios (When it Fails)
*   **A) Spoof-heavy environments**
    *   *Scenario*: Large orders appear then vanish.
    *   *Condition Signals*: Imbalance flips rapidly.
    *   *Why*: Signals are fake.
*   **B) High latency**
    *   *Scenario*: Slow API updates.
    *   *Condition Signals*: Stale book snapshots.
    *   *Why*: Execution lags reality.

## 5. Funding Rate / Crowding
**Core Idea**: Exploit cost of holding crowded leverage.

### Good Scenarios (When it Works)
*   **A) Extreme positive funding**
    *   *Scenario*: Longs pay heavy funding.
    *   *Condition Signals*: Funding percentile very high.
    *   *Why*: Longs eventually unwind.
*   **B) Flat price, rising funding**
    *   *Scenario*: Price stalls but funding increases.
    *   *Condition Signals*: Divergence.
    *   *Why*: Crowd pays without price progress.
*   **C) Post-liquidation normalization**
    *   *Scenario*: Funding resets after squeeze.
    *   *Condition Signals*: Open interest drops.
    *   *Why*: Mean reversion in positioning.

### Bad Scenarios (When it Fails)
*   **A) Strong trend override**
    *   *Scenario*: Funding high but price keeps rising.
    *   *Condition Signals*: Momentum dominates.
    *   *Why*: Trend overwhelms carry cost.

## 6. Statistical Arbitrage
**Core Idea**: Exploit temporary divergence between correlated assets.

### Good Scenarios (When it Works)
*   **A) Stable pair divergence**
    *   *Scenario*: BTC and ETH diverge briefly.
    *   *Condition Signals*: Cointegration intact.
    *   *Why*: Relationship snaps back.
*   **B) Cross-exchange lag**
    *   *Scenario*: Price moves faster on one exchange.
    *   *Condition Signals*: Latency difference.
    *   *Why*: Temporary inefficiency.

### Bad Scenarios (When it Fails)
*   **A) Structural repricing**
    *   *Scenario*: Protocol upgrade changes valuation.
    *   *Condition Signals*: Correlation breaks permanently.
    *   *Why*: Old relationship invalid.

## 7. Time / Session Effects
**Core Idea**: Exploit recurring behavioral patterns.

### Good Scenarios (When it Works)
*   **A) US session volatility**
    *   *Scenario*: Increased volume at NY open.
    *   *Condition Signals*: Historical volatility spikes.
    *   *Why*: Predictable participation.
*   **B) Weekend liquidity fade**
    *   *Scenario*: Thinner books on weekends.
    *   *Condition Signals*: Lower volume.
    *   *Why*: Moves exaggerate.

### Bad Scenarios (When it Fails)
*   **A) Major unexpected news**
    *   *Scenario*: Sudden exchange failure.
    *   *Condition Signals*: Abnormal volume.
    *   *Why*: Routine patterns break.

---

## Final Meta Rule
**Every strategy requires:**
1.  **Correct Regime**
2.  **Confirmation Signals**
3.  **Strict Exit Rules**

**WRONG REGIME = EXPECTED LOSS**
