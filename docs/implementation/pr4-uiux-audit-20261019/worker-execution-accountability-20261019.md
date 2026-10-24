# PR #4 — What each of the nine parallel reviewers ACTUALLY did

**Evidence:** [GitHub Actions run 39216459096](https://github.com/hakodev2k/Nexora/actions/runs/39216459096), tested SHA `9fa7a8fd1a79babe8c76175028550d96ca35f5be` (**historical**, not current PR HEAD). This checkpoint was created by inspecting the JSON and screenshot file indexes inside **all nine actual artifact ZIPs** and manually viewing representative Finance/Admin screenshots.

**Do not conflate AI and test jobs:** **0 spawned AI subagents** (no subagent API is exposed), **9 independent GitHub Actions Playwright UI review workers**, maximum 6 concurrent; each had its own disposable SQL Server, .NET API, Vite frontend, synthetic accounts and two Chromium projects: desktop1440/mobile390. **6 job successes; 3 failures**. A successful job is *not* a certified professional UI or completed product feature.

## Aggregate execution

| Measurement | Evidence from all 9 ZIP manifests |
|---|---:|
| Route × viewport targets | **164** |
| Route × viewport attempted | **132** |
| First-viewport probes rendered | **128** |
| Runtime blocked | **4** |
| Planned probes not attempted | **32** |
| Screenshot references | **130** |
| Non-destructive popup-opening events | **9** |
| Automated finding occurrences (includes repeated viewports and expected gated states) | **74** |

**Action levels:** `BROWSED` = navigate, read H1, measure reflow/axe, take screenshot. `DIALOG_OPENED` = click safe opener, inspect dialog and close via Escape; **not Save/Confirm**. `SQL_WRITE_CONFIRMED` = synthetic fixture actually created via real UI/API/local SQL. `GATED/PLACEHOLDER` = only an unavailable/placeholder shell rendered. `BLOCKED/NOT_RUN` = no accepted page review. No real providers, production data or destructive real-world actions.

## Detailed accountability of each reviewer

### Worker 1 — Authentication: FAILED

**Persona:** Anonymous. **Scope:** Login, Register, Email Verify, Forgot, Password Reset, invalid ShareLink. **Actual:** **10/12 rendered**, **2 blocked** (same ShareLink route at both sizes), **0 popups**. Visited `/login`, `/register`, `/verify-email`, `/password/forgot`, `/password/reset`; captured public form UI but **did not** complete a registration/reset mutation in this shard. `/share/synthetic-invalid-token` produced **NO_MAIN_LANDMARK (P1)**: no accepted main error view, possibly a modal-only/public error implementation or harness assumption. **No other form lifecycle is accepted by screenshot alone.** Artifact **12169851635**. Next: invalid-token error semantics, field-level validation, keyboard, full register/verify/login flows.

### Worker 2 — Navigation: FAILED

**Persona:** UserA. **Scope:** Home/Search/Saved/Favorites/Recents/Palette/Profile/Security/Inbox/Trash. **Actual:** **10/20 rendered**, **2 blocked**, **8 not attempted**. Observed Home, Search, Saved Searches, Favorites and Recents on desktop/mobile. **DIALOG_OPENED:** `/favorites` → “Thêm favorite” **twice across viewports**, no Save. Command Palette was blocked (MISSING_H1 + ROUTE_NOT_AUDITED); modal-only route is not necessarily a defective page. Remaining Profile/Security/Notifications/Trash not reached in this shard. Home generated SMALL_INTERACTIVE_TARGETS heuristic. Artifact **12170715199**. Next: use keyboard shortcut to open Palette, isolate route failures, populated Notification and navigation dirty-state tests.

### Worker 3 — Productivity: SUCCESS (route-shell only)

**Persona:** UserA. **Scope:** Goals/Planner/Habits and FX11–FX19. **Actual:** **24/24 rendered**. **SQL_WRITE_CONFIRMED:** a synthetic Project and dependent Task were created via the real UI→API→isolated SQL on desktop and mobile. **DIALOG_OPENED:** mobile Planner → “Thêm pin” (one screenshot), not submitted; desktop did not report a popup. Habits/FX17 each flagged SMALL_INTERACTIVE_TARGETS in both viewports (**4 heuristic instances**, not 4 verified tap defects). The screens show real Projects/Tasks layout but history/terminal states, drag/reorder, dates, dirty/cancel and Task/Reminder confirmation variants were not exercised. Artifact **12169539648**. Next: record edit/Archive/Trash, status transitions, protected refresh and 320px touch targets.

### Worker 4 — Knowledge: FAILED / NOT REVIEWED

**Persona:** UserA. **Scope:** Bookmarks, Snippets, ReadLater, Tags, Files, WorkLog, Sharing and FX20–FX24. **Actual:** **0/24 attempted**, **0 screenshots**, **0 popups**. Intended Bookmarks/Snippets synthetic UI creations and Notification SQL fixture returned unverified/blocked; the shared preparation flow aborted **before any route probe**. No visual quality verdict is possible. Artifact **12169729516**. The later isolated-route test-code fix cannot retroactively turn this run green. Next: first rerun all 12 pages with route isolation, then separately seed real owned records for CRUD/popup scenarios.

### Worker 5 — Business: SUCCESS (mixed real and gated screens)

**Persona:** UserA. **Scope:** Finance and FX27, FX29, FX31, FX32, FX36–40. **Actual:** **20/20 rendered**. **SQL_WRITE_CONFIRMED:** Finance Category plus explicit-VND manual Record created through UI/API/isolated SQL for both viewport projects. **DIALOG_OPENED:** each viewport opened two Finance dialogs (“Thêm category”, “Thêm record”), plus one FX27 category dialog = **6 open events**, **no exploratory Save**. **UI finding from inspected screenshot** `screenshots/UserA-1440-finance-dialog-0.png`: “Category mới” heading duplicated; “Làm mới/Tạo category” is detached from the bottom “Hủy” button; unclear primary/cancel hierarchy. **7 FX module routes per viewport** displayed only “Module chưa khả dụng” (**14 P2 coverage findings**), not working features. Artifact **12169873178**. Next: one modal title, consistent footer, keyboard/validation/dirty and feature real-state review where authorized.

### Worker 6 — Platform: SUCCESS (placeholder observation only)

**Persona:** UserA. **Scope:** FX01–FX10. **Actual:** **20/20 rendered**, **0 popups**, **no mutations**. All module screens appeared either as a generic “Module entry” **(6 per viewport)** or explicit “Module chưa khả dụng” **(4 per viewport)**. **20 finding instances** represent incomplete/gated UI coverage, not 20 confirmed backend bugs. A module entry card with “Available” or an FX code is not a complete feature UX. Artifact **12169429568**. Next: access the true implemented page and its actions with appropriate owner/role; never count placeholder as complete.

### Worker 7 — Gated/Integrations: SUCCESS (intentionally no real execution)

**Persona:** UserA. **Scope:** Developer Tools/Support and FX25, FX26, FX28, FX30, FX33, FX34, FX35. **Actual:** **18/18 rendered**, **0 dialogs** and **no real provider request**. There were **2 generic placeholders + 5 unavailable modules per viewport = 14 P2 availability findings**. This worker confirmed the visible gated experience, not that Vault, price monitoring or external automation work. Artifact **12169723608**. No unlocking FX28 secrets or FX30/34/35 provider jobs is permitted.

### Worker 8 — Administration: SUCCESS (visual/accessibility only)

**Persona:** SuperAdmin. **Scope:** Home, Admin Access/Modules, Security, Support, Sharing, Notifications. **Actual:** **14/14 rendered**, **0 popups**, **no role/grant write**. Inspected screenshot `screenshots/SuperAdmin-1440-admin-access-initial.png`: readable two-column user selector/editor, but heavy vertical information density, mixed technical labels “ETag”, “Action key”. **AXE_color-contrast on `/admin/access`** at both sizes, **16 affected DOM elements on mobile**, including selected user-row labels. SMALL_INTERACTIVE_TARGETS on Home/Access/Modules (heuristic: mobile module catalog reported **120 small dimension flags** and **18619px document height**, not 120 proven accessibility failures). **8 finding instances** total. Artifact **12169898207**. Next: compare actual contrast ratios, preview role grants, denied updates, stale ETag and full dialog keyboard/focus.

### Worker 9 — Restricted: SUCCESS (rendered safe states, not security audit)

**Persona:** Restricted synthetic user. **Scope:** Home, Projects FX11, Documents FX20, Sharing, Files, Profile. **Actual:** **12/12 rendered**, **0 popups**, **no cross-owner mutation or privilege escalation attempt**. Each viewport logged **2 SMALL_INTERACTIVE_TARGETS heuristics + 1 MODULE_UNAVAILABLE_NOT_FEATURE_IMPLEMENTED** (**6 instances**). A visible denied/unavailable state is only UX evidence; it does not certify current API authorization or absence of stale private data. Artifact **12169504796**. Next: exact Deny/Unset, cross-owner and source-revoked API checks, plus UI cache purge.

## UI verdict and incomplete popup coverage

The inspected Finance dialog shows a **real UX hierarchy problem** (duplicate heading and split footer). The inspected desktop Admin Access layout is understandable but dense; automated contrast finding is reproducible. Previously inspected mobile headers consume too much vertical space. In contrast, **Platform/gated placeholders cannot be visually scored as complete application pages**. There are **only 9 popup-open events**, while the [popup register](popup-source-register.md) enumerates **60 shared JSX invocation sites and 29 specialized component files**; these counts are not interchangeable. Conditional edit/delete/archive/restore/revoke dialogs and validation/draft/error states remain **NOT_EXERCISED** without a fixture and screenshot.

## Mandatory post-test report format for every new execution

For **each named worker** report: true AI-agent count (if any) vs number of automated Playwright workers; CI run ID and **exact source SHA**, role, viewport, worker start/completed/failure state; planned and actually opened route names; actions recorded as `navigate/click-open/click-tab/create-save/edit-save/confirm/cancel` with a distinction between attempted and successful DB mutation; page H1/visible state, layout, key CTA, popup title/footer, responsive/accessibility observations; **all finding codes with severity and reproduction path**; screenshot/JSON/JUnit artifact; **not-run/gated states** and recommended fixes. Show both per-worker and overall totals, explicitly separated by UI-visible, action-completed, blocked and untested counts. Do not infer passing UX from zero axe errors or server HTTP200.

**Keep PR #4 Draft; no merge and no real providers.** This report is historical run evidence, not latest-head release acceptance.
