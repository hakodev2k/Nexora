# Nexora PR #4 — Multi-page UI/UX review and synthetic sandbox evidence

**Audit opened:** 2026-10-19. **Current disposition: AUTOMATED_AUDIT_RUNNING / NOT_FULLY_VISUALLY_ACCEPTED**. The precise PR/tested-source SHA, CI run and artifact must be recorded **after** each new run. Do not promote this document to accepted without reviewing all screenshots, all active screen states and popup transitions. No production, real provider, real user data, SQL production migration or PR merge is authorized.

## 1. Environment / actual reproducibility

- The assistant's ephemeral local container has **Node v22.16.0, npm v10.9.2 and Chromium 144**, but does **not** contain a .NET SDK, SQL Server binaries or Docker; HTTPS Git clone failed because the container could not resolve github.com. **A complete local checkout+running application was therefore NOT performed in that container**. Do not claim otherwise.
- The repository already includes a GitHub-hosted disposable sandbox: workflow `.github/workflows/m01-ci.yml`, `scripts/dev/start-ci-sql.sh`, `scripts/dev/start-e2e-target.py` and `scripts/dev/stop-e2e-target.py`. Each browser job performs GitHub `actions/checkout` of the **PR commit**, installs pinned Node dependencies (`npm ci`), .NET 10, Playwright Chromium, builds API/local operators, starts job-owned SQL Server on 127.0.0.1:14339, migrates/creates fresh `Nexora_Test_<32hex>`, creates synthetic local accounts and launches API+Vite on loopback only. Secret-bearing logs and synthetic account passwords remain in mode-0600 runner temp. No real provider enablement.
- New **UIUX audit extension**: `web/Nexora.Web/playwright.ux-audit.config.ts`, `web/Nexora.Web/e2e/pr4-uiux-audit.spec.ts`, `start-e2e-target.py run-uiux-audit`, workflow CI audit step and separate sanitized artifacts `uiux-audit-<SHA>`. The runner is independent of the legacy browser test result and runs `if: always()` when the owned target is present; audit is **non-blocking** for CI until its probe code is stable. Each original CI test remains required, not silently skipped.
- Actual test data: eight synthetic account personas (SuperAdmin, UserA, UserB, Admin, AdminReader, Restricted, Disabled, Suspended). Audit now also **attempts** isolated UI creation of Bookmarks, a C# Snippet, a Project + dependent Task, and Finance category + explicit-VND manual record using real API/SQL; SQL operator seeds one synthetic Notification through its restricted job-owned fixture command. No real email/push/provider dispatch. Each fixture's **actual success/failure must be read from generated JSON**, never from the presence of seeding code. No blanket data-seeding claim for unimplemented/gated features.
- Browser audit modes: Anonymous, UserA, SuperAdmin, Restricted; desktop1440×900/mobile390×844. The legacy CI independently exercised 320px and768px viewport cases; current new audit does **not yet** cover all variants (dark mode/200% zoom/tablet/physical devices/screen readers), and expanding to all is an explicit follow-up.
- Every target route captures a first-viewport screenshot, visible `h1`, whole-page overflow, JS exception count, unlabeled controls and undersized click targets. Automated axe checks run on all first-level/public routes and **a sample of core FX module routes (11/12/20/24/27/40)** to bound CI duration; other FX module axe coverage must be marked `NOT_RUN_SAMPLED`, never pass. Dialog discovery attempts up to **2 non-destructive buttons per primary route** and **1 per module route**, plus up to5 native tabs. Popups inaccessible without source fixtures/current permission and destructive confirmations are `NOT_EXERCISED`; no Save/Delete/Revoke/Purge/real provider action is blindly clicked during discovery. UserA scope is batched into 25 primary, FX01–FX20 and FX21–FX40 groups to ensure individual progress artifacts.
- Report machine evidence: `web/Nexora.Web/artifacts/uiux-audit/desktop-1440-*.json`, `mobile-390-*.json`, `screenshots/*.png`, sanitized `playwright-junit.xml`. CI artifact expires after 7 days; manually attach relevant safe screenshots/finding summary to durable docs before expiration.

## 2. Qualitative visual observations — grounded in the PRIOR CI artifact (NOT current-head signoff)

Source: earlier sanitized [browser-e2e run 39151552763](https://github.com/hakodev2k/Nexora/actions/runs/39151552763), artifact `12144743847` (which ran `4d9876ac00457f8888b864e0dc63721b5a316eeb`). Six synthetic screenshots were downloaded and manually inspected; these are **previous-code visual evidence**, not a claim that current PR has already been retested. It contains desktop/mobile HOME, desktop/mobile PUBLIC registration validation, desktop/mobile BOOKMARK committed ACK. Crop/stretch in the chat client is not a pixel-accurate rendering of the real mobile viewport: inspect native screenshot/DOM and device viewport before calling any font too small.

| Evidence/observed screen | Assessment | Specific improvement and acceptance |
|---|---|---|
| Home desktop screenshot `playwright-output/.../home.png` | **NEEDS WORK — P1.** Tall module catalog dominated by dozens of FX-code cards and generic “Mở module” CTAs, empty data visible as raw “Available/SystemDisabled” technical state. The title takes 3 lines for a long synthetic email-derived display name; Dashboard layout editor and widgets show loading within the capture. | Group modules by user intent; searchable category list, prioritize common destinations, put developer/debug module inventory under Admin only when policy permits; map technical states to concise user-facing Vietnamese with actionable reason. Clamp/truncate very long display name in heading and expose full display name on profile only. Test initial loader settles and never renders sensitive widget before current source proof. |
| Home mobile screenshot | **NEEDS WORK — P1/P2.** Extremely tall scroll with many repeated low-information cards and several “Tạo mới” blocks; this matches older QA `NQA-003` duplicate Quick Create, verified 4/4 in a different earlier local baseline. Browser screenshot alone does not prove every key-collision root cause. | Exactly one Quick Create per owner/context, stable React sibling keys, task-oriented grouped/mobile collapsible module catalog. Measure CTA discoverability above fold, viewport overflow, focus order and back/forward navigation after owner/layout refresh. Run 320/390/768/1440 and touch/keyboard cases on newest commit. |
| Register desktop validation | **PARTIALLY GOOD.** Clean centered white panel on neutral background, readable hierarchy, visible error banner, distinguishable form fields and large primary CTA. **UX friction:** “Timezone IANA” is a technical term and free-text field, and password guidance is verbose; display name is optional yet long fallback names can overwhelm Home title. | Detect browser timezone and offer searchable timezone select with human-readable labels, explicit override and DST-safe storage. Plain-language explanation and accessible inline field errors with focus to invalid input; help password requirements in concise text. Test 320/390, screen reader and keyboard focus. |
| Bookmarks desktop committed-ACK | **PARTIALLY GOOD.** Clear navigation selection, two-column search/list layout, status acknowledgement is easy to see. **NEEDS WORK:** “Provider boundary” text is engineering jargon and empty-state plus full-width filters dominate initial view; committed ACK confirms save but current item intentionally unavailable. | User-facing copy “Lưu liên kết để đọc lại” / “Chưa có bookmark”, clear primary action and reduce raw implementation terms; distinguish Saved/Unable to show details/Retry original request, preserve safe opaque ACK. Check populated result visual density and keyboard focus. |
| Public / Bookmarks mobile screenshots | **NOT CERTIFIED.** Legacy screenshot shows those states existed under one mobile configuration, not a full multi-breakpoint/low-height/keyboard/touch review. | Audit actual 320/390/768 and full screen/dialog contents before assigning accepted; no automatic “professional UI” score. |
| Bookmarks mobile synthetic screenshot — top header | **P1 / visual usability:** the command-palette CTA wraps across multiple lines and competes for width with PersonalSpace/email text and Logout, pushing page title/action below a tall header. Text may remain legible but navigation density is unnecessarily high. | On mobile use compact search/command icon with tooltip/accessible label and move Logout/email into a protected account menu, keeping keyboard shortcut discoverability on desktop. Check 320px/390px with 200% zoom, long synthetic display/email and focus/tab order. |
| Project/Task current JSX source `web/Nexora.Web/src/App.tsx:3972–3993` | **NEEDS UX REDESIGN — P2, CODE-CONFIRMED.** Project create form exposes a raw **“Tags JSON (mảng)”** input to ordinary users, while “Title”, “Description” and “Load more Project choices” mix English with Vietnamese fields; Task form has numerous always-visible date/reminder/status controls. | Replace raw JSON with typed chips/multi-select, keep exact server data contract, group advanced scheduling in progressive disclosure, consistently localize labels/helper/error copy, show an inline task-Project selector with correct semantic edit-state guarding. Do not remove original source action/owner/ETag rules. |


These findings are **design recommendations**, not evidence of SQL bugs or an approval to change product/security semantics. Colors, typography and legibility require native screenshot review and product design owner sign-off.

## 3. Coverage register — every URL and gated module must receive a disposition

**Probed in new audit if the runner succeeds; until the current run's JSON is inspected the status of every row is `RUNTIME_PENDING`, not `PASS`.** A route screenshot of a disabled/gated module proves only that the unavailable state renders, not the absent feature. For each route inspect empty, populated, loading, error/offline, denied, stale conflict, read-only, archive/trash, canceled dirty, and create/edit/detail/delete dialog *where applicable*. Log `N/A` only if supported by actual UX/feature source.

### Public/auth (6 URLs)

`/login`, `/register`, `/verify-email`, `/password/forgot`, `/password/reset`, `/share/<invalid-synthetic-token>`. Read-only form and negative-token outcomes: labels/validation/readability/focus/restore/link security. Current real browser `m01-identity.spec.ts` failed a registration/verification/login journey on desktop/mobile in the older CI; its real root cause remains unproven until a sanitized new run.

### Application navigation (25 URLs)

| Group | Paths | Expected checks beyond image |
|---|---|---|
| Home/discovery | `/`, `/search`, `/search/saved`, `/favorites`, `/recent`, `/command-palette` | Empty/populated lists, filters/facets, load, safe detail routes, command keyboard, paging, widget layout, quick-create |
| Profile/security | `/settings/profile`, `/settings/security` | Locale/timezone/theme, sessions, password/update flows, privacy, dirty cancel, focus management |
| Inbox/trash | `/notifications`, `/trash` | Unread/selected bulk, safe ACK, source-open current authority, empty/error, archive/restore/purge preview |
| Personal organization | `/finance`, `/bookmarks`, `/snippets`, `/read-later`, `/organize/tags` | Create/edit/archive, valid/invalid submissions, current-data filtering, currency, source unavailable, history |
| Productivity | `/goals`, `/planner`, `/habits`, `/learning/work-log` | Detail/target dialogs, list/day/week state, consistency across source links, history, correct local dates |
| Platform/tools | `/developer/tools`, `/sharing`, `/support`, `/files` | User-friendly jargon, read-only sharing limits, sensitive projection, file/quarantine preview, copy/export restrictions |
| Administration | `/admin/access`, `/admin/modules` | SuperAdmin only, denied User state, action hierarchy, search/bulk filters, no false UI-authority |

### Dynamic module routes (40 destinations)

`/modules/FX01` through `/modules/FX40`. Treat each as a **separate audit row**, including module code, actual installed state and available capability, fallback reason, accessible entry CTA, query/empty/detail/blocked state. Do **not** count a generic fallback card as a complete product module. Module summaries live in `docs/features/README.md` and implementation gaps in `docs/implementation/pr4-feature-gap-review-20261019.md`.

### Popup/component acceptance register (source-identified, needs runtime match)

| Family / React component(s) | Route candidate | Required popup check |
|---|---|---|
| Resource form/confirm | `ResourceFormDialog` and App inline dialogs | Task/Project/Calendar/Document: title, field validation, authorisation preview, destructive confirmation, dirty cancel |
| Document editor/library | `DocumentBodyEditor`, `DocumentTreeDialog`, `DocumentHistory`, `DocumentImportPanel`, `DocumentExportDialog` | Two editor modes, history/version compare, folder/tree hierarchy, import/export preview, close/focus, errors |
| File lifecycle | `FileMetadataDialog`, `FileRenameDialog`, `FileTrashDialog`, `FileRestoreDialog` | Clean/quarantine, original name, version/restore, destructive impact and keyboard escape |
| Sharing | `ShareLinkEditor`, `ShareDisclosurePreview`, `CreatedShareLink` | Audience/expiry/field preview, once-only copy, token safety, revoked/expired/deleted source |
| Dashboard | `DashboardLayoutEditor`, `DashboardDrilldownDialog`, `DashboardQuickCreate` | Layout refresh/persist, duplicate CTA, widget provider failure and one panel at a time |
| Automation | `AutomationExecutionDialog`, `AutomationLifecycleDialog`, `AutomationProjectPicker`, `AutomationPurgeDialog`, `AutomationScheduleDialog`, `AutomationRunPanel` | **Synthetic-only/disabled** flow, no live dispatch; schedule, replay, preview and protected failure copy |
| Goals/Learning | `GoalTargetManager`, `Goal*History`, `SkillEvidence`, `WorkLogHistory`, `CourseScreen` | Numeric/Boolean target validation, evidence, history, milestone zero, stable date/progress, source unavailable |
| News/Career | `NewsArticleDialog`, `CareerAppointments` and Career/News views | Source loading, read-only article content, text sanitization, Calendar create/link, close/back keyboard |
| Admin/Support/Notifications | inline confirm/detail dialogs in `App.tsx` and dedicated screens | Exact role-gated confirm/revoke, overflow and safe redaction; do not reveal protected owner payload |

**Partial coverage caveat:** Generic auto-scanning cannot open dialogs that require a selected record, correct permission, multi-step state or populated list; these remain `NOT_EXERCISED` until a seeded scenario and explicit assertion are recorded. A popup counted by generic click is only a **visible panel appearance**, not a complete form validation or destructive flow check. Dedicated scenario tests are required per family.

## 4. Scoring rubric for human screenshot reviewer (per page and popup)

For each target assign 0=poor, 1=acceptable with issues, 2=professional and clear **based on the inspected output**, not on a tool claiming aesthetic quality. Separate scores: hierarchy/visual rhythm, consistency/design tokens, task findability/CTA labels, copy/localization/no jargon, data density and empty/loading/errors, mobile/touch/reflow, accessibility/keyboard/focus, confirmation/recovery/privacy. Record `N/A` only with source reason, and **status + screenshot path + exact viewport/persona/revision**.

Priorities: **P0** secret/privacy/role/action/critical destructive flow or inaccessible authentication; **P1** core journey blocked, duplicated primary action, overflow clipping essential action, lost draft; **P2** confusing hierarchy/copy/labels and unnecessarily many steps; **P3** minor spacing/visual consistency. A recorded overflow or axe finding is a reproducible symptom, not an automatic full WCAG certification or artistic-quality grade.

### Per-target audit row schema

`RouteOrDialogId | Persona | Viewport | CurrentState (empty/populated/loading/error/etc) | ProductScope (active/gated) | UI_score | UX_score | AccessibilityStatus | ScreenshotArtifact | StepsToRepro | FindingId/Priority | DesignSuggestion | ExactSourceSha | RevalidationRun | IndependentVisualReviewer | Verdict`.

Default rows remain `NOT_RUN/NEEDS_MANUAL_VISUAL_REVIEW` until current artifacts populate them. Do not write fictitious grades for unobserved pages.

## 5. Explicit go/no-go and next corrective implementation

- Review actual UIUX Actions artifacts and JUnit for the latest SHA. If absent or blocked, record failure; do not present 40 FX as fully inspected. Visual findings from the previous CI are provisional.
- Fix duplicated DashboardQuickCreate React sibling identity and horizontal overflow, then rerun at320/390/768/1440. Confirm exactly one create CTA and no clipping at modal/keyboard.
- Redesign giant raw module catalog into purpose-oriented navigation while preserving server-side capability/disabled state; minimize visible technical terms (“Provider boundary”, FX codes, “SystemDisabled” for regular users), improve long display-name fallback, and simplify timezone choice.
- Extend approved synthetic fixtures to ≥2 populated examples per **active** module before judging list sorting/density, detail/edit/delete and error states; do not auto-create records in gated FX28 or trigger FX30/34/35 real worker effects. Use source-owned HTTP commands/SQL operator with unique isolated owner IDs, not broad manually injected SQL bypass.
- Full QA requires reviewer-backed screenshot judgments for all populated screens and all reachable dialogs across roles/viewport/error/lifecycle, plus independent authorization/SQL/real E2E tests. An automated audit or docs commit does **not** resolve existing CI failures or approve merging.

**Release disposition: KEEP PR DRAFT / UIUX ACCEPTANCE NOT COMPLETE.**
