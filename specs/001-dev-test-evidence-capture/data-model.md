# Phase 1 Data Model: Dev Test Evidence Capture

Derived from the feature spec's Key Entities and Functional Requirements. All types live in
`DevTestEvidenceCapture.Core` and have no WPF dependency.

## CaptureSession

Represents one capture run tied to a ticket + scenario. In-memory during an active session; its
on-disk projection is `ScenarioMetadata` (`scenario.json`).

| Field | Type | Notes |
|---|---|---|
| `TicketNumber` | `string` | Raw, pre-sanitization user input; non-empty (FR-001) |
| `ScenarioName` | `string` | Raw, pre-sanitization user input; non-empty (FR-001) |
| `SanitizedTicketFolderName` | `string` | Result of sanitization (Naming); used for the ticket-level folder |
| `SanitizedScenarioFolderName` | `string` | Result of sanitization; used for the scenario subfolder, disambiguated per FR-004 if a collision exists |
| `FolderPath` | `string` | Absolute path: `<root>\<SanitizedTicketFolderName>\<SanitizedScenarioFolderName>\` |
| `Status` | `enum { Active, Stopped, Interrupted }` | See State Transitions below |
| `StartTimestamp` | `DateTimeOffset` | Set when the session starts (or resumes for the first time) |
| `EndTimestamp` | `DateTimeOffset?` | Null until explicitly stopped (FR-023) |
| `Screenshots` | `List<Screenshot>` | Ordered by `SequenceNumber` ascending |

**Validation rules**:
- `TicketNumber` and `ScenarioName` MUST be non-empty after trimming whitespace (FR-001, FR-029).
- `FolderPath` is never reused for a new session if it already contains a completed or interrupted
  session's data, unless the interrupted session is being resumed (FR-004, FR-020a).

## Screenshot

| Field | Type | Notes |
|---|---|---|
| `SequenceNumber` | `int` | 1-based, strictly increasing, chronological (FR-007) |
| `OriginalFileName` | `string` | Zero-padded numeric name, e.g. `01.png` (FR-007) |
| `FinalFileName` | `string` | Defaults to `OriginalFileName`; updated after a successful rename (FR-016, FR-024) |
| `CaptureTimestamp` | `DateTimeOffset` | Set at the moment the capture succeeds |

**Validation rules**:
- `SequenceNumber` is immutable once assigned (FR-007); renaming only ever changes `FinalFileName`.
- `FinalFileName` MUST be unique within the session (FR-017) and MUST retain the numeric prefix
  (FR-016).

## ScenarioMetadata (`scenario.json`)

The persisted, on-disk record described in FR-023/FR-024. See `contracts/scenario-metadata.schema.json`
for the authoritative JSON Schema. Field names below are the JSON property names.

| Field | Type | Notes |
|---|---|---|
| `ticketNumber` | `string` | Original (pre-sanitization) value entered by the user |
| `scenarioName` | `string` | Original (pre-sanitization) value entered by the user |
| `sessionStart` | `string` (ISO 8601) | |
| `sessionEnd` | `string` (ISO 8601) or `null` | `null` while the session is Active/Interrupted (FR-020a) |
| `screenshotCount` | `int` | Always equals `screenshots.length` |
| `screenshots` | `array` | Each entry: `{ "sequenceNumber": int, "fileName": string, "capturedAt": ISO 8601 string, "note"?: string }` (`note` is omitted when none was given) |

**Invariant**: `fileName` for each entry always reflects the *final* filename after any renaming, or
the original numeric filename if renaming was skipped or not yet performed (FR-024).

## AppConfiguration (`config.json`)

Local-only settings described by Principle VII / FR-002 / FR-027.

| Field | Type | Notes |
|---|---|---|
| `rootFolderPath` | `string` | Last-configured root evidence folder; remembered across launches |
| `captureShortcut` | `string` | Human-readable description of the configured global shortcut (e.g. `"Ctrl+Shift+S"`); default set on first run |
| `lastActiveSessionPath` | `string?` | Set on session start, cleared on explicit stop; used for interrupted-session detection (research.md §4) |

## State Transitions (CaptureSession.Status)

```text
(none) --Start Session--> Active
Active --Stop Session--> Stopped        [renaming offered exactly once, immediately]
Active --app closed/crashed--> Interrupted
Interrupted --resume on next launch--> Active   [sequence number recomputed from disk]
Interrupted --user does not resume--> Interrupted   [stays as-is; no renaming offered]
```

- `Stopped` is terminal — a stopped session is never reopened for renaming (FR-012, Assumptions).
- `Interrupted` is the only non-terminal state that can transition back to `Active` (FR-020a).
