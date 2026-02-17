# Configuration & Operations

## Configuration Sources
The application does not use a central `appsettings.json` or extensive `App.config` for logic parameters. Configuration is distributed across user-specific data files.

| Config Type | Source File | Description |
|-------------|-------------|-------------|
| **Runtime Config** | `App.config` | Controls .NET Runtime version (v4.8.1). |
| **API Keys** | `keys.json` | Stores encrypted API credentials. |
| **Active Keys** | `keys.active.json` | Stores which key is currently selected per broker. |
| **User Profiles** | `profiles.json` (conceptual) | Managed by `ProfileStore`, stores account settings. |

*Note: All local JSON files are stored in `%LocalAppData%\CryptoDayTraderSuite\`.*

## Secrets Management
**Critical**: This application handles live financial credentials.
- **Storage**: Keys are **never** stored in plain text on disk.
- **Mechanism**: `System.Security.Cryptography.ProtectedData` (Windows DPAPI) is used.
- **Scope**: `DataProtectionScope.CurrentUser`. Keys encrypted on one Windows user account cannot be decrypted by another.

## Dependencies (.dll)
The application relies on standard .NET Framework assemblies.
- `System.Web.Extensions`: Used for `JavaScriptSerializer` (JSON).
- `System.Windows.Forms.DataVisualization`: Used for charting.
- `System.Security`: Used for DPAPI.
