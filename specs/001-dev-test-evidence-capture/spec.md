# Feature Specification: Dev Test Evidence Capture

**Feature Branch**: `001-dev-test-evidence-capture`

**Created**: 2026-09-10

**Status**: Draft

**Input**: User description: "Create the Phase 1 specification for \"Dev Test Evidence Capture\". A Windows desktop utility for software developers performing manual dev testing, to make collecting and organizing screenshot evidence quick and consistent. Core workflow: enter ticket number and scenario name, select a root evidence folder, start a capture session which creates `<root folder>\<ticket number>\<scenario name>\`, capture screenshots globally via a keyboard shortcut while minimized (01.png, 02.png, ...), stop the session, then optionally rename screenshots (numeric prefix preserved, names may be left blank, renaming may be skipped entirely) without ever overwriting existing evidence. Each scenario has a scenario.json with ticket number, scenario name, start/end timestamps, screenshot count, filenames, and capture timestamps, always reflecting final filenames. UI needs ticket/scenario inputs, root folder configuration, Start/Stop Session, session status, screenshot count, configured shortcut, and a post-session naming screen. Must handle invalid Windows filename characters, duplicate scenario folders, and unexpected app closure safely, and keep capture independent of the naming UI. Out of scope for Phase 1: JIRA, Claude/AI integration, AI screenshot analysis, document generation, OCR, annotation, cloud storage, database, authentication, multi-user — but the folder/metadata structure should anticipate future discovery by a Claude Code skill."

## Clarifications

### Session 2026-09-10

- Q: If the app is closed (or crashes) while a session is active and then reopened, should the user be able to resume that same session — continuing to capture into the same folder with numbering picking up where it left off — or is that session simply over? → A: Support resuming; on next launch, detect an interrupted session and let the user reopen it to keep capturing into the same folder, continuing the numeric sequence correctly.
- Q: What exact folder structure should the app create for a session — nested `root\<ticket>\<scenario>\` or flat `root\<ticket>_<scenario>\`? → A: Nested — one folder per ticket, one subfolder per scenario, matching `root\<ticket number>\<scenario name>\`.
- Q: If saving a screenshot fails during an active session (disk full, unwritable folder, permission error), what should the app do? → A: Keep the session active, show a persistent non-blocking warning/error indicator on the app, and keep retrying subsequent captures — never silently drop a failure or force-stop the session.
- Q: When a screenshot is captured, what should confirm it to the user — a sound, a visual flash, or both? → A: Both — a short confirmation sound and a brief screen-wide/edge visual flash, since the app may be minimized or unfocused when the capture happens and either signal alone might be missed.
- Q: When a screenshot's thumbnail is clicked on the naming screen, should the preview open in an in-app popup or the system's default image viewer? → A: A simple in-app popup — it keeps the user inside the renaming workflow and closes on a subsequent click or Escape.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Capture a full set of screenshots for a ticket/scenario without losing any evidence (Priority: P1)

A developer manually testing a ticket wants to record what they see on screen as they go, without the
capture tool getting in the way of the application they're actually testing.

**Why this priority**: This is the entire reason the tool exists. Without reliable, ordered, global
capture, there is no product — everything else (naming, metadata) only adds value on top of a
working capture flow.

**Independent Test**: Can be fully tested by entering a ticket number and scenario name, starting a
session, minimizing the app, switching focus to another application, pressing the global capture
shortcut several times, then stopping the session — and verifying a folder exists containing the
correct number of sequentially numbered screenshots in the order they were taken.

**Acceptance Scenarios**:

1. **Given** a ticket number, scenario name, and a configured root folder, **When** the user starts a
   session, **Then** a session folder is created under the root folder identifying the ticket and
   scenario, and the app enters an active capture state.
2. **Given** an active capture session, **When** the user minimizes the app and gives focus to a
   different application, **Then** pressing the global capture shortcut still saves a new screenshot
   into the session folder.
3. **Given** an active capture session with the app minimized or unfocused, **When** a screenshot is
   successfully captured, **Then** the user perceives a confirmation sound and a brief visual flash,
   without needing to bring the app's window into focus or view.
4. **Given** an active session with N screenshots already captured, **When** the user presses the
   capture shortcut again, **Then** a new screenshot is saved as file number N+1, and no existing
   screenshot file is modified or overwritten.
5. **Given** an active session, **When** the user stops the session, **Then** the app exits the active
   capture state and no further screenshots are added to that session's folder.

---

### User Story 2 - Give screenshots meaningful names while keeping their capture order intact (Priority: P2)

After finishing a round of testing, the developer wants the evidence to be readable later — by
themselves, a reviewer, or a future tool — without losing track of the order things happened in.

**Why this priority**: This turns an anonymous sequence of images into understandable evidence. It is
valuable but not required for the tool to be useful; a developer can still find value in ordered,
unnamed screenshots.

**Independent Test**: Can be fully tested by completing a session with several screenshots, opening
the renaming screen, assigning names to some screenshots, leaving others blank, and verifying the
resulting filenames preserve the original numeric prefixes while reflecting the chosen names (or the
original name where left blank).

**Acceptance Scenarios**:

1. **Given** a just-completed session with screenshots `01.png`, `02.png`, `03.png`, **When** the
   renaming screen opens, **Then** all three are listed in that same order, each showing a small
   thumbnail of the image, its current filename, and an editable meaningful-name field.
2. **Given** the renaming screen, **When** the user clicks a screenshot's thumbnail, **Then** an
   in-app popup opens showing the full-size image, and it closes on a subsequent click or the Escape
   key.
3. **Given** the renaming screen, **When** the user enters "Login page" for `01.png` and leaves
   `02.png` blank, **Then** confirming results in `01_Login_page.png` and `02.png` unchanged.
4. **Given** the renaming screen, **When** the user's input would give two screenshots the same final
   filename, **Then** the app flags the conflict and blocks confirmation until the user changes one of
   the names.
5. **Given** the renaming screen, **When** the user chooses to skip renaming entirely, **Then** all
   screenshots keep their original numeric filenames unchanged.

---

### User Story 3 - Trust the recorded metadata as the source of truth for what was captured (Priority: P3)

A developer (or a future automated tool) wants to look at a scenario folder and know, without opening
the app, what ticket and scenario it belongs to, how many screenshots exist, and what each one is
called.

**Why this priority**: Metadata makes the evidence usable outside the app and sets up future
consumption (e.g., a Claude Code skill), but the folder of screenshots is still useful on its own even
without it.

**Independent Test**: Can be fully tested by completing a session (with or without renaming) and
inspecting the resulting `scenario.json`, verifying it lists the correct ticket number, scenario name,
timestamps, screenshot count, and the final filenames/capture timestamps.

**Acceptance Scenarios**:

1. **Given** a completed session that was not renamed, **When** `scenario.json` is inspected, **Then**
   it lists the original numeric filenames and matches the actual files on disk.
2. **Given** a completed session where some screenshots were renamed, **When** `scenario.json` is
   inspected, **Then** it lists the final (post-rename) filenames, not the original numeric-only
   names.
3. **Given** any completed session, **When** `scenario.json` is inspected, **Then** the screenshot
   count matches the number of screenshot files actually present in the folder.

---

### Edge Cases

- Starting a session whose ticket number + scenario name combination already has a scenario subfolder
  (and it is not being resumed as an interrupted session): the app creates a new, distinguishable
  scenario subfolder under the same ticket folder rather than reusing or merging into the existing
  one, so existing evidence is never touched.
- Ticket number, scenario name, or a user-entered meaningful name contains characters that are invalid
  in Windows file or folder names: the app substitutes them with a safe, deterministic replacement so
  folder/file creation never fails.
- The capture shortcut is pressed while no session is active: nothing happens — no file is created and
  no error is surfaced.
- The app is closed, crashes, or the machine loses power while a session is active: all screenshots
  already captured remain on disk, and that session's folder has no end timestamp recorded. On the
  next launch, the app detects the interrupted session and offers to resume it; if the user resumes,
  capture continues into the same folder with numbering picking up correctly from the existing
  screenshots. If the user does not resume it (or starts a different session instead), the
  interrupted session is left as-is and is not offered a renaming step until it is later resumed and
  explicitly stopped.
- The user tries to start a new session while one is already active: the app prevents it until the
  current session is stopped.
- The renaming step is interrupted partway (e.g., the app is closed mid-rename): screenshots already
  renamed keep their new names, the rest keep their original numeric names — no screenshot is lost or
  left in a corrupted state.
- A session is stopped with zero screenshots captured: the (empty) session folder and its
  `scenario.json` (with a screenshot count of zero) still exist, and the renaming step is skipped
  automatically since there is nothing to rename.
- The configured root folder is missing, removed, or not writable when the user tries to start a
  session: the app blocks starting the session with a clear message until a valid, writable root
  folder is configured.
- Saving a screenshot fails during an active session (e.g., disk full, folder became unwritable, or a
  permission error): the session stays active, a persistent non-blocking warning is shown on the app,
  and the app keeps retrying subsequent capture attempts; the failure is never silently dropped and
  never force-stops the session.
- The machine has no audio output device, or system sound is muted, when a screenshot is captured: the
  confirmation sound is skipped or inaudible, but the visual flash still occurs and the capture itself
  is never affected or delayed by a sound-playback problem.
- A screenshot file referenced by the renaming step is missing, unreadable, or corrupted when its
  thumbnail is loaded: the row shows a placeholder image instead of a thumbnail, and the rest of the
  renaming step (including that row's meaningful-name field) continues to work normally.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: Users MUST be able to enter a ticket number and a scenario name before starting a
  capture session.
- **FR-001a**: The ticket number input MUST offer the 10 most recently used ticket numbers. It MUST
  accept any identifier — not only JIRA keys but other trackers' IDs or a plain task name — and MUST
  NOT warn about or restrict its format beyond the filename sanitization in FR-018.
- **FR-002**: Users MUST be able to configure/select a root evidence folder; the app MUST remember it
  locally as the default for future sessions.
- **FR-003**: When a session starts, the system MUST create a session folder using a nested
  structure: a folder named for the ticket number directly under the root folder, containing a
  subfolder named for the scenario name (`root\<ticket number>\<scenario name>\`).
- **FR-004**: If a scenario subfolder for the same ticket number and scenario name already exists
  (and is not an interrupted session being resumed per FR-020a), the system MUST create a new,
  distinguishable scenario subfolder rather than reusing, merging into, or overwriting the existing
  one.
- **FR-005**: An active capture session MUST continue to accept captures while the application window
  is minimized or does not have focus, and while another application has focus.
- **FR-006**: The system MUST provide a global keyboard shortcut that, while a session is active,
  freezes the current contents of the Windows primary monitor and lets the user drag a rectangle to
  choose the area to save (Enter saves the full monitor; Esc or right-click cancels without saving or
  consuming a sequence number), then saves the selection as a new screenshot in the active session's
  folder. Pressing the shortcut again while the selection overlay is open MUST have no effect.
- **FR-006a**: After choosing an area and before saving, the user MUST be able to annotate it with a
  translucent yellow highlighter (configurable brush size) and with rectangles/ellipses (red by
  default, selectable colour, filled or border-only, configurable border thickness), and undo
  annotations. Annotations MUST be burned into the saved PNG. The highlighter size, border thickness,
  fill choice and colour MUST be remembered across captures and app launches.
- **FR-006b**: The annotation step MUST offer a pixelate tool that replaces a dragged area with
  coarse blocks of average colour (configurable, remembered block size) to obscure sensitive data.
- **FR-006c**: The annotation step MUST offer an optional free-text note, saved with that screenshot
  in `scenario.json` and shown on the naming screen.
- **FR-006d**: When enabled (the default, remembered), each saved screenshot MUST also be placed on
  the clipboard; a clipboard failure MUST NOT affect the saved file and MUST surface a non-blocking
  warning.
- **FR-007**: Screenshot filenames MUST use a zero-padded numeric sequence (minimum two digits,
  expanding automatically if a session exceeds 99 screenshots) that reflects strict chronological
  capture order; this order MUST NOT change as a result of any later action, including renaming.
- **FR-008**: Pressing the capture shortcut while no session is active MUST have no effect (no file is
  created, no error is surfaced).
- **FR-009**: Users MUST be able to capture additional screenshots repeatedly until they explicitly
  stop the session.
- **FR-010**: Users MUST be able to stop an active capture session via a UI control.
- **FR-011**: The system MUST prevent starting a new capture session while another capture session is
  already active.
- **FR-012**: When a session is stopped, the system MUST offer an optional renaming step, immediately,
  covering only that session's screenshots; this step MUST NOT be re-offered later (including after
  restarting the app) for a session that has already been completed.
- **FR-013**: The renaming step MUST list all of the session's screenshots in their original capture
  order, showing each one's current filename alongside an editable meaningful-name field.
- **FR-013a**: The renaming step MUST show a small thumbnail image of each screenshot alongside its
  row, so the user can identify which screenshot a row refers to without opening the file.
- **FR-013b**: Clicking a screenshot's thumbnail MUST open an in-app popup showing the full-size
  image; the popup MUST close on a subsequent click or the Escape key. This MUST NOT navigate away
  from or lose the in-progress renaming step.
- **FR-014**: Users MUST be able to leave any individual screenshot's meaningful-name field blank; a
  blank entry MUST leave that screenshot's original numeric filename unchanged.
- **FR-015**: Users MUST be able to skip the entire renaming step in one action, leaving all of the
  session's screenshots with their original numeric filenames.
- **FR-016**: When a meaningful name is provided for a screenshot, the system MUST rename the file by
  combining its unchanged numeric prefix with the provided name, preserving the numeric prefix
  exactly.
- **FR-017**: If the user's input during renaming would result in two screenshots sharing the same
  final filename, the system MUST block confirmation of the renaming step and flag the conflicting
  entries until the user changes the input; the system MUST NOT silently alter user-provided names to
  resolve the conflict.
- **FR-018**: The system MUST sanitize ticket number, scenario name, and meaningful-name input by
  replacing characters that are invalid in Windows file or folder names (including reserved device
  names and trailing dots/spaces) with a safe, deterministic substitute.
- **FR-019**: The system MUST NEVER overwrite an existing screenshot file, the existing contents of a
  session/scenario folder, or an existing `scenario.json`.
- **FR-020**: All screenshots already captured during a session MUST be preserved on disk if the
  application is closed, crashes, or is otherwise terminated before the session is explicitly stopped.
- **FR-020a**: On launch, the system MUST detect any session left interrupted (closed without being
  explicitly stopped) and offer the user the option to resume it, continuing capture into that same
  session folder with the next screenshot number determined correctly from the existing screenshots.
  The capture shortcut MUST have no effect until the user accepts; if the user declines, that session
  MUST NOT be offered again on later launches.
- **FR-020b**: `scenario.json`, the app configuration and screenshots MUST be written so that an
  unexpected exit mid-write never leaves a truncated file, and an unreadable configuration or
  `scenario.json` MUST NOT prevent the app from starting.
- **FR-021**: If the renaming step fails partway through (e.g., a file cannot be renamed), every
  screenshot MUST remain intact and identifiable by its original capture order, regardless of how many
  renames succeeded before the failure.
- **FR-022**: Screenshot capture MUST function independently of the renaming UI; a problem in the
  renaming step MUST NOT prevent or interrupt capture during a session.
- **FR-023**: The system MUST create and maintain a `scenario.json` file in the session folder
  containing at minimum: ticket number, scenario name, session start timestamp, session end timestamp,
  screenshot count, screenshot filenames, and each screenshot's capture timestamp.
- **FR-024**: The screenshot filenames recorded in `scenario.json` MUST always reflect the final
  filenames after the optional renaming step, or the original numeric filenames if renaming was
  skipped.
- **FR-025**: The system MUST clearly and continuously indicate, while a session is active, that
  capture is in progress.
- **FR-025a**: If saving a screenshot fails (e.g., disk full, unwritable folder, permission error),
  the system MUST keep the session active, display a persistent non-blocking warning indicator, and
  continue attempting subsequent captures; it MUST NOT silently discard the failure or automatically
  stop the session.
- **FR-025b**: On every successfully saved screenshot, the system MUST play a brief confirmation sound
  and show a brief visual flash; both MUST be perceivable regardless of whether the application window
  is visible, minimized, or focused. Neither MUST play on a failed capture (FR-025a's warning indicator
  is the failure signal instead).
- **FR-026**: The system MUST display the current screenshot count for the active session, updated as
  screenshots are captured.
- **FR-027**: The system MUST display the currently configured global capture keyboard shortcut, and
  MUST let the user change it by pressing a new key combination; if the new combination can't be
  registered, the previous shortcut MUST stay in effect.
- **FR-028**: The system MUST provide UI controls for: ticket number input, scenario name input, root
  folder configuration, Start Session, Stop Session, current session status, screenshot count, and the
  configured shortcut.
- **FR-029**: The Start Session control MUST remain unavailable until a ticket number, a scenario
  name, and a valid, writable root folder are all provided.

### Key Entities

- **Capture Session / Scenario**: One capture run tied to a specific ticket number and scenario name;
  has a session folder, a start timestamp, an end timestamp (once stopped), and an ordered set of
  screenshots.
- **Screenshot**: An individual captured image; has a capture-order sequence number, a capture
  timestamp, an original numeric filename, and an optional final filename if renamed.
- **Scenario Metadata (`scenario.json`)**: The persisted record of a session's ticket number, scenario
  name, start/end timestamps, screenshot count, and per-screenshot filenames and capture timestamps.
- **Root Evidence Folder**: The locally configured base location under which per-ticket folders are
  created, each containing one subfolder per scenario session (`root\<ticket number>\<scenario
  name>\`).

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: A developer can go from launching the app to actively capturing screenshots for a new
  ticket/scenario in under 30 seconds.
- **SC-002**: 100% of screenshots captured during a session remain present and correctly ordered even
  if the application is closed unexpectedly mid-session.
- **SC-003**: A developer can rename all screenshots in a 10-screenshot session, or skip renaming
  entirely, in under 2 minutes.
- **SC-004**: Across repeated sessions using the same ticket number and scenario name, no session ever
  results in an overwritten or lost screenshot.
- **SC-005**: Every completed scenario folder contains metadata sufficient to identify, without
  opening the app, which ticket and scenario the screenshots belong to and in what order they were
  captured.
- **SC-006**: 100% of successful captures produce a user-perceivable confirmation (sound and/or visual
  flash) without the user needing to bring the app's window into focus or view.
- **SC-007**: A developer can identify which screenshot is which on the naming screen by its thumbnail
  alone, without needing to open every file individually.

## Assumptions

- The global capture shortcut is configurable but ships with a sensible default; only one shortcut is
  active at a time in Phase 1.
- Screenshots are saved as PNG files, matching the naming examples provided.
- Multi-monitor capture targets the Windows primary monitor only in Phase 1; capturing a specific
  non-primary monitor or the full virtual desktop is out of scope.
- Renaming is offered exactly once, immediately after a session is stopped; Phase 1 has no feature to
  reopen a previously completed scenario to rename it later.
- A capture session is scoped to a single ticket/scenario at a time; a user must stop the current
  session before starting another.
- No login or user identity is associated with sessions in Phase 1, consistent with the no-
  authentication, single-local-user constraint on this project.
- The capture confirmation sound and visual flash are not user-configurable (no mute toggle, no flash
  color/duration setting) in Phase 1; they always fire together on a successful capture.
- Thumbnails are rendered at a small, fixed decoded size (not the full captured resolution) purely for
  the naming screen's list; the full-resolution image is only decoded when the user opens the in-app
  preview popup for that screenshot.
