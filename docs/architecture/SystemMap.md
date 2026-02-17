# System Organization Map

## Architecture Overview
The system follows a **Modular Service-Oriented Architecture** pattern, utilizing Dependency Injection (DI) to compose the application at startup.

### 1. Presentation Layer (`UI/`)
*   **Shell**: `MainForm.cs` acts as the UI Shell. The actual Composition Root is `Program.cs`. It does not contain business logic. It wires Services to Views.
*   **Views**: User Controls (e.g., `TradingControl`, `AutoModeControl`) that present data and capture user intent.
*   **Pattern**: Passive View / MVP-lite. Views expose simple methods (`SetProducts`, `Log`) or Events (`BacktestClicked`), and strict logic is delegated to Services.

### 2. Service Layer (`Services/`)
*   **Definition**: Stateful, instance-based classes that perform business operations.
*   **Scope**: Singleton (long-lived) or Transient (per-task).
*   **Examples**:
    *   `AutoPlannerService`: Orchestrates market scanning and trade proposal generation.
    *   `KeyRegistry` (Moving to `KeyService`): Manages secure storage.
    *   `ChromeSidecar`: Manages the AI transport connection.

### 3. Business Core (`Strategy/`)
*   **Definition**: Pure domain logic. No UI, no File I/O, no Network calls. Highly testable.
*   **Examples**:
    *   `StrategyEngine`: Evaluates `MarketData` against Rules (`IStrategy`).
    *   `RiskGuards`: Validates orders against risk parameters.

### 4. Infrastructure Layer (`Exchanges/`, `Repositories/`)
*   **Adapters**: `CoinbaseClient`, `KrakenClient`. Implements `IExchangeClient` to talk to external APIs.
*   **Persistence**: `HistoryStore` (File I/O).

## Directory Standards

| Folder | Namespace | Purpose | Dependency Rule |
| :--- | :--- | :--- | :--- |
| **UI** | `.UI` | Forms & Controls | Can use `Services`, `Models`. |
| **Services** | `.Services` | Business Logic & Orchestration | Can use `Core`, `Infra`, `Models`. No `UI`. |
| **Strategy** | `.Strategy` | Pure Domain Logic | Can use `Models`. No `Services`, No `UI`. |
| **Exchanges** | `.Exchanges` | External API Adapters | Can use `Models`. No `UI`. |
| **Models** | `.Models` | Data Structures (POCOs) | No Logic dependencies. |

## Plugin/Extension Points (Future)
*   **Interfaces**: `IExchangeClient`, `IStrategy`, `IPlannerService`.
*   **Resolution**: `MainForm` will eventually use a DI Container to resolve `IEnumerable<IStrategy>` to populate dropdowns automatically.

## Deprecated Patterns (Active Refactoring)
*   **Static God Classes**: Global static state (e.g., legacy `AutoPlanner`, static `HistoryStore`) is being converted to Services.
*   **UI Logic**: Logic inside `btn_Click` handlers is being moved to Services.

