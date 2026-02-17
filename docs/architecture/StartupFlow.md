# Application Startup Flow

1. **Entry Point**
   - `Program.Main()` in `Program.cs`.
   - Enables Visual Styles for WinForms.

2. **Static Initialization**
   - `KeyRegistry` static constructor triggers.
     - Loads `%LocalAppData%\CryptoDayTraderSuite\keys.json`.
     - Loads active key mappings from `keys.active.json`.
   - `Log` static initializer sets up the logging directory.
   - `Theme` initializes color palettes (Modern/Light).

3. **MainForm Initialization**
   - `new MainForm()` is instantiated.
   - `InitializeComponent()` builds the visual tree.
   - **Hooks Attachment**:
     - `MainForm_PredictionHook` attaches prediction logic.
     - `MainForm_LogHook` starts log monitoring.

4. **Runtime Loop**
   - The form awaits user input (button clicks).
   - No background threads are started automatically until a user action (e.g., "Scan" or "Connect") is triggered.
