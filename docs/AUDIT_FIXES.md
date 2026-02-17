# Audit Fixes Log

This document tracks the resolution of critical and major bugs identified in the automated audit.

## Resolved Issues

### AUDIT-0020: Risk Calc Risk
**Status**: Fixed
**Problem**: `RiskGuards.FeesKillEdge` calculated fees based on `tickValue * qty` (increment value) instead of `Price * qty` (Notional value), resulting in drastically underestimated fee impact for crypto percentage-based fees.
**Fix**:
- Refactored `FeesKillEdge` to accept `grossProfit` and `entryNotional` directly.
- Deprecated the broken `targetTicks` overload.

### AUDIT-0015: UI Freezing (RateRouter)
**Status**: Fixed
**Problem**: `RateRouter` and legacy `CoinbasePublicClient` methods were using `.Result` on async Tasks (Sync-over-Async), causing potential deadlocks on the UI thread.
**Fix**:
- Updated `RateRouter.MidAsync` and `ConvertAsync` to use `ConfigureAwait(false)`.
- Updated `CoinbasePublicClient` async methods to use `ConfigureAwait(false)`.
- Note: Legacy sync wrappers (`Mid`, `Convert`) still exist but are now safer against deadlocks (though still blocking).

### AUDIT-0017: Strategy Logic Error (ORB)
**Status**: Fixed
**Problem**: `ORBStrategy` assumed `dayCandles[0]` was always the market open. In multi-day backtests, this caused the Opening Range to be calculated from the start of the simulation, not the start of the day.
**Fix**:
- Updated `TrySignal` to dynamically find the 00:00 UTC candle relative to the current `index`.
- Opening Range High/Low is now calculated correctly per-day.

### AUDIT-0019: UI Hang (Coinbase History)
**Status**: Fixed
**Problem**: `CoinbasePublicClient.GetCandles` fetched history in a serial loop. While async, it could block long operations if called synchronously via `.Result`.
**Fix**:
- Applied `ConfigureAwait(false)` to all await calls in `GetCandlesAsync` to prevent UI thread context capture usage during the loop.

### AUDIT-0013: AI Amnesia
**Status**: Fixed
**Problem**: `PredictionEngine` state was in-memory only.
**Fix**:
- Added `LoadAI()` and `SaveAI()` hooks in `MainForm.cs`.
- Implemented JSON serialization for `PredictionModel`.

### AUDIT-0005: Synthetic UI
**Status**: Fixed
**Problem**: `DashboardControl` disabled Charts if `#if NETFRAMEWORK` wasn't met.
**Fix**:
- Removed `#if` directives to assume `System.Windows.Forms.DataVisualization` is available (standard in .NET Framework 4.8 and available via NuGet in Core).
