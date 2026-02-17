# Governance & Trade Planning

The **Trade Planner** is the final gatekeeper for all automated trades. It applies a set of **Governance Rules** to ensure risk compliance and strategy stability.

## Governance Rules
**Source**: `Strategy/TradePlanner.cs` (Class: `GovernanceRules`)

Before any trade is marked as `Enabled`, it must pass the following checks:

### 1. Blacklisting
- **Disabled Strategies**: A strategy (string name) can be explicitly disabled.
- **Disabled Products**: Specific symbols (e.g., "BTC-USD") can be blocked.

### 2. Time Windows
- **Trading Hours**: If `NoTradeBefore` or `NoTradeAfter` are set, trades outside this window are rejected day-over-day.

### 3. Edge Validation
- **Minimum Edge**: A trade is rejected if its modeled edge (`EstEdge`) is less than `MinEdge` (default: 0.0005 or 5 basis points). This ensures we don't trade on thin margins that vanish with slippage.

### 4. Losing Streak Protection
- **Threshold**: Defaults to 4 consecutive losses.
- **Logic**: The planner tracks consecutive losses per strategy.
- **Action**: If `DisableAfterLosingStreak` is true, and a strategy hits the threshold, future candidates from that strategy are automatically disabled.

## Trade Planner Lifecycle
**Source**: `Strategy/TradePlanner.cs` (Class: `TradePlanner`)

1.  **AddCandidate**: New trades are accepted into the `Planned` list.
2.  **ApplyRules**: The `TradeRecord.Enabled` property is calculated immediately.
3.  **ReapplyAll**: If rules change (e.g., a strategy hits its loss limit), all pending trades are re-evaluated.
