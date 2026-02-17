# Brokers & Execution

The system abstracts order execution via the `IBroker` interface, supporting both simulated (Paper) and live exchange trading.

## Broker Interface
**Source**: `Brokers/IBroker.cs`

All brokers must implement:

```csharp
public interface IBroker
{
    string Service { get; }
  BrokerCapabilities GetCapabilities();
  (bool ok, string message) ValidateTradePlan(TradePlan plan);
    Task<(bool ok, string message)> PlaceOrderAsync(TradePlan plan);
    Task<(bool ok, string message)> CancelAllAsync(string symbol);
}
```

## Implementations

### 1. Paper Broker
**Source**: `Brokers/PaperBroker.cs`
- **Service Name**: `"paper"`
- **Behavior**: Simulates an instant fill at the requested price.
- **Logging**: Writes "paper fill..." to the centralized log.

### 2. Coinbase Exchange Broker
**Source**: `Brokers/CoinbaseExchangeBroker.cs`
- **Service Name**: `"coinbase-exchange"`
- **Architecture**:
  - Uses direct DI services (`IKeyService`, `IAccountService`) and direct client calls.
  - No reflection-based order path is used.
- **Key Management**:
  - Resolves selected account key id and decrypts credentials via `IKeyService.Unprotect`.
- **Execution Safety**:
  - Exposes capability contract with `SupportsProtectiveExits = false`.
  - Live auto/planner execution is fail-closed until protective bracket exits are supported.
  - Enforces precision and protective-stop/target validation via `ValidateTradePlan`.

## Broker Factory
**Source**: `UI/AutoModeForm.cs` (Class: `BrokerFactory`)

Factory routing is used by `AutoModeControl` and `PlannerControl` execution paths.

```csharp
public static IBroker GetBroker(string service, AccountMode mode, IKeyService keyService, IAccountService accountService)
{
  if (mode == AccountMode.Paper || service.Equals("paper", StringComparison.OrdinalIgnoreCase))
    return new PaperBroker();

    switch (service.ToLowerInvariant()) {
    case "coinbase-exchange": return new CoinbaseExchangeBroker(keyService, accountService);
    default: return null;
    }
}
```

## Adding a New Broker
1.  Create a class implementing `IBroker` (e.g., `Brokers/IncredibleBroker.cs`).
2.  Implement the API connection logic (preferring the `Exchanges/` namespace for the client).
3.  Implement `GetCapabilities()` and `ValidateTradePlan(...)` with explicit precision/protective-exit constraints.
4.  Update the `BrokerFactory` switch statement to recognize your service string.
5.  Ensure account key mapping and `IKeyService` lookups match the service name.
