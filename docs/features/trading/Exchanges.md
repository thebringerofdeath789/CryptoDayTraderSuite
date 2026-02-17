# Exchange Connectivity

This directory documents how the application connects to external crypto exchanges (primarily Coinbase) for market data and trade execution.

## Client Libraries
**Namespace**: `CryptoDayTraderSuite.Exchanges`

The project separates logic into "Public" (market data, no auth) and "Exchange" (private, auth) clients.

### 1. Coinbase Public Client
**File**: `Exchanges/CoinbasePublicClient.cs`
**Purpose**: High-throughput market data fetching without authentication limits (mostly).

#### Key Capabilities
- **Product List**: Fetches all available pairs (e.g., "BTC-USD").
- **Candle Fetching**: 
  - Uses a **Chunking Loop** to overcome the 300-candle hard limit of the Coinbase API.
  - Automatically stitches 300-row chunks into a continuous timeline.
- **Mid-Price**: Simple ticker fetch to getting the spread midpoint.

### 2. Coinbase Exchange Client
**File**: `Exchanges/CoinbaseExchangeClient.cs`
**Purpose**: Private API access for order placement, balance checks, and fees.

#### Authentication
- **Mechanism**: Custom HMAC-SHA256 signature generation.
- **Headers**:
  - `CB-ACCESS-KEY`
  - `CB-ACCESS-SIGN` (Base64 of the HMAC)
  - `CB-ACCESS-TIMESTAMP` (Unix epoch seconds)
  - `CB-ACCESS-PASSPHRASE`
- **Security**: The `PrivateRequestAsync` method handles all signing logic. It relies on the `_secretBase64` field which must be decoded from Base64 before being used as the HMAC key.

#### Order Management
- **Type Support**: Market and Limit orders.
- **Normalization**: Converts UI symbols ("BTC/USD") to API format ("BTC-USD") and back.

## Adding a New Exchange
To add support for a new exchange (e.g., Kraken):

1.  **Create Client**: Add `Exchanges/KrakenClient.cs` implementing methods similar to `IExchangeClient` (even if not strictly using the interface yet, aiming for consistency).
2.  **Normalization**: Ensure symbol normalization (`ETH/USD` -> `ETHUSD` or `XETHZUSD`) handles exchange quirks.
3.  **Broker Adapter**: Create a `Brokers/KrakenBroker.cs` that wraps this client (see [Brokers Guide](../trading/Brokers.md)).
