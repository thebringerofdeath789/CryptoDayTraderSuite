# AI Simulation & Verification Guide

This guide details how to verify the Functional Simulation (Phase 15) of the AI integration.

## Prerequisites
1.  **Google Chrome** installed.
2.  **CryptoDayTraderSuite** compiled (Debug or Release).
3.  **Active Internet Connection**.

## Step 1: Prepare the AI Hook (Chrome Sidecar)
The application communicates with a specific Chrome debugging port.

1.  Close **ALL** running Chrome windows.
2.  Open a Terminal or Run dialog (Win+R).
3.  Execute:
    ```powershell
    chrome.exe --remote-debugging-port=9222 --user-data-dir="%LOCALAPPDATA%\CryptoSidecar"
    ```
4.  In the new window, navigate to [ChatGPT](https://chatgpt.com) or [Gemini](https://gemini.google.com).
5.  **Log in** manually.

## Step 2: Launch the Suite
1.  Start `CryptoDayTraderSuite.exe`.
2.  Check the **status bar** (bottom left) or logs.
    *   You should see: `[ChromeSidecar] Connected`.
    *   You should see: `[AIGovernor] AI Governor Started`.

## Step 3: Verify "Global Governor" Logic
The Governor runs every 15 minutes. To force a check or verify without waiting:
1.  Observe the **Governor Widget** on the Sidebar.
2.  It should start as `NEUTRAL`.
3.  After ~10-20 seconds (initial delay), it should change to `BULLISH` or `BEARISH` with a reason text.
    *   *If it stays Neutral*: Click "View Logs" to see if AI query failed.

## Step 4: Verify "Auto Planner" Hook
1.  Go to the **Planner** tab (or Auto Mode).
2.  Click **Generate Plan**.
3.  **Watch the Chrome Window**:
    *   You should see a prompt appear in the chat input instantly: `Review this trade...`.
    *   The AI should generate a response.
4.  **Watch the App**:
    *   If the AI says "Approve": The trade appears in the list with a note `[AI Approved: ...]`.
    *   If the AI says "Reject": The trade is silently discarded (check logs).

## Troubleshooting
*   **"Chrome Sidecar: Not connected"**: Ensure Chrome was started with port 9222.
*   **"Timeout"**: The AI took too long to respond. The app will proceed without AI (Fail Open) or discard (Fail Close) depending on config.

