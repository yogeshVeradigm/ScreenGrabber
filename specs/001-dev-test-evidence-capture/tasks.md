---

description: "Task list template for feature implementation"
---

# Tasks: Dev Test Evidence Capture

**Input**: Design documents from `/specs/001-dev-test-evidence-capture/`

**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/, quickstart.md (all present)

**Tests**: Included — the constitution (Principle IX) and the plan's technical constraints both call
for automated unit tests on deterministic Core logic (naming, numbering, sanitization, metadata,
rename, conflict handling). UI code and thin Win32 interop wrappers are exempt per the constitution.

**Organization**: Tasks are grouped by user story (from spec.md) to enable independent implementation
and testing of each story.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependency on another incomplete task)
- **[Story]**: US1/US2/US3, mapping to spec.md's prioritized user stories
- Every task includes exact file path(s)

## Path Conventions

Per `plan.md`'s Project Structure:
- `src/DevTestEvidenceCapture.App/` — WPF UI + composition root
- `src/DevTestEvidenceCapture.Core/` — plain class library (no WPF references)
- `tests/DevTestEvidenceCapture.Core.Tests/` — xUnit tests against Core

---

## Phase 1: Setup

**Purpose**: Solution/project scaffolding

- [X] T001 Create `DevTestEvidenceCapture.sln` and three projects — `src/DevTestEvidenceCapture.App` (WPF), `src/DevTestEvidenceCapture.Core` (class library), `tests/DevTestEvidenceCapture.Core.Tests` (xUnit) — and wire project references (`App`→`Core`, `Core.Tests`→`Core`) per plan.md's Project Structure
- [X] T002 Configure all three `.csproj` files: `TargetFramework` = `net8.0-windows`, enable nullable reference types and implicit usings, add `System.Drawing.Common` package reference to `DevTestEvidenceCapture.Core.csproj`, add `xunit`/`xunit.runner.visualstudio`/`Microsoft.NET.Test.Sdk` to `DevTestEvidenceCapture.Core.Tests.csproj`
- [X] T003 [P] Add root `.gitignore` for .NET (`bin/`, `obj/`, `.vs/`) at repository root

**Checkpoint**: Solution builds empty; ready for foundational Core code.

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Domain models, service interfaces, and the deterministic logic (naming, storage,
configuration, metadata) every user story depends on.

**⚠️ CRITICAL**: No user story work can begin until this phase is complete.

- [X] T004 [P] Create `CaptureSession` and `Screenshot` domain models (per data-model.md) in `src/DevTestEvidenceCapture.Core/Session/CaptureSession.cs` and `src/DevTestEvidenceCapture.Core/Session/Screenshot.cs`
- [X] T005 [P] Create `AppConfiguration` model (per data-model.md) in `src/DevTestEvidenceCapture.Core/Configuration/AppConfiguration.cs`
- [X] T006 Define the seven Core service interfaces per `contracts/core-service-interfaces.md`: `IAppConfigurationService` in `src/DevTestEvidenceCapture.Core/Configuration/IAppConfigurationService.cs`, `IEvidenceStorage` in `src/DevTestEvidenceCapture.Core/Storage/IEvidenceStorage.cs` (note `TryFindInterruptedSessionAsync` is async — see contract), `INamingService` in `src/DevTestEvidenceCapture.Core/Naming/INamingService.cs`, `IScreenCaptureService` in `src/DevTestEvidenceCapture.Core/Capture/IScreenCaptureService.cs`, `IGlobalHotkeyService` in `src/DevTestEvidenceCapture.Core/Hotkey/IGlobalHotkeyService.cs`, `IScenarioMetadataService` in `src/DevTestEvidenceCapture.Core/Metadata/IScenarioMetadataService.cs`, `ISessionManager` in `src/DevTestEvidenceCapture.Core/Session/ISessionManager.cs` (depends on T004, T005)
- [X] T007 [P] Implement `NamingService.SanitizeForFileSystem` and `NamingService.FormatScreenshotFileName` (Windows-invalid-char/reserved-device-name/trailing-dot-space handling; zero-padded numeric filenames, per research.md §5 and §1) in `src/DevTestEvidenceCapture.Core/Naming/NamingService.cs` (depends on T006)
- [X] T008 [P] Implement `AppConfigurationService` (load/save `config.json` under `%LOCALAPPDATA%\DevTestEvidenceCapture\`, with defaults: no root folder configured, default shortcut `"Ctrl+Shift+S"`) in `src/DevTestEvidenceCapture.Core/Configuration/AppConfigurationService.cs` (depends on T005, T006)
- [X] T009 [P] Implement `ScenarioMetadataService.WriteAsync`/`ReadAsync` for `scenario.json` per `contracts/scenario-metadata.schema.json` in `src/DevTestEvidenceCapture.Core/Metadata/ScenarioMetadataService.cs` (depends on T004, T006)
- [X] T010 Implement `EvidenceStorage.CreateSessionFolder` (nested `root\<ticket>\<scenario>\` creation with FR-004 collision disambiguation) and `EvidenceStorage.GetNextSequenceNumber` (scan existing numeric-prefixed files, return `max + 1`) in `src/DevTestEvidenceCapture.Core/Storage/EvidenceStorage.cs` (depends on T004, T006, T007)
- [X] T011 [P] Unit tests for `NamingService` sanitization (invalid chars, `CON`/`PRN`/`AUX`/`NUL`/`COM1-9`/`LPT1-9`, trailing dots/spaces) and numbering (zero-padding, width expansion past 99) in `tests/DevTestEvidenceCapture.Core.Tests/Naming/NamingServiceTests.cs` (depends on T007)
- [X] T012 [P] Unit tests for `AppConfigurationService` defaults and load/save round-trip in `tests/DevTestEvidenceCapture.Core.Tests/Configuration/AppConfigurationServiceTests.cs` (depends on T008)
- [X] T013 [P] Unit tests for `ScenarioMetadataService` write/read round-trip and JSON field mapping in `tests/DevTestEvidenceCapture.Core.Tests/Metadata/ScenarioMetadataServiceTests.cs` (depends on T009)
- [X] T014 [P] Unit tests for `EvidenceStorage` folder creation/disambiguation and sequence-number scanning (including mixed 2-digit/3-digit filenames) in `tests/DevTestEvidenceCapture.Core.Tests/Storage/EvidenceStorageTests.cs` (depends on T010)

**Checkpoint**: Foundation ready — models, interfaces, and Naming/Storage/Configuration/Metadata
implementations exist and are unit-tested. User story implementation can now begin.

---

## Phase 3: User Story 1 - Capture a full set of screenshots without losing any evidence (Priority: P1) 🎯 MVP

**Goal**: Reliable, globally-triggered, ordered screenshot capture tied to a ticket/scenario, that
keeps working while minimized/unfocused and survives an unexpected app close via resume.

**Independent Test**: Enter a ticket/scenario, start a session, minimize the app, switch focus to
another application, press the global capture shortcut several times, stop the session, and verify a
folder exists with the correct number of sequentially numbered screenshots in capture order.

### Tests for User Story 1

> **NOTE**: Write these tests first; they will fail to compile/pass until the implementation tasks
> below land.

- [X] T015 [P] [US1] Unit tests for `SessionManager` lifecycle using fake `IEvidenceStorage`/`IScreenCaptureService`/`IScenarioMetadataService`: no-op capture with no active session (FR-008), repeated captures increment sequence (FR-009), starting a second session while one is active is rejected (FR-011) — `tests/DevTestEvidenceCapture.Core.Tests/Session/SessionManagerTests.cs`
- [X] T016 [P] [US1] Unit tests for `SessionManager.CaptureAsync` with a failing `IScreenCaptureService` (FR-025a): the session remains `Active`, a non-blocking warning/failure signal is raised, and a subsequent successful capture still succeeds — `tests/DevTestEvidenceCapture.Core.Tests/Session/SessionManagerCaptureFailureTests.cs`
- [X] T017 [P] [US1] Unit tests for `EvidenceStorage.TryFindInterruptedSessionAsync` covering an interrupted session (no `sessionEnd`), a completed session, and no prior session at all, using a fake `IScenarioMetadataService` to control the `scenario.json` content read back — `tests/DevTestEvidenceCapture.Core.Tests/Storage/EvidenceStorageResumeTests.cs`

### Implementation for User Story 1

- [X] T018 [US1] Implement `EvidenceStorage.TryFindInterruptedSessionAsync` (reads `AppConfiguration.lastActiveSessionPath`, awaits `IScenarioMetadataService.ReadAsync` for that session's `scenario.json`, and checks for a null `sessionEnd`, per research.md §4) in `src/DevTestEvidenceCapture.Core/Storage/EvidenceStorage.cs` (depends on T008, T009, T010; satisfies T017)
- [X] T019 [P] [US1] Implement `ScreenCaptureService.CapturePrimaryMonitorPngAsync` (`GetSystemMetrics(SM_CXSCREEN/SM_CYSCREEN)` + `Bitmap`/`Graphics.CopyFromScreen` + PNG encode, per research.md §1) in `src/DevTestEvidenceCapture.Core/Capture/ScreenCaptureService.cs` (depends on T006)
- [X] T020 [P] [US1] Implement `GlobalHotkeyService` (`RegisterHotKey`/`UnregisterHotKey` P/Invoke + `WM_HOTKEY` `HwndSource` message hook, per research.md §2) in `src/DevTestEvidenceCapture.Core/Hotkey/GlobalHotkeyService.cs` (depends on T006)
- [X] T021 [US1] Implement `SessionManager.StartAsync`/`CaptureAsync`/`StopAsync` (orchestrates storage + capture + metadata write-on-start/each-capture/stop; on a capture failure keeps the session active, raises a non-blocking warning, and keeps retrying subsequent captures per FR-025a) in `src/DevTestEvidenceCapture.Core/Session/SessionManager.cs` (depends on T009, T010, T018, T019, T020; satisfies T015, T016)
- [X] T022 [US1] Implement `MainWindow` UI and `MainViewModel` (ticket number input, scenario name input, root folder configuration, Start/Stop Session buttons, active-session indicator, screenshot count, configured-shortcut display, and an interrupted-session resume prompt on launch that awaits `TryFindInterruptedSessionAsync`) in `src/DevTestEvidenceCapture.App/Views/MainWindow.xaml`, `src/DevTestEvidenceCapture.App/Views/MainWindow.xaml.cs`, `src/DevTestEvidenceCapture.App/ViewModels/MainViewModel.cs` (depends on T021)
- [X] T023 [US1] Wire the composition root in `src/DevTestEvidenceCapture.App/App.xaml.cs` (construct concrete `Core` services, register the global hotkey once the main window's `HwndSource` is available via `SourceInitialized`, unregister on window close) (depends on T022)
- [X] T038 [US1] Implement `CaptureFeedbackPresenter` (FR-025b): `PlayConfirmationSound()` using `System.Media.SystemSounds` (no new dependency), and `ShowFlash()` which briefly displays a topmost, borderless, transparent overlay window sized to `SystemParameters.PrimaryScreenWidth`/`PrimaryScreenHeight` and auto-closes after ~150ms via a `DispatcherTimer` — perceivable regardless of whether `MainWindow` is minimized or focused — in `src/DevTestEvidenceCapture.App/Feedback/CaptureFeedbackPresenter.cs`
- [X] T039 [US1] Wire `MainViewModel.OnHotkeyPressedAsync` to call `CaptureFeedbackPresenter.PlayConfirmationSound()` and `ShowFlash()` only when `CaptureAsync` returns a non-null `Screenshot` (never on a failed capture — FR-025a's warning indicator remains the failure signal) in `src/DevTestEvidenceCapture.App/ViewModels/MainViewModel.cs` (depends on T038)

**Checkpoint**: User Story 1 is fully functional and independently testable — run quickstart.md
Scenarios A and D.

---

## Phase 4: User Story 2 - Give screenshots meaningful names while keeping capture order intact (Priority: P2)

**Goal**: Optional post-session renaming that preserves the numeric prefix, allows blanks, allows
skipping entirely, blocks on naming conflicts instead of silently resolving them, and stays resilient
if a rename fails partway through.

**Independent Test**: Complete a session with several screenshots, open the renaming screen, assign
names to some, leave others blank, attempt a conflicting pair of names, resolve it, and confirm — then
verify the resulting filenames.

### Tests for User Story 2

- [X] T024 [P] [US2] Unit tests for `NamingService.ApplyRenames`: prefix is preserved, a blank entry leaves the original filename unchanged, two screenshots given the same meaningful name blocks the whole batch with zero files touched (FR-016, FR-017, FR-021 — note: since the numeric prefix is always preserved and unique, conflicts are detected on the meaningful name itself, not the full final filename, which can never collide between two different screenshots), and a meaningful name containing Windows-invalid characters is sanitized before use (FR-018) — `tests/DevTestEvidenceCapture.Core.Tests/Naming/NamingServiceRenameTests.cs`
- [X] T025 [P] [US2] Unit tests for `NamingService.ApplyRenames` mid-batch I/O failure (FR-021): simulate an exception renaming one file after others in the same batch already succeeded, and assert already-renamed files keep their new names, not-yet-renamed files keep their numeric names, and no screenshot is lost or corrupted — `tests/DevTestEvidenceCapture.Core.Tests/Naming/NamingServiceRenamePartialFailureTests.cs`
- [X] T026 [P] [US2] Unit tests for `SessionManager.RenameAsync`/`SkipRenameAsync` verifying `scenario.json` is updated to the final filenames after renaming, or left as the original numeric filenames after skipping (FR-024) — `tests/DevTestEvidenceCapture.Core.Tests/Session/SessionManagerRenameTests.cs`

### Implementation for User Story 2

- [X] T027 [US2] Implement `NamingService.ApplyRenames` (sanitize each provided meaningful name via `SanitizeForFileSystem` before combining it with its numeric prefix — FR-018 — then run an up-front conflict check across all resulting final names; on conflict, no file is renamed) in `src/DevTestEvidenceCapture.Core/Naming/NamingService.cs` (depends on T007; satisfies T024, T025)
- [X] T028 [US2] Implement `SessionManager.RenameAsync`/`SkipRenameAsync` (delegates to `NamingService.ApplyRenames`, then calls `ScenarioMetadataService.WriteAsync` to persist final filenames) in `src/DevTestEvidenceCapture.Core/Session/SessionManager.cs` (depends on T021, T027, T009; satisfies T026)
- [X] T029 [US2] Implement `RenameWindow` UI and `RenameViewModel` (lists screenshots in original capture order with current filename and an editable meaningful-name field per row, inline conflict flags that block confirmation, and a single "Skip" action) in `src/DevTestEvidenceCapture.App/Views/RenameWindow.xaml`, `src/DevTestEvidenceCapture.App/Views/RenameWindow.xaml.cs`, `src/DevTestEvidenceCapture.App/ViewModels/RenameViewModel.cs` (depends on T028)
- [X] T030 [US2] Wire `MainViewModel` to open `RenameWindow` automatically, immediately after Stop Session (FR-012), in `src/DevTestEvidenceCapture.App/ViewModels/MainViewModel.cs` (depends on T022, T029)
- [X] T040 [US2] Add thumbnail support to `ScreenshotRenameRow` (FR-013a): expose the screenshot's full file path and a small decoded `BitmapImage` thumbnail (`DecodePixelWidth` ~64px, decoded lazily); if the file is missing, unreadable, or corrupted, expose a null/placeholder thumbnail instead of throwing (per the missing/corrupted-thumbnail edge case) — `src/DevTestEvidenceCapture.App/ViewModels/ScreenshotRenameRow.cs`
- [X] T041 [US2] Add a thumbnail column to `RenameWindow.xaml` (showing a placeholder graphic when a row's thumbnail is null) and implement `ImagePreviewWindow` (FR-013b): clicking a thumbnail opens it showing the full-resolution image, closing on a subsequent click or the Escape key, without disturbing the in-progress renaming step — `src/DevTestEvidenceCapture.App/Views/RenameWindow.xaml`, `src/DevTestEvidenceCapture.App/Views/RenameWindow.xaml.cs`, `src/DevTestEvidenceCapture.App/Views/ImagePreviewWindow.xaml`, `src/DevTestEvidenceCapture.App/Views/ImagePreviewWindow.xaml.cs` (depends on T040)

**Checkpoint**: User Stories 1 and 2 both work independently — run quickstart.md Scenario B.

---

## Phase 5: User Story 3 - Trust the recorded metadata as the source of truth (Priority: P3)

**Goal**: `scenario.json` is always schema-conformant and matches disk state, whether or not renaming
happened, so a future tool can rely on it without opening the app.

**Independent Test**: Complete a session (with and without renaming) and verify the resulting
`scenario.json` against `contracts/scenario-metadata.schema.json`.

### Implementation for User Story 3

- [X] T031 [US3] Add a JSON Schema validation helper that loads `contracts/scenario-metadata.schema.json` and validates a `scenario.json` instance against it, for use by T032/T033 — `tests/DevTestEvidenceCapture.Core.Tests/Integration/SchemaValidationHelper.cs` (depends on T021, T028)

### Tests for User Story 3

- [X] T032 [P] [US3] Integration test: run a full session (start → 2 captures → stop, no renaming) and assert the resulting `scenario.json` validates against the schema helper from T031 and matches the files on disk — `tests/DevTestEvidenceCapture.Core.Tests/Integration/ScenarioMetadataIntegrationTests.cs`
- [X] T033 [P] [US3] Integration test: run a full session with renaming applied and assert `scenario.json`'s `screenshots[].fileName` values reflect the final (post-rename) names, not the originals — `tests/DevTestEvidenceCapture.Core.Tests/Integration/ScenarioMetadataRenameIntegrationTests.cs`

**Checkpoint**: All three user stories are independently functional — run quickstart.md Scenario C.

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: Remaining edge cases, packaging, and end-to-end validation.

- [X] T034 [P] Unit tests for edge cases: stopping a session with zero screenshots captured (empty `scenario.json`, no rename step offered) and Start Session remaining unavailable when the ticket number, scenario name, or the configured root folder is missing/invalid (FR-029) — `tests/DevTestEvidenceCapture.Core.Tests/Session/SessionManagerEdgeCaseTests.cs`
- [X] T035 [P] Add a win-x64 self-contained single-file publish profile per research.md §6 — `src/DevTestEvidenceCapture.App/Properties/PublishProfiles/win-x64-selfcontained.pubxml`
- [X] T036 Run full `quickstart.md` validation (Scenarios A–E) manually against the built app and record pass/fail results
- [X] T037 [P] Write root `README.md` with build, test, run, and publish instructions
- [X] T042 [P] Update `quickstart.md` Scenario A with a step validating the capture confirmation sound/flash (FR-025b), and Scenario B with steps validating thumbnails and the in-app preview popup (FR-013a, FR-013b) — `specs/001-dev-test-evidence-capture/quickstart.md`
- [X] T043 Manually re-run the updated `quickstart.md` Scenarios A and B against the built app (capture feedback perceivable while minimized; thumbnails identify each row; preview popup opens/closes correctly; missing/corrupted screenshot file shows a placeholder) and record pass/fail results (depends on T039, T041, T042)

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies — start immediately
- **Foundational (Phase 2)**: Depends on Setup — BLOCKS all user stories
- **User Story 1 (Phase 3)**: Depends on Foundational only
- **User Story 2 (Phase 4)**: Depends on Foundational; its implementation also extends `SessionManager`/`MainViewModel` from User Story 1 (T021, T022), so build it after US1 completes even though its own acceptance test is independent
- **User Story 3 (Phase 5)**: Depends on Foundational; its integration tests exercise the `SessionManager` flows built in US1 (T021) and US2 (T028), so build it last
- **Polish (Phase 6)**: Depends on all three user stories being complete

### Within Each Phase

- Tests are listed before the implementation tasks they exercise and should fail until that
  implementation lands
- Models/interfaces before services; services before UI
- A phase's checkpoint must pass before starting the next phase's UI-integration tasks

### Parallel Opportunities

- Setup: T003 can run alongside T002
- Foundational: T004 and T005 in parallel; then T007, T008, T009 in parallel (all depend only on
  T004–T006); T010 waits on T007; finally T011–T014 (the four test files) in parallel
- User Story 1: T015, T016, and T017 in parallel; then T019 and T020 in parallel (independent of
  each other and of T018); T038 has no file overlap with T019/T020 and can run alongside them
- User Story 2: T024, T025, and T026 in parallel; T040 has no file overlap with T027/T028 and can
  start as soon as Foundational is done
- User Story 3: T032 and T033 in parallel (once T031 exists)
- Polish: T034, T035, T037, T042 in parallel

---

## Parallel Example: Foundational Phase

```bash
# After T006 (interfaces) completes, these three are independent:
Task: "Implement NamingService.SanitizeForFileSystem and FormatScreenshotFileName in src/DevTestEvidenceCapture.Core/Naming/NamingService.cs"
Task: "Implement AppConfigurationService in src/DevTestEvidenceCapture.Core/Configuration/AppConfigurationService.cs"
Task: "Implement ScenarioMetadataService.WriteAsync/ReadAsync in src/DevTestEvidenceCapture.Core/Metadata/ScenarioMetadataService.cs"
```

## Parallel Example: User Story 1

```bash
Task: "Implement ScreenCaptureService.CapturePrimaryMonitorPngAsync in src/DevTestEvidenceCapture.Core/Capture/ScreenCaptureService.cs"
Task: "Implement GlobalHotkeyService in src/DevTestEvidenceCapture.Core/Hotkey/GlobalHotkeyService.cs"
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1: Setup
2. Complete Phase 2: Foundational (blocks everything)
3. Complete Phase 3: User Story 1
4. **STOP and VALIDATE**: run quickstart.md Scenarios A and D independently
5. This alone is a usable MVP — ordered, crash-resilient, globally-triggered evidence capture

### Incremental Delivery

1. Setup + Foundational → foundation ready
2. + User Story 1 → test independently → MVP
3. + User Story 2 → test independently → adds meaningful names
4. + User Story 3 → test independently → proves the metadata contract for future tooling
5. + Polish → packaging and full quickstart pass

### Parallel Team Strategy

Once Foundational is done, User Story 2's `NamingService.ApplyRenames` (T027) and User Story 3's
schema helper (T031, once T021/T028 exist) have no file overlap with User Story 1's UI tasks (T022,
T023), so a second developer could start US2's Core-level rename logic while the first finishes US1's
UI — but the two stories' UI wiring (T030 depends on T022/T029) and US3's integration tests (depend on
T021/T028) mean full independent test passes still land in priority order.

---

## Notes

- [P] tasks touch different files and have no dependency on another incomplete task
- [Story] labels (US1/US2/US3) map every user-story-phase task back to spec.md for traceability
- Commit after each task or logical group
- Stop at any checkpoint to validate a story independently before moving on
- UI (`App` project) and thin Win32 P/Invoke wrappers (`ScreenCaptureService`, `GlobalHotkeyService`,
  `CaptureFeedbackPresenter`, `ImagePreviewWindow`) are exempt from unit testing per the constitution's
  Principle IX, but must stay thin enough that all real logic lives in the tested `Core` services they
  call
