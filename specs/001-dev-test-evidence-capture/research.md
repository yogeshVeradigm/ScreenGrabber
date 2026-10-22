# Phase 0 Research: Dev Test Evidence Capture

No `[NEEDS CLARIFICATION]` markers remained in the Technical Context — the spec's clarifications
session already resolved the scope-level ambiguities (resume behavior, folder structure, capture
failure handling). The research below covers the technical decisions needed to implement that scope
within the constitution's constraints.

## 1. Full-screen capture mechanism

- **Decision**: Use `System.Drawing.Common`'s `Bitmap` + `Graphics.CopyFromScreen`, sized to the
  primary monitor's bounds obtained via `user32.dll`'s `GetSystemMetrics(SM_CXSCREEN)` /
  `GetSystemMetrics(SM_CYSCREEN)` (primary monitor is always anchored at virtual-desktop origin
  `(0,0)`), then `Bitmap.Save(path, ImageFormat.Png)`.
- **Rationale**: `System.Drawing.Common` is a Microsoft-maintained package that runs fine on
  `net8.0-windows`, needs no COM/WinRT activation dance, and is the simplest path to a PNG file.
  Reading monitor bounds via `GetSystemMetrics` avoids pulling in `System.Windows.Forms` just for
  `Screen.PrimaryScreen`, keeping the dependency surface to exactly one imaging package plus Win32
  P/Invoke (both already justified by Principle I).
- **Alternatives considered**:
  - `Windows.Graphics.Capture` (WinRT): more "modern" and GPU-backed, but requires a package
    reference to the Windows App SDK / WinRT projection, minimum-version gating, and an async
    capture-session lifecycle — disproportionate complexity for "grab the primary monitor and save a
    PNG."
  - Raw `BitBlt`/`PrintWindow` via `gdi32.dll` only (no `System.Drawing.Common`): avoids the one
    extra package, but then PNG encoding has to be hand-rolled or done through WIC interop, which is
    more code and more risk for no real benefit over the built-in `Bitmap.Save`.

## 2. Global hotkey registration

- **Decision**: P/Invoke `RegisterHotKey`/`UnregisterHotKey` (`user32.dll`) against the main window's
  `HWND`, obtained from WPF's `HwndSource` once the window is initialized (`SourceInitialized`
  event). Add an `HwndSource.AddHook` message hook that watches for `WM_HOTKEY` (`0x0312`) and raises
  a capture request. Unregister on window close.
- **Rationale**: `RegisterHotKey` is a per-thread, OS-level registration — it keeps firing
  `WM_HOTKEY` messages to the owning window's message queue regardless of whether that window is
  minimized, hidden, or not focused, which is exactly Principle VI's requirement and needs no extra
  background service, tray process, or low-level keyboard hook (`WH_KEYBOARD_LL`), which would be far
  more invasive (sees every keystroke system-wide) for the same result.
- **Alternatives considered**:
  - Low-level keyboard hook (`SetWindowsHookEx(WH_KEYBOARD_LL, ...)`): works globally too, but
    intercepts *all* keystrokes system-wide (privacy/AV-flagging concerns, more code to filter for
    just one combo) — unnecessary given `RegisterHotKey` already satisfies the requirement.
  - Third-party global-hotkey NuGet packages: rejected outright by Principle I (BCL/Win32 over
    third-party when the Win32 call is this small and well-documented).

## 3. Metadata and configuration serialization

- **Decision**: `System.Text.Json` (BCL) for both `scenario.json` (per session) and
  `config.json` (app-wide, under `%LOCALAPPDATA%\DevTestEvidenceCapture\`), using simple POCOs and
  `JsonSerializerOptions { WriteIndented = true }` for human-readable output.
- **Rationale**: Zero extra dependencies, indented output is legible if a developer opens the file
  manually, and it is what a future Claude Code skill would also parse without needing any special
  library.
- **Alternatives considered**: `Newtonsoft.Json` — no capability gap justifies the extra dependency
  over the BCL's own JSON stack (rejected by Principle I).

## 4. Resume/interrupted-session detection

- **Decision**: The app config (`config.json`) tracks a single `lastActiveSessionPath` (nullable),
  written when a session starts and cleared when it is explicitly stopped. On launch, if that path is
  set and the `scenario.json` at that path has no `sessionEnd` timestamp, the app offers to resume it.
  The next screenshot's sequence number is always computed by scanning the session folder's existing
  numeric-prefixed files and taking `max + 1` — never trusted from in-memory state alone — so a resumed
  session numbers correctly even though the process restarted.
- **Rationale**: A pointer in local config is O(1) to check on launch; scanning the whole configured
  root folder recursively for incomplete sessions would be slower and unnecessary since Phase 1 only
  ever has at most one session active at a time (FR-011).
- **Alternatives considered**: Recursive scan of the root evidence folder for any `scenario.json`
  missing `sessionEnd` — rejected as unnecessary I/O given only one session can ever be interrupted at
  a time.

## 5. Filename/foldername sanitization rules

- **Decision**: Replace each of the reserved Windows characters (`< > : " / \ | ? *`) and ASCII
  control characters (0–31) with `_`; if the sanitized name case-insensitively matches a reserved
  device name (`CON`, `PRN`, `AUX`, `NUL`, `COM1`–`COM9`, `LPT1`–`LPT9`), append a trailing `_`; strip
  trailing dots/spaces (Windows silently drops them, which can otherwise cause hidden collisions).
  This is one pure function shared by ticket number, scenario name, and meaningful-name sanitization.
- **Rationale**: Deterministic, total (always produces a valid name), and testable in isolation —
  directly satisfies Principle V and the FR-018 requirement.
- **Alternatives considered**: Rejecting invalid input outright and forcing the user to retype it —
  rejected because the spec (FR-018) calls for safe substitution, not rejection, to keep the capture
  flow friction-free.

## 6. Packaging for distribution

- **Decision**: `dotnet publish src/DevTestEvidenceCapture.App -c Release -r win-x64 --self-contained
  true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true` produces one
  distributable `.exe` with no separate .NET runtime install required on the target machine.
- **Rationale**: Matches the "lightweight, easy to publish as a standalone Windows executable"
  constraint with a built-in `dotnet publish` flag, no extra tooling.
- **Alternatives considered**: Framework-dependent deployment (smaller executable, but requires the
  target machine to have the matching .NET 8 desktop runtime pre-installed) — rejected since this is
  handed to developer machines that may not have it, and self-contained single-file removes that
  friction entirely.

## Outstanding items

None — all Technical Context fields are resolved; no `NEEDS CLARIFICATION` markers remain.
