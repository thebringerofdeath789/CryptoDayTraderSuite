# Architecture Audit & Analysis 2026

**Date:** 2026-02-06
**Scope:** Maintainability, Extensibility, Plugin Architecture
**Status:** DRAFT

## 1. Executive Summary
The `CryptoDayTraderSuite` represents a standard **WinForms Modular Monolith**. While it separates concerns into directories (Services, Strategy, Brokers), the logical coupling is extremely high. The application currently functions well for its specific use case but is resistant to extension (Open/Closed Principle violations) and difficult to test (Dependency Inversion violations).

To enable "Easy Plugins" and "Feature Extensions," a fundamental shift from **Static/Direct Instantiation** to **Dependency Injection & Interface Composition** is required.

## 2. Current Architecture Analysis

### 2.1 Coupling & Dependencies (Critical Weakness)
*   **The "MainForm" God Class**: `MainForm.cs` is the Composition Root. It manually instantiates `CoinbaseExchangeClient`, `BitstampClient`, etc., inside switch statements.
    *   *Impact*: Adding a new exchange requires modifying `MainForm.cs`, recompiling the core, and testing the entire UI.
*   **Static Glue Code**: `Services/AutoPlanner.cs` is a functional static class but hardcodes `new CoinbasePublicClient()`.
    *   *Impact*: The "Auto Planner" feature only works for Coinbase. It cannot easily support Kraken or custom data sources.
*   **Strategy Binding**: Strategies like `ORBStrategy` are hard-coded in `AutoPlanner` and `MainForm`.
    *   *Impact*: Users/Developers cannot "drop in" a DLL with a new strategy.

### 2.2 Functional Layering (Mixed Success)
*   **Brokers Layer**: `IBroker` exists, which is good. However, the UI bypasses it in places to talk directly to `IExchangeClient`.
*   **WinForms Logic**: Significant business logic sits inside `private void btn*_Click` handlers.
    *   *Impact*: Logic cannot be reused by a CLI runner, a Web API, or a Test Runner.

## 3. Proposed Architecture: The "Core + Plugin" Model

To achieve the goal of easy maintenance and plugins, we should migrate to a **Kernel-Plugin** architecture.

### 3.1 The "Core" (Kernel)
The Core Application defines *only* Interfaces and basic wiring.
*   **`IExchangePlugin`**: Defines metadata (Name, Capabilities) and factories for `IExchangeClient`.
*   **`IStrategyPlugin`**: Defines strategy logic / signals.
*   **`IServiceContainer`**: A central registry (DI Container) where plugins register themselves.

### 3.2 The Plugin System
Features are moved into modular assemblies (DLLs) or isolated namespaces that implement the Core interfaces.
*   *Example*: `CryptoDayTraderSuite.Plugins.Coinbase` (Contains `CoinbaseClient`, `CoinbaseBroker`).
*   *Example*: `CryptoDayTraderSuite.Plugins.BasicStrategies` (Contains `ORB`, `VWAP`).

### 3.3 Dependency Injection (DI)
Introduce `Microsoft.Extensions.DependencyInjection`.
*   **Before**: `var client = new CoinbaseExchangeClient(...)`
*   **After**: `var client = _serviceProvider.GetRequiredService<IExchangeFactory>().Create("Coinbase");`

### 3.4 UI Extensibility: The Shell Pattern
To allow plugins to add UI elements (Tabs, Menu Items, Toolbars) without modifying `MainForm`, the main form must become a "Shell" that aggregates UI components from registered plugins.

*   **`IMainFormShell`**: An interface exposed by the Shell to plugins (e.g., `AddTab(string title, Control content)`, `AddMenuItem(string path, Command action)`).
*   **`IUIPlugin`**: Plugins implement this to inject their UI.
    ```csharp
    public interface IUIPlugin {
        void Initialize(IMainFormShell shell);
    }
    ```
*   **Menu Composition**: `MainForm_Menu.cs` currently uses hardcoded building and Reflection (`TryClick`). This should be replaced by a `CommandRegistry` where plugins register named commands (e.g., "Backtest.Run") that the menu items invoke.

## 4. Migration & Refactoring Roadmap

This transformation can be done iteratively without breaking the app.

### Phase 1: Inversion of Control (The "Preparation")
1.  **Stop using Static Classes**: Convert `AutoPlanner` to `AutoPlannerService` (Instance).
2.  **Extract Interfaces**: Ensure `IExchangeClient` covers all UI needs.
3.  **Create Factories**: Move `switch(exchange)` logic out of `MainForm` into a `ExchangeProvider` class.

### Phase 2: The Plugin Contract (The "Interface")
1.  Define `IPlugin` interface.
2.  Update `StrategyEngine` to accept `List<IStrategy>` injected via constructor.

### Phase 3: UI Decoupling (The "Cleanup")
1.  Move `btnBacktest_Click` logic into a `BacktestService`.
2.  Usage of an **Application Event Bus** (e.g., `IMessenger`) for log messages instead of passing `TradingControl` references around.

## 5. Technology Recommendations
*   **DI Container**: `Microsoft.Extensions.DependencyInjection` (Standard, lightweight).
*   **Event Bus**: `CommunityToolkit.Mvvm.Messaging` (Excellent for decoupling WinForms components).
*   **Plugin Loading**: `System.ComponentModel.Composition` (MEF) or simple `Assembly.Load` scanning for `IPlugin`.

## 6. maintainability Audit Checklist

| Area | Status | Action Item |
| :--- | :--- | :--- |
| **Logging** | OK | `Log` class is static but works. Consider `ILogger` abstraction. |
| **Exception Handling** | Poor | `async void` handlers need `try/catch` blocks (partially present). |
| **Configuration** | Poor | Hardcoded strings in `MainForm` ("Coinbase"). Move to `appsettings.json`. |
| **Unit Tests** | Missing | `AutoPlanner` is untestable. Extract Interfaces to enable Moq. |


## 7. Performance & Safety Audit (Feb 2026)

**Auditor Role**: Architecture & Performance Auditor (Automated)

### 7.1 Efficiency Assessment
A deep scan of the codebase reveals critical hotspots impacting performance and reliability.

| Component | Status | Issue | Impact | High Risk |
| :--- | :--- | :--- | :--- | :--- |
| **Backtester** | **CRITICAL** | `candles.GetRange(0, i+1)` used inside the simulation loop. This creates O(N^2) memory allocations. A 1GB backtest could trigger 10,000 GC runs. | **High** | UI freezing during long backtests. |
| **AutoPlanner (Static)** | **CRITICAL** | Instantiates `new CoinbasePublicClient()` on every call. Uses inefficient `Take(i)` LINQ in loops. | **High** | Network socket exhaustion; Slow scans. |
| **ExchangeProvider** | **INEFFICIENT** | Decrypts API keys (`KeyRegistry.Unprotect`) on every single private request. | **Medium** | CPU waste on crypto primitives. |
| **AutoModeControl** | **BROKEN** | Injects `AutoPlannerService` (Instance) but calls `AutoPlanner` (Static). | **High** | Code divergence; Features implemented in one don't appear in the other. |

### 7.2 Missing Planning Items
Critical gaps in the repository documentation and specs were found:

1.  **AI Integration Plan (Empty)**
    *   *Before Fix*: `docs/architecture/AI_Integration_Plan.md` was 0 bytes.
    *   *Action*: Restored from backup.
2.  **Slippage & Latency Model**
    *   *Missing*: No document defines how backtests handle bid/ask spread or fill latency.
    *   *Risk*: Strategy PnL is "Paper Profit" only.
3.  **Strategy/Service Unification Plan**
    *   *Missing*: No formal decision on whether to use `Strategy/AutoPlanner.cs` (Static/Legacy) or `Services/AutoPlannerService.cs` (Modern/Instance).
    *   *Action*: Must formally deprecate the Static class.

### 7.3 Remediation Plan (Phase 8)
The following tasks have been added to the Roadmap to address these findings.

#### Priority 1: Performance Hotspots
*   [ ] **Refactor Backtester.cs**: Replace `List<Candle>.GetRange` with `ArraySegment<Candle>` or `Span<Candle>` to achieve **Zero-Allocation** windowing.
*   [ ] **Refactor ExchangeProvider**: Implement an internal cache for Authenticated Clients to prevent re-decryption overhead.

#### Priority 2: Control Flow Safety
*   [ ] **Deprecate Static AutoPlanner**: Migrate all logic from `Strategy/AutoPlanner.cs` to `Services/AutoPlannerService.cs`.
*   [ ] **Rewire AutoModeControl**: Ensure it calls `_planner.ProjectAsync` (Instance) instead of the static method.
*   [ ] **Unify Indicators**: Force all strategy services to use `Strategy/Indicators.cs` instead of duplicating math logic inline.

#### Priority 3: Reliability
*   [ ] **Add Resilience**: Add a `RateLimiter` or `RetryPolicy` (Polly) to `ExchangeProvider` to handle API 429/500 errors.
