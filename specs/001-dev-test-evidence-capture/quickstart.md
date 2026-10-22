# Quickstart: Dev Test Evidence Capture

Validation guide for the Phase 1 build. Assumes the solution described in `plan.md`'s Project
Structure exists (`src/DevTestEvidenceCapture.App`, `src/DevTestEvidenceCapture.Core`,
`tests/DevTestEvidenceCapture.Core.Tests`).

## Prerequisites

- Windows 10/11
- .NET 8 SDK installed
- Repository checked out at this feature branch (`001-dev-test-evidence-capture`)

## Build and test

```powershell
dotnet build
dotnet test tests/DevTestEvidenceCapture.Core.Tests
```

Expected: build succeeds; all unit tests pass, covering the deterministic logic behind
`INamingService`, `IEvidenceStorage`, and `IScenarioMetadataService` (see
`contracts/core-service-interfaces.md`).

## Run the app

```powershell
dotnet run --project src/DevTestEvidenceCapture.App
```

## Scenario A — Core capture flow (User Story 1)

1. Enter a ticket number (e.g. `TICKET-123`) and scenario name (e.g. `Login flow`).
2. Configure a root evidence folder (any writable local folder).
3. Click **Start Session**.
   - **Expect**: `<root>\TICKET-123\Login flow\` is created; UI shows an active-session indicator.
4. Minimize the app; open Notepad (or any other app) so it has focus.
5. Press the configured capture shortcut 3 times (default shown in the UI, e.g.
   `Ctrl+Shift+S`).
   - **Expect**: `01.png`, `02.png`, `03.png` appear in the session folder; the app's screenshot
     count updates each time even while minimized; each capture also plays a short confirmation
     sound and a brief screen-wide visual flash, perceivable without switching back to the app
     (FR-025b).
6. Click **Stop Session**.
   - **Expect**: active-session indicator clears; the renaming screen opens automatically.

## Scenario B — Renaming (User Story 2)

1. On the renaming screen opened in Scenario A, confirm each row shows a small thumbnail of its
   screenshot (FR-013a) — you should be able to tell which row is which without reading the
   filename.
2. Click a thumbnail.
   - **Expect**: an in-app popup opens showing the full-size image; clicking again or pressing
     Escape closes it without disturbing the renaming screen underneath (FR-013b).
3. Type `Login page` for `01.png`, leave `02.png` blank, and type `Login page` again for `03.png`
   (a deliberate conflict with the first entry — the two screenshots would otherwise end up with
   the same descriptive name).
   - **Expect**: confirming is blocked and the two conflicting rows are flagged, per FR-017.
4. Change `03.png`'s name to `Search results`, then confirm.
   - **Expect**: files on disk are now `01_Login_page.png`, `02.png`, `03_Search_results.png`; the
     numeric prefixes are unchanged.

## Scenario C — Metadata as source of truth (User Story 3)

1. Open `scenario.json` in the session folder from Scenario B.
   - **Expect**: it validates against `contracts/scenario-metadata.schema.json`; `screenshotCount` is
     `3`; `screenshots[].fileName` matches the renamed files exactly (`01_Login_page.png`, `02.png`,
     `03_Search_results.png`); `sessionEnd` is populated.

## Scenario D — Interrupted-session resume

1. Start a new session, capture 2 screenshots, then kill the app process (Task Manager → End Task) —
   do **not** click Stop Session.
2. Relaunch the app.
   - **Expect**: the app detects the interrupted session and offers to resume it.
3. Resume, capture one more screenshot.
   - **Expect**: the new file is `03.png` (numbering continued correctly from disk, not restarted at
     `01.png`).
4. Stop the session and confirm renaming (or skip) as in Scenario B.

## Scenario E — Safety edge cases

- Re-run Scenario A with the same ticket number and scenario name a second time.
  - **Expect**: a new, distinguishable scenario subfolder is created (FR-004) — the first session's
    files are untouched.
- Enter a scenario name containing `:` or `?` (e.g. `Search: results?`).
  - **Expect**: the created folder name has those characters safely substituted (FR-018); no error.
- On the renaming screen, delete or rename one of the screenshot files on disk before the thumbnail
  finishes loading (or point a row at a corrupted image).
  - **Expect**: that row shows a placeholder instead of a thumbnail; the rest of the renaming step
    (including that row's meaningful-name field) still works normally.

Passing all five scenarios manually is the Phase 1 acceptance bar until UI automation is introduced
(out of scope for Phase 1 per the constitution's Principle IX UI exemption).
