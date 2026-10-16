# FX34 Project selection — 2026-10-11

P06-AUT-001/002, FX34-S02, automation.definition.create/update/validate/import. IMPLEMENTED_UNVERIFIED; runtime acceptance NOT_RUN under the current code-only execution amendment.

AutomationProjectPicker replaces raw Project UUID entry in the working workflow editor. It reads existing owner-authorized Projects list/detail endpoints, with 25-row cursor paging and a separate bounded lookup for the selected Project outside the current page. Completed/Skipped/Deleted Projects cannot be selected. Imports retain their target identity and explicitly require rebind when it is unavailable. Reads never substitute a different Project into the pending command. Existing SQL reference, permission and Task time-bound validation remains authoritative at Validate/Enable/Run.

Production frontend: AutomationScreen.tsx and AutomationProjectPicker.tsx. Existing Projects API, SQL persistence and domain validation are reused; no schema, provider activation or new server endpoint. Pending-command disabled fieldset and root navigation guards remain in force. Generation fences discard late reads; denied/error reads clear previously displayed Project names. React escapes names and the picker does not project descriptions or notes.

Frontend TypeScript/Vite build PASS (existing large-bundle warning), git diff --check PASS. Independent source/security review: no confirmed blocker. Functional, SQL/API and browser verification NOT_RUN; no tests, fixtures or demo records created. This is an editor workflow improvement, not completion of FX34. Scheduler, verified DryRun, terminal retry, retention/purge/support and remaining acceptance are still outstanding.
