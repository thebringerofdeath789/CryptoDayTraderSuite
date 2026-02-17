# UI Refactor & Modernization Plan

## Overview
This document outlines the plan to modernize the User Interface of the `CryptoDayTraderSuite`. The goal is to transition from a legacy `TabControl`-based layout to a responsive, single-window application with a collapsible sidebar and dedicated feature panes. This modernization also aims to expose new AI functionalities (Governor, Sidecar) that are currently hidden in backend services.

## Core Directives
1.  **Single Window Philosophy**: No popups for main navigation. All major features (Dashboard, Trading, Planner, AI, Profiles) reside within the main shell.
2.  **Modern Dark Theme**: A unified "Dark Graphite" palette (`#151717` sidebar, `#1E2026` content) to match professional trading tools.
3.  **Sidebar Navigation**: A collapsible left rail that expands on click/toggle to reveal text labels, maximizing screen real estate for charts and data.
4.  **Backend State Exposure**: UI must reflect real-time state from `AIGovernor` (Market Bias) and `ChromeSidecar` (Connection Status).

## Architectural Changes

### 1. Navigation Architecture
*   **Current**: `TabControl` with hardcoded tabs.
*   **Target**: `SidebarControl` (Left Dock) + `ContentPanel` (Fill Dock).
*   **Interaction**: 
    *   Clicking a sidebar icon loads the corresponding `UserControl` into `ContentPanel`.
    *   Sidebar has two states: `Collapsed` (Icon only, ~50px) and `Expanded` (Icon + Text, ~200px).
    *   Expansion is toggled via a "Hamburger" or "Three Dots" button.

### 2. Theme System
*   **File**: `Themes/Theme.cs`
*   **New Palette**:
    *   `SidebarBg`: `#151717`
    *   `ContentBg`: `#1E2026`
    *   `PanelBg`: `#25272D`
    *   `Accent`: `#508CFF` (Blue)
    *   `Text`: `#E6E8EC`
    *   `TextMuted`: `#808285`

### 3. Feature Panes

#### A. AI HUD (Heads Up Display)
*   **Location**: Integrated into `DashboardControl` and Sidebar Footer.
*   **Components**:
    *   **Sidecar Status**: A traffic light indicator (Red/Green) in the sidebar footer showing WebSocket connection health to Chrome.
    *   **Governor Widget**: A prominent dashboard panel displaying:
        *   **Market Bias**: Large text (BULLISH / BEARISH / NEUTRAL).
        *   **Reasoning**: The text explanation returned by the LLM.
        *   **Last Updated**: Timestamp of the last Governor cycle.

#### B. Strategy Configurator
*   **Location**: `UI/StrategyConfigDialog.cs` (launched from Trading/Auto panes).
*   **Mechanism**: Uses `PropertyGrid` and Reflection.
*   **Refactor**: `IStrategy` implementations (`DonchianStrategy`, etc.) must expose parameters as public properties with `[Category]` attributes.
*   **Goal**: Allow users to tune `Lookback`, `Risk`, and `ATR Multipliers` without code changes.

#### C. Profiles Manager
*   **Location**: `UI/ProfilesControl.cs`
*   **Layout**: Master-Detail view.
*   **Features**:
    *   **List**: Saved profiles (e.g., "Scalping", "Swing", "Testnet").
    *   **Actions**: Load, Save (Export), Delete.
    *   **Summary**: Show number of Keys and Accounts in the selected profile.

## Implementation Roadmap

### Phase 1: Foundation
1.  **Backend Events**: Add C# Events to `AIGovernor` and `ChromeSidecar` to broadcast state changes.
2.  **Theme Update**: Implement the new color palette in `Theme.cs`.
3.  **Sidebar Control**: Build the `SidebarControl` with animation/resizing logic.

### Phase 2: Shell Refactor
1.  **MainForm**: Remove `TabControl`. Connect `AIGovernor` and `ChromeSidecar` events.
2.  **Navigation Logic**: Implement the "View Switcher" in `MainForm` to swap UserControls based on Sidebar selection.

### Phase 3: Feature Implementation
1.  **AI Integration**: Build `GovernorWidget` and wire it to the real-time data.
2.  **Profiles UI**: Build `ProfilesControl` and connect it to `ProfileService`.
3.  **Strategy UI**: Refactor strategies and build the configuration dialog.

## Technical Constraints
*   **No WPF**: Must rely on standard WinForms GDI+ or standard controls.
*   **Responsiveness**: Layouts must use `Dock` and `Anchor` or `TableLayoutPanel` to handle window resizing gracefully.
*   **Performance**: Avoid recreating heavy controls (Charts) on every navigation click; cache views where possible.
