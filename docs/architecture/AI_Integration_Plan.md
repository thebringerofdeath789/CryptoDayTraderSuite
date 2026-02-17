# AI Integration Plan: Chrome Sidecar & Governor

## Overview
This document outlines the architecture for integrating Generative AI (ChatGPT/Gemini) into the CryptoDayTraderSuite via a local Chrome Sidecar pattern.

## Components

### 1. Chrome Sidecar (Transport Layer)
*   **Role**: WebSocket client communicating with Chrome over CDP (Chrome DevTools Protocol).
*   **Responsibility**: 
    *   Connect to an existing Chrome window (--remote-debugging-port=9222).
    *   Find the active AI chat tab.
    *   Inject prompts into the DOM.
    *   Read responses from the DOM.
*   **Safety**: Runs locally, no API keys stored in app, uses user's existing session.

### 2. AI Governor (Strategy Layer)
*   **Role**: Background service running on a 15-minute timer.
*   **Responsibility**:
    *   Fetch 4H market structure (Candles).
    *   Ask AI for "Global Market Bias" (Bullish/Bearish/Neutral).
    *   Update `StrategyEngine.GlobalBias`.
*   **Failure Mode**: If AI is unreachable, retain previous bias (User intervention required if stuck).

### 3. AutoPlanner Review Hook (Execution Layer)
*   **Role**: Interceptor in `AutoPlannerService`.
*   **Responsibility**:
    *   Before proposing a trade, send signal details to AI.
    *   Ask for "Approve/Reject" + "Reason".
    *   If Rejected: Discard trade.
    *   If Approved: Attach "AI Reason" to trade note.
    *   **Fallback**: If AI is offline, proceed with trade (Fail Open).

## Data Flow
1.  **Governor Loop**: `AIGovernor` -> `ExchangeProvider` (Get Data) -> `ChromeSidecar` (Ask AI) -> `StrategyEngine` (Set Bias).
2.  **Trade Loop**: `AutoPlanner` -> `StrategyEngine` (Check Signal) -> `ChromeSidecar` (Review) -> User UI.

## Future Enhancements (Phase 16)
*   **Bias TTL**: Revert to Neutral if Governor fails for > 2 hours.
*   **Screenshot Analysis**: Send chart screenshots to AI (requires Multimodal model).

