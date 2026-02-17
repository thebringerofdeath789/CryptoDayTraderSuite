# AI Integration: The Chrome Sidecar

## Overview
The **Chrome Sidecar** is a feature that allows CryptoDayTraderSuite to leverage "Free Tier" Generative AI models (like ChatGPT, Gemini, or Claude) running in your local browser to assist with trading decisions.

Instead of paying for API keys, the application connects to a specific Chrome window via the **Chrome DevTools Protocol (CDP)** and automates the process of asking for analysis.

## Core Capabilities

### 1. The Global Governor (Strategy Layer)
*   **What it does:** Runs every 15 minutes to analyze the 4-hour market structure of Bitcoin (BTC).
*   **Goal:** Determine the global "Market Bias" (Bullish, Bearish, or Neutral).
*   **Effect:** 
    *   If **Bearish**: The system blocks new High-Risk Long entries.
    *   If **Bullish**: The system blocks new High-Risk Short entries.
    *   **Neutral**: All strategies run normally.

### 2. The Trade Reviewer (AutoPlanner)
*   **What it does:** Before the `AutoPlanner` suggests a trade plan, it sends the specific parameters (Entry, Stop, Target, Reason) to the AI.
*   **Goal:** Sanity check the trade logic.
*   **Effect:** 
    *   If the AI says **"No"**, the trade is discarded.
    *   If the AI says **"Yes"**, the trade is generated with an approval note attached.

### 3. Verified AI Proposer (Optional)
*   **What it does:** Allows AI to suggest one trade setup (`side/entry/stop/target`) for the selected symbol before the normal strategy-first proposal path.
*   **Goal:** Let AI contribute setup selection while preserving deterministic safety constraints.
*   **Hard Verification Rules (must pass all):**
    *   At least one live strategy signal must exist and align with proposed side.
    *   Trade geometry must be valid (`Buy: stop < entry < target`, `Sell: target < entry < stop`).
    *   Global bias gate still applies (`Bearish` blocks buys, `Bullish` blocks sells).
    *   Position sizing is still computed from your account risk settings.
*   **If any rule fails:** The AI proposal is rejected and planner falls back to normal strategy-first flow.

### 4. Automatic Provider Rotation (Rate-Limit Mitigation)
*   **What it does:** For non-strict planner/governor sidecar calls, request primaries rotate across `ChatGPT -> Gemini -> Claude`.
*   **Goal:** Avoid repeatedly sending consecutive requests to a single provider and reduce rate-limit concentration.
*   **Behavior:**
    *   The sidecar switches to the selected provider tab before injection.
    *   If the selected provider fails/no-sends, fallback order continues to other providers.
    *   Strict provider calls (explicitly pinned to one service) still stay pinned by design.

## How to Use (Step-by-Step)

### Step 1: Prepare Chrome
The application now attempts to auto-start Chrome in "Remote Debugging" mode if CDP is not reachable.

1.  Close all open Chrome windows.
2.  Open **Run** (Win+R) or a Terminal.
3.  Recommended manual command (still supported and useful for explicit control):
    ```powershell
    chrome.exe --remote-debugging-port=9222 --user-data-dir="%LOCALAPPDATA%\CryptoSidecar"
    ```
    *(Note: This launches a **separate** Chrome profile to keep your main browsing data safe. It will not show your existing history or logins initially, but it **will save** your login state for future sessions once you sign in.)*

### Step 2: Connect to AI
1.  In the new Chrome window, navigate to your preferred AI provider:
    *   [ChatGPT](https://chatgpt.com) (Recommended)
    *   [Gemini](https://gemini.google.com)
    *   [Claude](https://claude.ai)
2.  **Log in** to your account manually.
3.  Keep this tab open. You can minimize the window, but do not close it.

### Step 3: Launch the Suite
1.  Start `CryptoDayTraderSuite.exe`.
2.  Watch the **Logs** tab or the Dashboard Status area.
3.  You should see:
    > `[ChromeSidecar] Connecting to ChatGPT...`
    > `[ChromeSidecar] Connected via CDP.`
    > `[AIGovernor] AI Governor Started.`

### Step 4: Verification
To verify it is working:
1.  Wait ~15 minutes for the Governor loop to trigger, or check the logs for "Market Bias".
2.  (Optional) Enable AI proposer mode before launch:
    ```powershell
    $env:CDTS_AI_PROPOSER_MODE='enabled'
    ```
    *(Legacy toggle also supported: `CDTS_AI_PROPOSER_ENABLED=true`.)*
3.  Go to the **Planner** tab and run **Scan -> Propose** using the inline controls (Account, Symbol, Granularity, Lookback, Equity).
4.  If valid trades are found, you will see activity in the Chrome window as the AI prompt is injected and planner notes/logs will include either:
    *   AI proposer accepted (verified), or
    *   AI proposer rejected + fallback to strategy-first review flow.

## Troubleshooting

### "Chrome Sidecar: Not Found"
*   **Cause**: CDP host is unavailable and automatic Chrome launch could not complete (for example, Chrome not installed in standard path or policy restrictions).
*   **Fix**: Start Chrome manually with `--remote-debugging-port=9222` using the command above.

### "No AI tab found"
*   **Cause**: No supported AI tab was detected in the CDP profile (not logged in yet, tab closed, or unsupported landing page).
*   **Fix**: Open `chatgpt.com`, `gemini.google.com`, or `claude.ai` in the CDP window and sign in if prompted.

### "Connection failed"
*   **Cause**: Port 9222 is blocked or another app is using it.
*   **Fix**: Ensure no other debuggers are running. Restart the PC if necessary.

## Security Note
*   The application runs **locally**.
*   It does **not** send your credentials to any server.
*   It uses your **existing** browser session, so no API keys are stored in the app.
