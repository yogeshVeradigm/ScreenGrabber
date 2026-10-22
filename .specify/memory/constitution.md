<!--
Sync Impact Report
- Version change: (uninstantiated template) → 1.0.0
- Rationale: Initial ratification of the project constitution (first concrete adoption; MAJOR-equivalent baseline set at 1.0.0).
- Modified principles: none (initial creation)
- Added principles:
  - I. Radical Simplicity & Phase-1 Scope Discipline
  - II. Local-First, Zero Data Egress
  - III. Idiomatic, Maintainable C# on .NET 8 / WPF
  - IV. Separation of Concerns
  - V. Deterministic, Ordered, Non-Destructive Persistence
  - VI. Reliable Global Capture
  - VII. Simple, Local Configuration
  - VIII. Forward-Compatible Metadata, No Premature Integration
  - IX. Automated Tests for Business Logic
- Added sections: Technology & Platform Constraints; Development Workflow & Quality Gates; Governance
- Removed sections: none
- Templates requiring updates: none found referencing constitution version outside this file
  (checked .specify/templates/*.md) — no further propagation needed.
- Deferred TODOs: none
-->

# Dev Test Evidence Capture Constitution

## Core Principles

### I. Radical Simplicity & Phase-1 Scope Discipline

The application MUST implement only what Phase 1 requires and MUST NOT gain speculative features,
frameworks, or abstractions built for hypothetical future phases. Third-party dependencies MUST be
minimized; a new dependency MUST be justified against equivalent functionality already available in
the .NET 8 Base Class Library or WPF before it is added.

Rationale: this is an internal productivity tool — every added dependency, layer, or option increases
maintenance cost without user-visible benefit.

### II. Local-First, Zero Data Egress

The application MUST run entirely on the local machine. Phase 1 MUST NOT introduce cloud services, a
database, authentication, or external network APIs. Screenshots and testing metadata MUST NEVER be
uploaded, synced, or transmitted to any remote endpoint, telemetry service, or third party, under any
configuration.

Rationale: the tool captures potentially sensitive internal test evidence; the only way to guarantee
it never leaks is to make network egress structurally impossible, not merely disabled by default.

### III. Idiomatic, Maintainable C# on .NET 8 / WPF

The application MUST target .NET 8 and WPF for the UI unless a specific Phase 1 requirement cannot be
met by that stack, in which case the alternative and its justification MUST be recorded in the
implementation plan. Code MUST follow idiomatic, maintainable C# conventions (nullable reference
types enabled, async/await for I/O, no unnecessary reflection or dynamic typing).

Rationale: a single, well-understood, in-support stack keeps a small internal tool cheap to maintain
and easy for any .NET developer to pick up.

### IV. Separation of Concerns

UI, application/orchestration logic, screenshot capture, global hotkey handling, and file storage
MUST be implemented as distinct components with explicit boundaries (e.g., separate classes or
namespaces), communicating through interfaces rather than direct concrete-type coupling. The UI layer
MUST NOT contain capture, hotkey, or file I/O logic directly.

Rationale: these concerns fail and evolve on different timelines (e.g., hotkey handling is
Win32-interop-heavy, storage is filesystem-heavy), and mixing them makes the smallest change risky to
test.

### V. Deterministic, Ordered, Non-Destructive Persistence

File and folder names MUST be deterministic, generated from well-defined rules, and safe for the
Windows filesystem (no reserved characters, reserved device names, or trailing dots/spaces). Screenshot
numbering MUST strictly preserve the order in which captures occurred. The application MUST NEVER
overwrite an existing screenshot file; on a naming collision it MUST fail safely (surface an error or
generate the next safe, non-colliding name) rather than silently destroy prior evidence.

Rationale: the entire value of this tool is a trustworthy, ordered record of what happened during
manual testing — corrupting or losing that record defeats its purpose.

### VI. Reliable Global Capture

The screenshot capture mechanism MUST work globally: it MUST function while the application window is
minimized, hidden, or not focused, and while any other application holds foreground focus. Hotkey
registration and capture MUST NOT depend on the application under test having any awareness of this
tool.

Rationale: developers trigger captures while interacting with the application being tested, not with
this tool's own window.

### VII. Simple, Local Configuration

Configuration MUST be stored locally (e.g., a local file such as JSON under the user's app-data or the
application folder) and MUST NOT require any server, service, or account. Configuration MUST have
sensible defaults so the application is usable without any setup.

Rationale: matches the local-first, zero-friction nature of an internal dev tool.

### VIII. Forward-Compatible Metadata, No Premature Integration

The generated folder structure and any capture metadata MUST be designed so that a future Claude Code
integration or AI-generated test documentation feature could consume them (e.g., predictable layout,
machine-readable metadata alongside images). Phase 1 MUST NOT implement that integration or any
AI-facing feature itself — only the data shape needs to anticipate it.

Rationale: keeps Phase 1 simple (Principle I) while avoiding a costly reshape of on-disk data later.

### IX. Automated Tests for Business Logic

Business logic — file/folder naming rules, numbering/ordering, collision handling, and configuration
parsing — MUST have automated tests where practical. UI code and thin Win32 interop wrappers are
exempt from this requirement but MUST be kept thin enough that the logic they call is what gets
tested.

Rationale: the correctness guarantees in Principles V and VI are exactly the kind of rules that
regress silently without tests.

## Technology & Platform Constraints

- **Target platform**: Windows desktop only (Windows 10/11); no cross-platform requirement in Phase 1.
- **Stack**: .NET 8, WPF, C# with nullable reference types enabled. Any deviation MUST be justified
  in the implementation plan.
- **Dependencies**: prefer the .NET BCL and Win32 interop over third-party packages; each added
  package MUST be named and justified in the plan.
- **Out of scope for Phase 1**: cloud services, databases, authentication/identity, external network
  APIs, telemetry/analytics, auto-update.

## Development Workflow & Quality Gates

- Any change that adds a third-party dependency, changes the on-disk folder/file/metadata format, or
  weakens the no-overwrite or global-capture guarantees MUST call out the affected principle in the
  plan or PR description.
- Automated tests for business logic (Principle IX) MUST pass before a change is considered complete.
- Features or integrations beyond Phase 1 scope (e.g., AI/Claude Code integration, cloud sync) MUST be
  captured as deferred/future work, not implemented under this constitution's Phase 1 principles.

## Governance

This constitution supersedes other informal practices for this project. Amendments require: (1) a
documented rationale for the change, (2) a version bump following semantic versioning — MAJOR for
backward-incompatible principle removals or redefinitions, MINOR for new principles or materially
expanded guidance, PATCH for clarifications or wording — and (3) an update to the Last Amended date
below. All plans and PRs MUST verify compliance with this constitution; any deviation MUST be
explicitly justified against the specific principle it relaxes. Use `.specify/memory/constitution.md`
as the source of truth for runtime development guidance during planning and implementation.

**Version**: 1.0.0 | **Ratified**: 2026-09-10 | **Last Amended**: 2026-09-10
