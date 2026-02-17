# Theme System

The application uses a custom, manual theming engine to support both Dark (Default) and Light modes.

## Implementation hierarchy

**Namespace**: `CryptoDayTraderSuite.Themes`

### Base Theme Logic
**File**: `Themes/Theme.cs`

The static `Theme` class acts as the default Dark Theme and the applicator logic. It does not use WinForms controls' built-in "Renderer" classes but instead recursively iterates through the Control tree.

#### Color Palette (Dark)
- **Background**: `#1A1C22` (Nearly Black)
- **Panel/Control**: `#24262D` (Dark Grey)
- **Text**: `#E6E8EC` (Off-White)
- **Accent**: `#508CFF` (Blue)

### recursive Application
The `Apply(Form f)` method recursively visits every control in the form:

```csharp
foreach (Control c in f.Controls) ApplyControl(c);
```

It creates a `flat` look by:
1.  Setting `FlatStyle = Flat` for Buttons.
2.  Removing borders from `DataGridView`.
3.  Overriding `BackColor` and `ForeColor` for standard inputs (TextBox, NumericUpDown).

## Light Theme
**File**: `Themes/LightTheme.cs`

Implements the same static pattern but with a standard Bootstrap-like light palette.

- **Background**: `#F8F9FA`
- **Panel**: `#FFFFFF`
- **Text**: `#212529`
- **Accent**: `#0D6EFD`

## How to Apply
To switch themes, a form must call the static Apply method in its constructor or `Load` event:

```csharp
// In Form constructor
InitializeComponent();
CryptoDayTraderSuite.Themes.Theme.Apply(this);
```

*Note: The current implementation does not support live-switching without reloading the form or re-calling Apply() on all open windows.*
