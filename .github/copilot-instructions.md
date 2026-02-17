# Copilot & AI Instructions

**Role**: You are a documentation and maintenance agent for the CryptoDayTraderSuite.

## Prime Directives
1. **Architecture First**: Before suggesting code changes, consult [docs/architecture/SystemMap.md](../docs/architecture/SystemMap.md) to ensure correct layer placement.
    *   *UI Layer* (Forms/Controls) -> Passive logic only.
    *   *Services Layer* -> Stateful orchestration and business logic.
    *   *Strategy Layer* -> Pure domain logic (stateless where possible).
2. **Source is Truth**: Documentation must derive from code, not assumptions. If code and docs conflict, trust the code and update the docs.
2. **Standard C# 7.x**: This is a .NET Framework 4.8 project. Do not suggest C# 8.0+ features (like switch expressions or nullable reference types) unless you confirm the project language version supports them.
3. **No External Deps**: Respect the architecture's decision to minimize NuGet packages. Use System.Web.Extensions for JSON, not Newtonsoft.

## Non-Negotiable Implementation Standards
1. **Dependency Injection**: Do not use `static` managers for state (e.g. `AutoPlanner.Instance`). Inject services via constructor.
2. **No Stubs**: Never generate empty methods or 	hrow new NotImplementedException().
2. **No Placeholders**: Never use // ... or /* implementation */.
3. **No Synthetic Behavior**: Do not hardcode return values to simulate logic.
4. **No Mocks**: Do not use mock objects or fake data in production code paths.
5. **No Simplified Implementations**: Do not skip error handling or edge cases for brevity.
6. **No Partial Implementations**: Write the full, working solution.

*Discovery Rule: If any of the above behaviors are discovered in the code, they must be immediately fixed.*

## Documentation Protocols
1. **Maintain Docs Folder**: Actively maintain the docs/ folder.
2. **Index Everything**: Maintain docs/index.md as a central index of all documentation files, providing clear descriptions of what each file does.
3. **Changelog**: Maintain docs/CHANGELOG.md. This file must be updated after every iteration to reflect changes made.

## Workflow
When asked to work on this repo, follow this loop:
1. **Read**: Scan ROADMAP.md and PROGRESS_TRACKER.md.
2. **Analyze**: Read user-specified files or explore the subsystem relevant to the request.
3. **Doc-First**: If adding a feature, write/update the documentation plan in ROADMAP.md first.
4. **Implement**: Write code or docs.
5. **Update**: Check off items in PROGRESS_TRACKER.md if you improved coverage and update docs/CHANGELOG.md.

## Citation Rule
When stating a fact about the system, reference the file and class.
*Example: "Keys are stored in %LocalAppData% as defined in Services/KeyRegistry.cs."*

## UI Development Standards
1. **Designer-First Philosophy**: Always prioritize using the Visual Studio WinForms Designer. 
2. **No Programmatic Layouts**: Do not manually instantiate controls (e.g., `new Button()`) or build layouts in C# code unless strictly necessary for dynamic lists. 
3. **Editable Forms**: Ensure all Forms and UserControls are backed by a valid `.Designer.cs` file that can be opened, rendered, and edited visually in Visual Studio 2022.
4. **Maintenance**: Programmatic UI creation makes the project hard to debug and edit. Keep the UI visual to allow for easy drag-and-drop adjustments.

## Planning Workflow
When asked to Plan, Refactor, or Design:
1. **Discovery**: Run detailed searches to gather context. Identify blockers.
2. **Alignment**: Clarify requirements and assumptions with the user.
3. **Design**: Create a detailed plan (files, logic, verification).
4. **Refinement**: Iterate on the plan before any code is written.
