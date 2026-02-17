# Data Flow & Rate Router

The system minimizes external dependencies by handling FX conversion and data routing internally.

## Rate Routing Service
**Class**: `Services.RateRouter`

The `RateRouter` is responsible for normalized currency conversion (e.g., "How much is 0.5 ETH worth in BTC?"). It is critical for the `AutoPlanner` which normalizes risk and equity across different trading pairs.

### Routing Logic
The `Convert(from, to, amount)` method attempts to find a price path in the following order:

1.  **Direct Market**: Checks if a pair `FROM-TO` exists (e.g., `ETH-BTC`).
2.  **Inverse Market**: Checks if `TO-FROM` exists (e.g., `BTC-ETH`) and inverts the price ($1/P$).
3.  **Hub Hop**: Attempts to route through a major liquid "Hub" asset.
    - **Hubs**: `USD`, `USDC`, `USDT`, `BTC`, `ETH`.
    - **Path**: `Convert(FROM -> HUB)` \times `Convert(HUB -> TO)`.

### Caching
- **Scope**: Instance-level cache (`_mid` Dictionary).
- **Invalidation**: No automatic invalidation (assumes short lifecycle of the Router instance during a "Scan" or "Plan" operation).
- **Source**: Fetches Ticker logic from `CoinbasePublicClient`.

## Message/Event Bus
*Current State*: The application does **not** use a global event bus.
- **Coupling**: Components verify specific dependencies (e.g., `AutoModeForm` instantiates `AutoPlanner` directly).
- **Feedback**: Feedback from `Backtest` or `Broker` execution is returned via synchronous return objects or `MessageBox` dialogs.
- **Future Impl**: A "MessageCenter" is proposed for de-coupling the UI from the Broker updates.
