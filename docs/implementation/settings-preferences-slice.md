# Settings preferences local slice

Status: `SLICE_IMPLEMENTED` in PR #4; runtime acceptance remains owner verification.

## Contract trace

- Goal: `NXG-FX09-G01…G03` and `SET-001…004`.
- Source: [FX-09 Settings and Application Shell](../features/09-settings-and-app-shell.md),
  `P01-SHL-001…007`, and the versioned Preference payload contract.
- Operations: `listPreferences`, `updatePreference`.

## Implemented behavior

- Global preferences are stored in `platform.Preference` with an owner derived
  from the authenticated PersonalSpace, explicit creator/updater attribution,
  schema version, JSON validation and SQL rowversion.
- Registered non-secret payloads are limited to `theme`, `locale`, `nav` and
  `list`; unknown keys and arbitrary settings bags are rejected. The first
  preference write requires `If-Match: *`; later writes require the current
  ETag.
- The frontend exposes a Theme editor and keeps drafts in memory. No auth
  token, credential, channel mute or quiet-hours preference is added.

## Evidence status

Frontend build and structural static verification ran on the working tree.
.NET build, migration execution, functional/API/integration/E2E/browser tests
and manual QA were not run in this code-only environment.
