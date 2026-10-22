# Implementation Plan: Dev Test Evidence Capture

**Branch**: `001-dev-test-evidence-capture` | **Date**: 2026-09-10 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `/specs/001-dev-test-evidence-capture/spec.md`

## Summary

A Windows desktop WPF utility that lets a developer tag a manual test session with a ticket number
and scenario name, then capture an ordered sequence of full-screen PNG screenshots via a global
hotkey while working in any other application. On stopping a session the user may optionally assign
meaningful names to screenshots (numeric prefix preserved, conflicts blocked) or skip renaming
entirely. Every session folder carries a `scenario.json` metadata file that always reflects the final
filenames, so a future tool (e.g., a Claude Code skill) can discover tickets/scenarios/screenshots
without needing this app. Technical approach: a small two-project .NET 8 solution (WPF `App` +
plain-class-library `Core`), Win32 interop (`RegisterHotKey`, `GetSystemMetrics`) for global capture,
`System.Drawing.Common` for the actual bitmap grab/PNG encode, `System.Text.Json` for metadata and
configuration, and xUnit for testing the deterministic Core logic — no database, no network, no
third-party frameworks beyond what's needed to talk to Win32.

## Technical Context

**Language/Version**: C# 12 / .NET 8 (`net8.0-windows`)

**Primary Dependencies**: WPF (`Microsoft.WindowsDesktop.App` SDK), `System.Drawing.Common`
(Microsoft-authored, Windows-only bitmap capture/PNG encode), `System.Text.Json` (BCL — config and
`scenario.json`), Win32 `user32.dll`/`gdi32.dll` via `P/Invoke` (global hotkey, primary-monitor
metrics). Test-only: xUnit + `Microsoft.NET.Test.Sdk` (dev dependency, not shipped in the published
app).

**Storage**: Local filesystem only — PNG files and one `scenario.json` per session folder, plus a
single local JSON config file under `%LOCALAPPDATA%\DevTestEvidenceCapture\config.json`. No database.

**Testing**: xUnit unit tests against the `Core` class library (naming, sanitization, numbering,
conflict handling, metadata read/write, rename operations). No UI automation in Phase 1.

**Target Platform**: Windows 10/11 desktop, x64. Published as a self-contained single-file `.exe`
(`dotnet publish -r win-x64 --self-contained true -p:PublishSingleFile=true`) so it can be handed to a
developer's machine without a separate .NET runtime install.

**Project Type**: Desktop application (WPF UI + class library + test project).

**Performance Goals**: Hotkey-to-saved-screenshot latency imperceptible to the user (target <500ms on
typical hardware) so it never interrupts the flow of manual testing; app launch-to-ready-to-capture
under 30s (SC-001); rename-or-skip flow for 10 screenshots completable in under 2 minutes (SC-003).

**Constraints**: Fully offline — no network calls anywhere in the app (Principle II). Must not require
administrator elevation to register a global hotkey or write to the configured evidence folder. Only
one capture session active at a time (FR-011). Capture must keep working while the app window is
minimized/unfocused (Principle VI, FR-005).

**Scale/Scope**: Single local user, single machine, one active session at a time; a session
realistically holds up to a few hundred screenshots. No concurrency or multi-user concerns.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Check | Result |
|---|---|---|
| I. Radical Simplicity & Phase-1 Scope Discipline | Two projects (App, Core) + one test project; only two non-BCL dependencies (`System.Drawing.Common` for capture, xUnit for tests-only) | PASS |
| II. Local-First, Zero Data Egress | No network/HTTP client code anywhere; no telemetry | PASS |
| III. Idiomatic, Maintainable C# on .NET 8 / WPF | Stack is exactly .NET 8 + WPF + C#, nullable reference types enabled solution-wide | PASS |
| IV. Separation of Concerns | `Core` splits into `Session`, `Capture`, `Hotkey`, `Storage`, `Naming`, `Metadata`, `Configuration` namespaces behind interfaces; `App` (UI) only binds to view models that call those interfaces | PASS |
| V. Deterministic, Ordered, Non-Destructive Persistence | Dedicated `Naming`/`Storage` components own sequence numbering, sanitization, and collision handling; never write over an existing file | PASS |
| VI. Reliable Global Capture | `RegisterHotKey` is an OS-level registration tied to a window handle, not window visibility/focus state — works minimized/unfocused by design | PASS |
| VII. Simple, Local Configuration | One local JSON config file, sensible built-in defaults, no setup required to run | PASS |
| VIII. Forward-Compatible Metadata, No Premature Integration | `scenario.json` schema fixed in `contracts/`; no AI/Claude/JIRA code present | PASS |
| IX. Automated Tests for Business Logic | All of naming, sanitization, numbering, conflict handling, metadata, rename logic lives in `Core` and is unit-tested; UI project stays thin | PASS |

No violations — Complexity Tracking table is not needed.

## Project Structure

### Documentation (this feature)

```text
specs/001-dev-test-evidence-capture/
├── plan.md              # This file (/speckit-plan command output)
├── research.md          # Phase 0 output (/speckit-plan command)
├── data-model.md         # Phase 1 output (/speckit-plan command)
├── quickstart.md        # Phase 1 output (/speckit-plan command)
├── contracts/           # Phase 1 output (/speckit-plan command)
└── tasks.md             # Phase 2 output (/speckit-tasks command - NOT created by /speckit-plan)
```

### Source Code (repository root)

```text
src/
├── DevTestEvidenceCapture.App/            # WPF UI + composition root (net8.0-windows)
│   ├── App.xaml / App.xaml.cs
│   ├── Views/
│   │   ├── MainWindow.xaml               # Ticket/scenario inputs, root folder, Start/Stop, status
│   │   └── RenameWindow.xaml             # Post-session screenshot naming screen
│   └── ViewModels/
│       ├── MainViewModel.cs
│       └── RenameViewModel.cs
│
└── DevTestEvidenceCapture.Core/           # Plain class library, no WPF/UI refs (net8.0-windows)
    ├── Session/                           # ISessionManager, CaptureSession, session lifecycle
    ├── Capture/                           # IScreenCaptureService (System.Drawing.Common + Win32 metrics)
    ├── Hotkey/                            # IGlobalHotkeyService (RegisterHotKey/UnregisterHotKey P/Invoke)
    ├── Storage/                           # IEvidenceStorage (folder creation, resume/interrupt detection)
    ├── Naming/                            # Filename sanitization, numbering, rename + conflict resolution
    ├── Metadata/                          # scenario.json read/write (IScenarioMetadataService)
    └── Configuration/                     # IAppConfigurationService (root folder, shortcut, last session)

tests/
└── DevTestEvidenceCapture.Core.Tests/     # xUnit: Naming, Storage, Metadata, Session logic
```

**Structure Decision**: Single-solution, two-runtime-project layout (`App` + `Core`) plus one test
project, matching Option 1 (single project family) from the template but split in two so the WPF UI
project has zero business logic and the `Core` library has zero WPF references — this is what makes
Principle IV (Separation of Concerns) and Principle IX (testability) achievable without extra
infrastructure. A third project ("web"/"mobile" options) is not applicable to a Windows desktop tool.

## Complexity Tracking

*No Constitution Check violations — table intentionally left empty.*
