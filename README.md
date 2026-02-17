# CryptoDayTraderSuite

Windows-first algorithmic crypto trading workstation for strategy research, guarded execution, and multi-exchange runtime operations.

This repository is an actively developed WinForms/.NET Framework 4.8.1 codebase focused on correctness-first execution behavior:
- fail-closed broker and exchange integrations,
- deterministic validation and telemetry,
- operator-visible guardrails,
- production-minded runtime evidence tooling.

## What this project includes

- Manual and automated trading surfaces (`Trading`, `Planner`, `Auto Mode`) in a single desktop shell.
- Strategy stack with ORB, VWAP trend, Donchian, and RSI reversion paths.
- Planner/risk pipeline for stop/target geometry, sizing, and strategy ranking.
- Multi-venue exchange clients and broker adapters with service alias routing.
- Optional AI sidecar governance/proposal assist via Chrome CDP integration.
- Runtime certification tooling for matrix/provider/reject-evidence checks.

## Current exchange and routing scope

Service aliases currently used by runtime paths include:
- `coinbase-advanced`
- `binance-us`, `binance-global`
- `bybit-global`
- `okx-global`
- `kraken`
- `bitstamp`
- `paper`

Broker and client layers are designed to normalize symbols, validate trade geometry, and enforce venue constraints where available.

## Architecture at a glance

Primary repository areas:
- `UI/` - WinForms controls/dialogs (`AutoModeControl`, `PlannerControl`, `TradingControl`, account/key/profile management).
- `Services/` - orchestration and domain services (provider routing, profile/history/account services, policy/health services, optional AI governor).
- `Brokers/` - execution adapters (`IBroker`) that translate validated plans to venue-specific order/cancel semantics.
- `Exchanges/` - HTTP exchange clients with payload-shape normalization and fail-closed parsing.
- `Strategy/` - strategy logic, feature extraction, risk guards, planner integration.
- `Models/` - contracts for accounts, orders, fees, market data, projections, profiles.
- `Util/` - logging/security/http helpers and operational scripts.

For deeper architecture docs:
- [System Map](docs/architecture/SystemMap.md)
- [Startup Flow](docs/architecture/StartupFlow.md)
- [Data Flow](docs/architecture/DataFlow.md)

## Build and run

### Prerequisites
- Windows (WinForms + DPAPI assumptions)
- Visual Studio 2022
- .NET Framework 4.8.1 targeting pack

### Open and execute
1. Open `CryptoDayTraderSuite.sln`.
2. Build (`Debug` recommended for development).
3. Run from Visual Studio.

CLI build verification command used in this repo:

`msbuild CryptoDayTraderSuite.csproj /nologo /p:Configuration=Debug /p:OutDir=bin\Debug_Verify\ /t:Build /v:minimal`

## First-time operator setup

1. Create or edit accounts in `Accounts`.
2. Add credentials in `Keys` and link by service/account label.
3. Prefer `paper` mode first; validate strategy and risk behavior before live routing.
4. For Coinbase Advanced credentials, use key name + EC private key PEM format.
5. Verify logs and status surfaces before enabling unattended Auto Mode.

## Auto Mode and runtime governance

`AutoModeControl` supports profile-based orchestration with pair scope, cadence, and guardrail controls. The runtime is expected to:
- isolate profile execution state,
- enforce per-profile risk/cooldown guardrails,
- emit deterministic cycle telemetry,
- keep failure containment visible in report artifacts.

See:
- [Auto Mode Automation](docs/features/trading/AutoMode_Automation.md)
- [AutoMode Matrix Validation](docs/ops/AutoMode_Matrix_Validation.md)
- [AutoMode Runtime Snapshot](docs/ops/AutoMode_Runtime_Snapshot.md)

## Certification and operations scripts

Primary scripts:
- `Util/run_provider_public_api_probe.ps1`
- `Util/run_multiexchange_certification.ps1`
- `Util/check_multiexchange_contract.ps1`
- `Util/validate_automode_matrix.ps1`
- `Util/run_b5_validation_scenario.ps1`

Typical output artifacts are written under `obj/runtime_reports/` for local verification and audit evidence.

Key docs:
- [Multi-Exchange Certification Runner](docs/ops/MultiExchange_Certification_Runner.md)
- [Multi-Exchange Certification Matrix](docs/ops/MultiExchange_Certification_Matrix.md)
- [Provider API & Credentials](docs/ops/MultiExchange_Provider_API_And_Credentials.md)

## AI sidecar (optional)

If using the Chrome sidecar workflow:
1. Launch Chrome with a dedicated profile and remote debugging port.
2. Sign into your chosen AI provider(s) in that Chrome instance.
3. Start the app and verify sidecar/governor connectivity from runtime logs.

Detailed runbook:
- [Simulation Instructions](docs/ops/SIMULATION_INSTRUCTIONS.md)
- [AI Workflow](docs/features/ai/AI_Workflow.md)
- [AI Integration Plan](docs/architecture/AI_Integration_Plan.md)

## Security model and repository hygiene

- Secrets are intended to be protected at rest via Windows DPAPI paths used by key/account services.
- Do not commit private keys, certificates, key exports, `.user` files, local IDE state, build outputs, or runtime logs.
- Repository hygiene is enforced via `.gitignore` and by keeping generated evidence in `obj/runtime_reports/` local-only.

If you need to share runtime evidence, scrub account identifiers, key names, and any credential-adjacent payload fields first.

## Documentation map

Start here:
- [Documentation Index](docs/index.md)

Important references:
- [Roadmap](ROADMAP.md)
- [Progress Tracker](PROGRESS_TRACKER.md)
- [Changelog](docs/CHANGELOG.md)
- [Contributing](docs/contributing/Contributing.md)

## Development notes

- Keep changes correctness-focused and fail-closed in exchange/broker paths.
- Preserve deterministic telemetry fields used by certification scripts.
- Update `PROGRESS_TRACKER.md` and `docs/CHANGELOG.md` for non-trivial changes.

## Disclaimer

This software can trigger or assist financial trading decisions and order execution.
Use at your own risk, begin in paper mode, and validate strategy/risk behavior in your own environment before any live deployment.