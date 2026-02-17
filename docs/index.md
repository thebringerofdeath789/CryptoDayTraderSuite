# CryptoDayTraderSuite Documentation Index

## Overview
*   [**Trading Principles**](overview/TradingPrinciples.md): The core philosophy and design principles (Edges, Risk, Modularity).

## Architecture
*   [**System Map**](architecture/SystemMap.md): The core architectural definition. Defines the "Modular Service-Oriented Kernel" pattern, layer responsibilities, and coding standards. **READ THIS FIRST**.
*   [**AI Integration Plan**](architecture/AI_Integration_Plan.md): Technical specification for the "Chrome Sidecar" AI integration.
*   [**Architecture Audit 2026**](architecture/Architecture_Audit_2026.md): Analysis of the legacy monolithic state and the plan for refactoring.
*   [**Startup Flow**](architecture/StartupFlow.md): Detailed sequence of the application initialization process.
*   [**Data Flow**](architecture/DataFlow.md): Map of how market data propagates through the system.

## Features
*   [**Feature Index**](features/FeatureIndex.md): Index of functional features.
*   [**Trading Playbook**](features/trading/Regime_Playbook.md): Regime Analysis, Examples, and Scenarios.
*   [**Strategies**](features/trading/Strategies.md): Detailed logic for ORB, VWAP, Donchian, etc.
*   [**Governance**](features/trading/Governance.md): Rules for strategy enabling/disabling.
*   [**Auto Planner**](features/trading/AutoPlanner.md): Automated scanning and ranking.
*   [**Auto Mode Automation**](features/trading/AutoMode_Automation.md): Dual-track specification for unattended auto-trading and multi-broker account profile orchestration.
*   [**Multi-Exchange Profit Architecture**](features/trading/MultiExchange_Profit_Architecture.md): Blueprint for cross-venue arbitrage, funding capture, and liquidity-aware smart routing.
*   [**Multi-Exchange Master Plan**](features/trading/MultiExchange_Implementation_MasterPlan.md): File-level add/convert/remove plan, UI changes, and unattended reliability gates for production rollout.
*   [**Multi-Exchange Execution Checklist**](features/trading/MultiExchange_Execution_Checklist.md): Strict owner/file/acceptance checklist for implementing Phase 19 and promotion gates.
*   [**Risk Management**](features/trading/RiskManagement.md): Sizing, Stops, and Guards.
*   [**Backtesting**](features/trading/Backtesting.md): Simulation engine details.
*   [**Machine Learning**](features/trading/MachineLearning.md): The Logistic Regression Prediction Engine.
*   [**Exchanges**](features/trading/Exchanges.md): API Client details.
*   [**Brokers**](features/trading/Brokers.md): Execution brokers.
*   [**AI Workflow**](features/ai/AI_Workflow.md): Guide to using the Chrome Sidecar AI integration.
*   [**Theming**](features/ui/Theming.md): Customizing the UI look and feel.

## UI & Experience
*   [**Refactor Plan**](ui/UI_Refactor_Plan.md): Modernization roadmap (Sidebar, Dark Theme).
*   [**Control Reorganization Blueprint (2026)**](ui/Control_Reorganization_Blueprint_2026.md): Detailed control audit and proposed restructuring for Trading, Planner, Auto, and Dashboard surfaces.
*   [**Multi-Exchange UI Execution Plan**](ui/MultiExchange_UI_Execution_Plan.md): Designer-first control, layout, and telemetry changes required for Phase 19 multi-venue runtime.
*   [**Usage Guide**](ui/UsageGuide.md): User guide for the application shell.

## Operations
*   [**Changelog**](CHANGELOG.md): Project history and version tracking.
*   [**Configuration**](ops/Configuration.md): Guide to application settings and secrets management.
*   [**AutoMode Matrix Validation**](ops/AutoMode_Matrix_Validation.md): Runbook + script usage for deterministic Track B matrix evidence checks.
*   [**AutoMode B5 Scenario Runner**](ops/AutoMode_B5_Scenario_Runner.md): One-command setup/run/validate flow for strict Track B5 mixed-scope evidence.
*   [**AutoMode Runtime Snapshot Verifier**](ops/AutoMode_Runtime_Snapshot.md): One-command metrics snapshot over recent AutoMode runtime logs (`cycles/fills/429/no-signal/bias-block/ai-veto`).
*   [**Multi-Exchange Certification Matrix**](ops/MultiExchange_Certification_Matrix.md): Mandatory exchange+strategy validation gates for phase rollout and unattended promotion.
*   [**Multi-Exchange Certification Runner**](ops/MultiExchange_Certification_Runner.md): One-command PASS/PARTIAL/FAIL report generation for Phase 19 certification evidence artifacts.
*   [**Multi-Exchange Provider API & Credentials**](ops/MultiExchange_Provider_API_And_Credentials.md): Provider public-API verification gate and enforced per-exchange key requirement matrix.
*   [**Roadmap**](../ROADMAP.md): High-level project goals and phase tracking.
*   [**Progress Tracker**](../PROGRESS_TRACKER.md): Granular task tracking.

## Development
*   [**Contributing Guide**](contributing/Contributing.md): Code standards and how to contribute.
*   [**Issue Templates**](contributing/IssueTemplates.md): Formats for bug reports.
*   [**Copilot Instructions**](../.github/copilot-instructions.md): Rules and guidelines for AI assistants working on this repo.
