# Dev Test Evidence Capture

A Windows desktop utility for developers doing manual test passes: tag a session with a ticket
number and scenario name, capture an ordered sequence of screenshots via a global keyboard shortcut
while working in any other application, and optionally give the screenshots meaningful names when
you're done. Everything is stored locally — see `specs/001-dev-test-evidence-capture/` for the full
spec, plan, and design docs.

## Requirements

- Windows 10/11
- .NET 8 SDK (for building; the published app is self-contained and needs no separate runtime)

## Build

```powershell
dotnet build
```

## Test

```powershell
dotnet test
```

Runs the xUnit suite in `tests/DevTestEvidenceCapture.Core.Tests` against the `Core` class library
(naming/sanitization, folder/sequence handling, configuration, metadata, session orchestration,
rename conflict handling, and integration tests that validate `scenario.json` against
`specs/001-dev-test-evidence-capture/contracts/scenario-metadata.schema.json`).

## Run (development)

```powershell
dotnet run --project src/DevTestEvidenceCapture.App
```

## Publish (standalone executable)

```powershell
dotnet publish src/DevTestEvidenceCapture.App -p:PublishProfile=win-x64-selfcontained
```

Produces a single self-contained `DevTestEvidenceCapture.App.exe` under
`src/DevTestEvidenceCapture.App/bin/Release/net8.0-windows/win-x64/publish/` that runs on a Windows
machine without a separate .NET install.

## Usage

1. Launch the app. Enter a **Ticket / Task** (or pick one of your 10 most recent from the dropdown)
   and **Scenario Name**, and configure a **Root Folder** (remembered for next time). The ticket can
   be any identifier — a JIRA key (`CAMP-253`), an Azure DevOps work item (`AB#1234`), a ServiceNow
   number, or just a task name (`Release 26.1 smoke test`). Characters Windows doesn't allow in
   folder names are replaced with `_` for the folder; `scenario.json` keeps what you typed.
2. Click **Start Session**. You can now minimize the app and work in any other application.
3. Press the capture shortcut shown in the app (default `Ctrl+Shift+S`). The primary monitor freezes
   under a dimmed overlay: drag a rectangle to choose an area, press **Enter** to save the whole
   screen, or press **Esc** / right-click to cancel without saving. Once an area is chosen, a toolbar
   lets you mark it up before saving:
   - **Highlighter**: translucent yellow freehand brush.
   - **Rectangle** / **Ellipse**: hold **Shift** for a square/circle; choose **Filled** or border-only
     and a colour (red by default).
   - **Pixelate**: drag over sensitive data (e.g. patient details) to replace it with coarse blocks.
     Larger block sizes hide more; for anything that must never be recoverable, a **Filled**
     rectangle is the safest choice.
   - **Size** sets the highlighter brush size, the shape border thickness or the pixelate block size.
   - **Note**: optional text describing what you did or saw; saved with the screenshot in
     `scenario.json`.
   - **Undo** (`Ctrl+Z`), **Cancel** (`Esc`), **Save** (`Enter`). **Select area** lets you redraw the area.

   Brush size, border thickness, pixelate block size, fill and colour are remembered between captures
   and launches. Screenshots are numbered in capture order (`01.png`, `02.png`, ...). With **Copy each
   screenshot to the clipboard** ticked (the default), each saved screenshot is also ready to paste
   into Teams, JIRA or a PR comment.

   To use a different capture shortcut, click **Change...** next to it and press the new combination.
4. Click **Stop Session**. If any screenshots were captured, a naming screen opens where you can give
   each one a meaningful name (the numeric prefix is always kept), leave any blank, or skip renaming
   entirely.
5. Each session folder (`<root>\<ticket>\<scenario>\`) contains the screenshots plus a `scenario.json`
   with the ticket/scenario, timestamps, final filenames and any notes — see the schema linked above.

If the app is closed unexpectedly while a session is active, relaunching it offers to resume that
session, continuing capture into the same folder with numbering picked up correctly from disk. If you
decline, it won't ask about that session again. `scenario.json`, `config.json` and screenshots are
written via a temporary file and rename, so a crash mid-write can't leave a truncated file behind.
