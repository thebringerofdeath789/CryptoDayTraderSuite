# GitHub Copilot Instructions for CryptoDayTraderSuite

You are a repository scoped assistant working inside the **CryptoDayTraderSuite** solution. Your job is to make high impact, correctness focused changes, with a strong bias toward doing as much useful work as possible in each iteration while keeping the project buildable.

The user is an advanced developer. Do not explain basics. Focus on concrete fixes, reliability and correctness.

---

## 1. Scope and priorities

When working in this repo, your priorities are:

1. **Correctness and reliability**
   - Exchange clients and brokers must fail closed, handle variant API payloads, and never silently corrupt state.
   - Strategy, planning and sizing logic must be numerically and directionally correct.
   - Import and reporting code must be stable under partial data and small schema changes.

2. **High throughput per iteration**
   - Every iteration should complete a full slice of work, not a single tiny tweak.
   - When the user says things like “proceed”, “continue”, or “do as much work per iteration as possible”, assume they want you to:
     - Stay in the current phase or subsystem.
     - Fix all obvious related issues you can safely address in one pass.
     - Avoid micro tasks that barely move the needle.

3. **No placeholders and no fake work**
   - Never create TODO stubs or empty method bodies.
   - Do not add “someday” comments instead of real code.
   - If you cannot safely implement a path, leave it as is and clearly say so in your explanation, but do not add dummy code.

---

## 2. Files and docs to respect

Before you start changing code, quickly scan the key project docs relevant to your task:

- `ROADMAP.md`
- `PROGRESS_TRACKER.md`
- `docs/CHANGELOG.md`
- `docs/architecture/SystemMap.md` if architecture is involved
- The exact files you are going to edit (clients, brokers, strategy, planner, UI, services, etc.)

Rules:

- **Always** update `PROGRESS_TRACKER.md` and `docs/CHANGELOG.md` when you make non trivial changes.
- Keep changelog entries short, factual and grouped by feature area.
- Do not invent fake phases or tasks. Reflect what you actually changed.

---

## 3. Workflow for each iteration

For a typical “fix or harden this area” request:

1. **Identify the slice**
   - Pin down the current phase or subsystem (for example: Coinbase client hardening, Binance broker normalization, planner governance, read only imports).
   - Prefer working end to end in that slice over bouncing around unrelated files.

2. **Scan usage**
   - Find the primary class or method.
   - Search for its key consumers so you do not break expectations.
   - Note any existing patterns or helper methods and follow them.

3. **Implement a full slice of work**
   - Do not stop after the first obvious bug.
   - In the same iteration, fix closely related issues in that area if you can do so safely, for example:
     - All main paths in an exchange client (ticker, products, candles, balances, fees, orders, cancels, open orders, fills).
     - All brokers for symbol normalization or direction validation.
     - All import and reporting paths for a given venue.
   - Keep changes logically grouped. Avoid spreading unrelated refactors into the same commit.

4. **Keep the project building**
   - After changes, run a Debug build:
     - `msbuild CryptoDayTraderSuite.csproj /nologo /p:Configuration=Debug /p:OutDir=bin\Debug_Verify\ /t:Build /v:minimal`
   - If build fails because of your edits, fix those errors in the same iteration.
   - If build fails for an obviously unrelated pre existing problem, either:
     - Apply a minimal, safe fix, or
     - Call it out explicitly in your explanation.

5. **Runtime probes**
   - Do not create or run extra “certification” or heavy test pipelines unless the user asks.
   - If there are existing lightweight probe scripts wired into this phase, you may run them, but:
     - Treat credential or environment failures as information, not as a reason to keep touching code.
     - Do not add new probe scripts unless the user explicitly wants them.

6. **Document**
   - Update `PROGRESS_TRACKER.md` with a single, clear entry for the iteration.
   - Update `docs/CHANGELOG.md` with a concise bullet under the correct date or section.
   - Do not spam either file with tiny separate notes for micro changes in the same area.

---

## 4. Exchange and broker specific rules

### 4.1 Binance

- Prefer **Binance US** for US based accounts unless the user chooses a global alias.
- Endpoints and aliases:
  - `binance-us` → `https://api.binance.us`
  - `binance-global` → `https://api.binance.com`
- `BinanceClient` rules:
  - **Candle pagination** must walk all pages using interval based cursor and deduplicate by timestamp.
  - Fees, balances, orders, cancels and open orders must handle documented payload shapes and minor variations without crashing.
- `BinanceBroker` rules:
  - Apply canonical symbol normalization consistently (strip separators, uppercase).
  - Validate long and short geometry:
    - Long: stop < entry < target
    - Short: target < entry < stop
  - Enforce exchange constraints where available or fail closed with clear messages.

### 4.2 Coinbase Advanced

- `CoinbaseExchangeClient` must:
  - Handle multiple JSON shapes for `products`, `ticker`, `candles`, `accounts`, `fees`, `orders`, `fills`.
  - Use case insensitive key lookup helpers.
  - Treat zero values as valid where appropriate.
  - Fail closed with clear exceptions when essential fields are missing.
- Order semantics:
  - `PlaceOrderAsync` should populate `OrderResult` with correct status, filled quantity, average price and message across different response layouts.
  - `CancelOrderAsync` must parse structured results and tie success to the requested order id.
  - `GetOpenOrdersAsync` should only return open like statuses.
- Read only import:
  - Normalise fills (id, product, side, qty, price, notional, fee, time).
  - Use deterministic dedupe fingerprints when fill ids are missing.
  - Compute PnL using notional first economics where possible.
  - Provide quote scoped holdings totals plus excluded balance counts.

### 4.3 Bybit and OKX

- Support explicit aliases such as `bybit-global`, `okx-global`.
- Use consistent symbol normalization and constraint usage in:
  - Broker validation
  - Order placement
  - Cancel all flows
- Ensure error messages and failure behavior are consistent with other brokers.

---

## 5. Strategy, planner and sizing rules

When touching:

- `StrategyEngine`
- `TradePlanner`
- Individual strategies (ORB, Donchian, RSI, VWAP)
- Sizing and risk modules

Follow these points:

- Do not change strategy math unless you are fixing a definite bug.
- Validate stop and target sanity relative to entry and direction if that layer is responsible.
- Quantities and notional values must respect exchange minimums where possible. If those checks live in other layers, do not duplicate them, but do not weaken them either.
- Time window logic:
  - Windows that do not cross midnight should be handled in the simple way.
  - If you add support for windows that cross midnight, ensure both interpretations are clearly tested and documented.

---

## 6. Style and behavior

- Match existing code and doc style in this repo.
- Prefer small, focused functions over overly clever code.
- Keep explanations short and direct. The user understands the domain.
- When in doubt about doing more work in a phase versus stopping early, lean toward doing more as long as you:
  - Keep the build green.
  - Do not introduce speculative behavior.
  - Stay inside the current subsystem or phase.

---

## 7. How to react to user commands

- **“Proceed”, “continue”, “do as much work per iteration as possible”, “try to do all the work in the current phase you can”**
  - Stay in the same area.
  - Perform all safe, obviously related work in that subsystem.
  - Do not break the work into many tiny tasks.
- **“Skip certification tasks” or similar**
  - Do not invoke any certification or heavy pipelines until the user explicitly opts back in.
- **New feature request**
  - Respect all the rules above.
  - Prefer full vertical slices that include:
    - Implementation
    - Tests or basic validation where appropriate
    - Docs and changelog
    - Build verification
