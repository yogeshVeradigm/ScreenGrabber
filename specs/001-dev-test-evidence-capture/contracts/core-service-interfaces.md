# Internal Service Contracts: DevTestEvidenceCapture.Core

These are the interface boundaries the `App` (WPF/UI) project is allowed to depend on. They are the
mechanism for Constitution Principle IV (Separation of Concerns): the UI project MUST call these
interfaces only, never touch `System.Drawing`, `RegisterHotKey`, or `System.IO` directly.

## `IAppConfigurationService` (Configuration)

```csharp
Task<AppConfiguration> LoadAsync();
Task SaveAsync(AppConfiguration config);
```

Owns reading/writing `config.json`. Supplies defaults (default shortcut, no root folder configured
yet) when the file does not exist.

## `IEvidenceStorage` (Storage)

```csharp
CaptureSession CreateSessionFolder(string rootFolder, string ticketNumber, string scenarioName);
Task<CaptureSession?> TryFindInterruptedSessionAsync(AppConfiguration config);
int GetNextSequenceNumber(string sessionFolderPath);
```

Owns folder creation under `<root>\<ticket>\<scenario>\`, collision disambiguation (FR-004), and
resume detection (research.md §4). Delegates name sanitization to `Naming`. `TryFindInterruptedSessionAsync`
is async because it must await `IScenarioMetadataService.ReadAsync` to inspect the candidate
session's `scenario.json` — a synchronous signature would force a sync-over-async call on the WPF UI
thread and risk a deadlock.

## `INamingService` (Naming)

```csharp
string SanitizeForFileSystem(string rawName);
string FormatScreenshotFileName(int sequenceNumber);
RenameResult ApplyRenames(CaptureSession session, IReadOnlyDictionary<int, string?> sequenceNumberToMeaningfulName);
```

Pure, deterministic logic: Windows-safe sanitization (research.md §5), numeric filename formatting
(FR-007), and conflict-checked batch rename (FR-016, FR-017, FR-021). `RenameResult` reports either
success with the applied final filenames, or a list of conflicting sequence numbers with no files
touched.

## `IScreenCaptureService` (Capture)

```csharp
Task<byte[]> CapturePrimaryMonitorPngAsync();
```

Wraps `GetSystemMetrics` + `Bitmap`/`Graphics.CopyFromScreen` + PNG encode (research.md §1). Returns
raw PNG bytes; saving them to disk is `IEvidenceStorage`'s job, keeping capture independent of storage
(FR-022).

## `IGlobalHotkeyService` (Hotkey)

```csharp
event EventHandler? HotkeyPressed;
void Register(IntPtr windowHandle, string shortcutDescription);
void Unregister();
bool ProcessWindowMessage(int message, IntPtr wParam);
```

Wraps `RegisterHotKey`/`UnregisterHotKey` and parses the shortcut string into modifiers/virtual-key
codes (research.md §2). Because `Core` has zero WPF references (Principle IV), it cannot use
`HwndSource` itself — `ProcessWindowMessage` lets the `App` project's WPF message hook feed raw
window messages in, and this service reports back whether it was the registered `WM_HOTKEY`. Raises
`HotkeyPressed`; never touches capture or storage itself.

## `IScenarioMetadataService` (Metadata)

```csharp
Task WriteAsync(CaptureSession session);
Task<ScenarioMetadata> ReadAsync(string sessionFolderPath);
```

Owns `scenario.json` serialization per `contracts/scenario-metadata.schema.json`. Called after every
capture (to keep `screenshotCount`/timestamps current) and after renaming (to update `fileName`s per
FR-024).

## `ISessionManager` (Session)

```csharp
CaptureSession? Current { get; }
event EventHandler<CaptureFailedEventArgs>? CaptureFailed;
Task StartAsync(string ticketNumber, string scenarioName);
Task<CaptureSession?> TryResumeInterruptedSessionAsync();
void DeclineResume();
Task<Screenshot?> CaptureAsync();
Task StopAsync();
Task<RenameResult> RenameAsync(IReadOnlyDictionary<int, string?> sequenceNumberToMeaningfulName);
Task SkipRenameAsync();
```

The orchestration root the `App` view models call. Composes `IEvidenceStorage`,
`IScreenCaptureService`, `IScenarioMetadataService`, `INamingService`, and `IAppConfigurationService`
(to resolve the root folder and persist `lastActiveSessionPath`); enforces FR-008 (`CaptureAsync`
returns `null` rather than throwing when no session is active — a no-op, not an error) and FR-011
(only one active session). `TryResumeInterruptedSessionAsync` is called once on app launch; if it
finds an interrupted session it becomes `Current` (with `Status` set back to `Active`) and is
returned, otherwise it returns `null`. `CaptureFailed` is raised (per FR-025a) when a capture attempt
fails; the session is kept active and subsequent captures are still attempted.
