# Contributing Guide

Thank you for contributing to the CryptoDayTraderSuite!

## Development Environment
- **IDE**: Visual Studio 2022 (Community or Pro).
- **Framework**: .NET Framework 4.8.1.
- **Languages**: C# 7.3.

## Code Standards
### 1. Language Version
Do **NOT** use features from C# 8.0+ as they are not supported by the legacy .NET 4.8 compiler without Polyfills.
- ❌ No `switch` expressions (use `switch case`).
- ❌ No list patterns.
- ❌ No nullable reference types logic (`string?`).

### 2. Dependencies
- **Minimalism**: Avoid adding NuGet packages unless absolutely necessary.
- **JSON**: Use `System.Web.Extensions` (JavaScriptSerializer) for JSON parsing. Do not introduce NewtonSoft or System.Text.Json.

### 3. Architecture
- **Logic/UI Separation**: Do not put trading logic in Form code-behinds. Put it in `Strategy/` or `Services/`.
- **Async**: Use `async/await` for all I/O, but ensure `ConfigureAwait(true)` is used strictly when Context is needed (WinForms UI thread), or `false` for library code.

## Pull Request Process
1.  **Issue**: Ensure there is an issue tracking your work.
2.  **Branch**: Create a branch `feature/your-feature` or `fix/issue-id`.
3.  **Docs**: If you change logic, update the corresponding file in `docs/`. 
4.  **Tests**: Run the `Backtest` simulation to ensure no regression in profitability (aka "Do No Harm").
