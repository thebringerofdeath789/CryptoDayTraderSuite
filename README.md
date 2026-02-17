# Crypto Day Trader Suite

**A standalone, dependency-light algorithmic trading platform for C# .NET Framework.**

## What is this?
Crypto Day Trader Suite is a Windows Forms application designed for automated and semi-automated cryptocurrency trading. It features an integrated strategy tester, a machine-learning based prediction engine, and multi-account management for both paper and live trading contexts.

Unlike complex modern stacks, this project creates a self-contained "trading desk" application using standard .NET libraries (WinForms, System.Web.Extensions) with zero reliance on heavy external NuGet packages like Newtonsoft or Entity Framework.

## Core Capabilities
- **Multi-Strategy Engine:** Includes ORB (Opening Range Breakout), VWAP Trend, Donchian Channels, and Mean Reversion strategies.
- **Auto-Planner:** Automatically ranks strategies by "Expectancy" (risk-adjusted return) based on recent market data.
- **Machine Learning:** Online Logistic/Linear Regression models (`PredictionEngine`) that learn from trade outcomes in real-time.
- **Paper & Live Modes:** Seamless switching between simulated execution and live exchange connectivity (Coinbase Exchange).
- **Security:** API keys are encrypted at rest using Windows DPAPI (per-user encryption).

## Quick Start
### Prerequisites
- Windows OS (Required for WinForms/DPAPI)
- Visual Studio 2022
- .NET Framework 4.8.1

### Build & Run
1. Open `CryptoDayTraderSuite.sln` in Visual Studio.
2. Build solution (Debug or Release).
3. Run `CryptoDayTraderSuite.exe`.

### First Run Setup
1. **Accounts**: Click the "Accounts" button to create a profile (e.g., "Paper Trader").
2. **Keys**: Go to "Keys" to add exchange credentials (API Key/Secret/Passphrase).
3. **Data**: The app connects to public APIs (Coinbase) for candle data automatically; no local database setup required.

### AI Integration (Optional)
To enable the **Chrome Sidecar** for AI Governance:
1. Close all Chrome instances.
2. Run Chrome with debugging port:
   `chrome.exe --remote-debugging-port=9222 --user-data-dir="%LOCALAPPDATA%\CryptoSidecar"`
3. Log into `chatgpt.com` or `gemini.google.com` in that window.
4. Launch the application.
   - Status: Check logs for "Chrome Sidecar Connected".
   - See [Simulation Instructions](docs/ops/SIMULATION_INSTRUCTIONS.md) for full details.

## Documentation Index
The full project documentation is located in the `docs/` folder.
**[Full Documentation Index](docs/index.md)**

- **Overview**
  - [System Map](docs/architecture/SystemMap.md) - High-level component diagram.
  - [Architecture](docs/architecture/StartupFlow.md) - Startup and Data Flow.
  - [AI Integration](docs/architecture/AI_Integration_Plan.md) - Chrome Sidecar & Governor.

- **Trading & Strategy**
  - [Strategies](docs/features/trading/Strategies.md) - Logic for ORB, VWAP, Donchian.
  - [Risk Management](docs/features/trading/RiskManagement.md) - Guards, Sizing, and Governance.
  - [Machine Learning](docs/features/trading/MachineLearning.md) - How the ML model works.
  - [Backtesting](docs/features/trading/Backtesting.md) - Simulation engine details.
  - [Exchanges](docs/features/trading/Exchanges.md) - API Client details.

- **Developer Guides**
  - [UI Guide](docs/ui/UsageGuide.md) - Usage of the Main Form and Scanner.
  - [Theming](docs/features/ui/Theming.md) - Customizing the look.
  - [Internal Ops](docs/ops/Configuration.md) - Config files and Security.
  - [Brokers](docs/features/trading/Brokers.md) - Implementing new brokers.

- **Contributing**
  - [Contributing Guide](docs/contributing/Contributing.md) - Code standards.
  - [Issue Templates](docs/contributing/IssueTemplates.md) - Bug/Feature formats.

## Safety & Disclaimer
**Use at your own risk.** This software contains logic for automated financial execution.
- Always start with **Paper Mode** accounts.
- Verify `RiskGuards` settings in code before deploying capital.
- The software is provided "as is", without warranty of any kind.