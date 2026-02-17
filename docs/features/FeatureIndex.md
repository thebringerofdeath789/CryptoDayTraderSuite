# Feature Index

A directory of user-facing features mapped to their source code implementation.

| Feature Area | Feature Name | Description | Related Code |
|--------------|--------------|-------------|--------------|
| **Trading** | Auto Planner | Scans markets and ranks strategies by expectancy. | `Services/AutoPlanner.cs`, `UI/AutoModeForm.cs` |
| **Trading** | Auto Mode Automation | Non-interactive `Scan -> Propose -> Execute` cycle with runtime guardrails and kill switch. | `UI/AutoModeControl.cs`, `Services/AutoPlannerService.cs` |
| **Trading** | Auto Profiles (Planned) | Per-account/broker pair-scope profiles (e.g., 3 pairs on one broker, all pairs on another). | `UI/AutoModeControl.cs`, `Services/AccountService.cs`, `Brokers/*` |
| **Trading** | Prediction Engine | ML model predicting direction and magnitude. | `Strategy/PredictionEngine.cs` |
| **Trading** | Risk Guards | Prevents trades with bad fee ratios or spread ticks. | `Strategy/RiskGuards.cs` |
| **Trading** | [Regime Analysis](trading/Regime_Playbook.md) | Deep dive into strategy conditions. | `Strategy/StrategySignals.cs` |
| **Account** | Profile Management | Create Paper/Live accounts with specific risk definitions. | `Services/ProfileStore.cs`, `UI/AccountsForm.cs` |
| **Account** | Key Store | Secure management of API Keys. | `Services/KeyRegistry.cs`, `UI/KeysForm.cs` |
| **Ops** | Logging | Real-time rolling logs viewable in UI. | `Util/Log.cs`, `UI/MainForm_LogHook.cs` |
| **Ops** | Paper Trading | Simulated execution environment. | `Brokers/PaperBroker.cs` |
| **Ops** | AI Governor | Chrome "Sidecar" integration for AI market analysis. | `Services/ChromeSidecar.cs`, `Services/AIGovernor.cs` |
| **UI** | Theme System | Light/Modern theme switcher. | `Themes/Theme.cs` |
