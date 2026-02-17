# Trading Principles & System Philosophy

**"The goal isn’t 'win every day.' The goal is positive expectancy + survival + compounding."**

This document outlines the core philosophy driving the design of valid strategies within this suite.

## 1. Start with Edges, Not Indicators
Most losing bots start with: *"Which indicators should I use?"*
Winning systems start with: *"Where does the market structurally leak money?"*

Real edges come from:
*   **Market Microstructure**: Order book imbalances, liquidity gaps, funding rate dynamics.
*   **Behavioral Inefficiencies**: Panic selling, FOMO chasing, mean reversion after overreaction.
*   **Asymmetric Information Timing**: Reaction speed to funding updates, liquidations, volatility shifts.

## 2. Design Philosophy: Modular, Not "One Big Brain"
Different market regimes need different behavior. A strong system is a collection of small, specialized systems managed by a **Meta-Controller**.

*   **Strategies**: Trend-following, Mean-reversion, Volatility breakout, Funding-rate arbitrage.
*   **Meta-Controller**: Decides when each strategy is allowed to trade.

**Key Capability**: Each strategy can be disabled, reduce size, or go dormant based on the regime.

## 3. Risk Management is the Profit Engine
Risk management is not just "stop losses".

*   **Position Sizing**: Volatility-adjusted sizing, risk capped per trade (0.25%–1%).
*   **Kill Switches**: Max daily/weekly loss, consecutive loss limiters.
*   **Regime Detection**: Widen stops, reduce frequency, or go flat when conditions change.

## 4. Execution Quality Beats Signal Quality
A mediocre signal with good execution can beat a great signal with bad execution.

*   **Slippage**: Simulate realistic fills and penalize backtests.
*   **Latency**: Be aware of exchange lag.
*   **Liquidity**: Avoid illiquid pairs unless specifically exploiting them.

## 5. Data Matters More Than Models
Avoid overfitting on bad data.
*   **Clean History**: Handle gaps and outliers.
*   **Validation**: Use Walk-Forward validation, not just static backtesting.
*   **Rule**: If you can’t explain why a strategy works in words, it probably doesn’t.

## 6. Machine Learning (Used Carefully)
ML is a support tool, not the trader.
*   **Good Use**: Regime classification, volatility forecasting, anomaly detection.
*   **Bad Use**: Direct "buy/sell" prediction, black-box decision making.

## 7. Capital Efficiency and Compounding
*   Aim for steady returns (e.g., 0.3% per day) rather than home runs.
*   Reinvest intelligently.
*   Avoid catastrophic losses to allow compounding to work.

## 8. Operational Discipline
*   **Logging**: Know *why* a trade happened or failed.
*   **Versioning**: Track strategy decay and retire old versions without emotion.
*   **Adaptability**: Markets evolve; the system must too.

## 9. Failure Planning
Professional systems plan for:
*   Exchange downtime.
*   API bans/rate limits.
*   Fee changes.
*   Black swan events.

## 10. Why AI Trading Fails

### People use AI for the wrong job
They try to predict price direction directly. That’s the hardest problem and usually noise-dominated at day trading horizons.
*   **Better Use**: Regime classification, anomaly detection, adaptive thresholds, not direct “buy or sell” prophecy.

### Overfitting is brutal in markets
Markets are non-stationary. The pattern you trained on often dies when:
*   Volatility regime changes.
*   Participants change.
*   Fees or microstructure changes.
*   Liquidity changes.

A model that “wins” in backtest often just learned the quirks of that dataset.

### The model ignores execution reality
A model can look amazing until you account for:
*   Slippage.
*   Spreads.
*   Partial fills.
*   Maker vs taker fees.
*   Latency and rate limits.

Execution turns many “edges” negative.

### Labels are usually wrong
Most ML setups label outcomes like “price up in 5 minutes.” That ignores:
*   Risk and drawdown.
*   Path dependency (it might go up but stop you out first).
*   Liquidity constraints.

Bad labels lead to useless models.

### Concept drift kills static models
Crypto changes fast. A model trained last quarter can be garbage this quarter. Without drift detection and revalidation, you run blind.

### AI systems lack hard risk gates
People trust the model output and let it trade too big. When it’s wrong, it’s catastrophically wrong.
*   **Solution**: The risk engine must always be the boss.

### “More features” often means more leakiness
If your features include anything even slightly forward-looking by accident, or indirectly leak future info, you get fake performance that disappears live.

### Data quality is worse than people think
Missing candles, exchange outages, bad prints, timestamp drift, and survivorship bias all wreck ML.

---

**Summary**: A highly effective automated crypto day trader is **boring, disciplined, risk-obsessed, modular, and humble about uncertainty.**
