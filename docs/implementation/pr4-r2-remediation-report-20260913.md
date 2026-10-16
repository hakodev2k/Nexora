# PR #4 R2 remediation report — current review state

Run date: 2026-09-14. This is a reviewable implementation/evidence artifact;
it is not a PR comment, approval, merge, deployment or production-readiness
claim. The working tree is intentionally left uncommitted.

## Task brief and authority

| Field | Value |
| --- | --- |
| Repository / PR / branch | `hakodev2k/Nexora` / PR `#4` / `impl/m01-s00-scaffold` |
| Normative base | `main` = `8782f46be51f4b0f3cb54f0f0f7a06eae3b0d3e3` |
| Actual reviewed head | `6d5b58dbac330b0d0d9aceb9e0b2f4695b423316` (matches the requested R2 head) |
| Parent / comparison point | `fcb1f75fd8dccfa0ba1f15b49569e04c694569ce`; comparison only, no reset or checkout |
| Authorization used | User-requested repair on the existing PR; current main decision `DEC-20260909-001` only: M01 S00–S11, backend/frontend scaffold and local scripts |
| Scope explicitly not used | PR-only `DEC-20260909-014`, full R1, production, real providers/OAuth/webhooks, paid services, real secrets/data, merge or public launch |
| Amendment | Code-only implementation pass from `AGENTS.md`: no new/changed/executed tests, fixtures, demo data or functional runtime evidence; compile/build/typecheck/lint only |
| Main story/AC binding | M01-S00..S11; M01-AC00..AC11, especially AC02/03/04/05/07/08/09/10/11 |
| Main goals | `NXG-M01-01..05`; relevant system goals `NXG-SYS-01/03/05/06/08/10/11/13/14/16`; module goals `NXG-FX01-G01..G03`, `NXG-FX02-G01..G03`, `NXG-FX03-G01..G03`, `NXG-FX06-G01..G03`, `NXG-FX09-G01..G03` |
| Routed rules | Technical Lead core; architecture/module boundaries/transactions; backend authorization/background/security/idempotency/input validation; frontend accessibility/forms/security; database migration/concurrency; security identity/secrets/privacy; verification/evidence and owner-isolation gates |
| Evidence environment | Windows, PowerShell, .NET SDK available for compile-only builds, Node/Vite available; no SQL Server/database migration, browser QA, CI run or independent reviewer executed |

The complete routed rule set and the authority mapping are recorded in
[`pr4-r2-authority-manifest-20260913.md`](pr4-r2-authority-manifest-20260913.md).
Normative files were read from `main` with `git show main:<path>`. The PR's
`docs/requirements/12-owner-decisions-local-e2e-implementation.md` is absent
from `main`; it was not used to approve post-M01 work.

“Blocked by scope” below is deliberate, not a missing implementation attempt:
main says those business slices are not approved by `DEC-20260909-001`. They
remain source-confirmed findings with repair design and human-QA cases, but
implementing them here would self-expand the approved product scope.

## Changes implemented in the approved M01 slice

- `SqlSelfCapability` now separates User/SuperAdmin own-resource baseline from
  Admin SELF explicit Allow/no Deny, evaluates current account/space/module/
  dependency state, and treats `RegistrationEnabled` only as a future
  verification default. `ActionGrantPolicy` has an exact M01 action allowlist
  and an explicit Admin-grantable allowlist.
- Request receipts now support validated anonymous-session binding, safe result
  status/body replay for approved identity/admin operations, keyed credential
  proof digests and no raw credential/token storage. The binding signing secret
  is stable across API restarts. Admin self-demotion returns
  `204` and the UI clears the privileged projection before re-authentication.
- Registration/resend/reset write an authenticated encrypted local delivery
  envelope in the SQL intent transaction. A bounded local worker claims with a
  valid read-committed/`READPAST` pattern, leases/fences work, handles expired
  exhausted claims and delivers only to a private operator CLI capture. No HTTP
  mailbox or raw-token application log exists.
- New password hashes use the versioned ASP.NET Identity `PasswordHasher` at
  the M01 PBKDF2-HMAC-SHA512 cost. The old local format is accepted only for a
  bounded successful-login/reauthentication rehash path.
- CSRF recovery has a bounded single-flight refresh/retry and a separately
  signed stable anonymous-session binding. M01 identity/profile/admin/settings
  mutation keys are retained for the same logical intent; the shell has vi/en
  resources for the M01 surface and keeps persisted theme binding.
- Current authority, traceability and PR-description drafts now pin the real
  base/head. Historical PR documents are explicitly labelled and are not
  acceptance evidence.

## R2 disposition at the actual head

Status vocabulary: `Fixed in source; build verified; runtime pending`,
`Fixed in evidence`, `Partial — M01 portion fixed; post-M01 portion blocked by
scope`, `Confirmed at actual head; blocked by scope`, and `Blocked by runtime`.
Build evidence is compile/static evidence only; it never means runtime accepted.

| ID | Requirement / action / AC / goal | Status and source evidence | Files / change | Build/static evidence | Human QA and remaining gate |
| --- | --- | --- | --- | --- | --- |
| R2-01 | `DEC-20260909-001`; M01-S00..S11; M01-AC11; `NXG-M01-01..05`; all current M01 action rows | **Fixed in evidence.** `main` contains DEC-001 and no DEC-014; PR-only authority claims are superseded. | `pr4-r2-authority-manifest-20260913.md`; historical brief/matrix/PR draft banners | SHA/ref/source review | Owner verifies every claim against main/head; no scope expansion or merge gate is authorized. |
| R2-02 | M01-S07/S08; M01-AC07/08; `access.user.read`, `access.permission.read`, `access.change.read`, `access.role.set`, `access.permission.set`, `access.entitlement.set`; `NXG-FX02-G01/G03`, `NXG-SYS-03` | **Fixed in source; build verified; runtime pending.** Admin no longer gets ambient SELF; absent/Deny fails, Allow still needs current prerequisites. User and SuperAdmin branches remain distinct. | `SqlSelfCapability.Evaluate`; `ActionGrantPolicy.ApprovedM01Actions/AdminGrantableActions`; `SqlAdminAccessService` response boundary | Infrastructure/API/Bootstrap Release builds exit 0 | Admin Unset/Deny/Allow and prerequisite matrix; User baseline; SuperAdmin owner scope; SQL/revoke race; independent security review Pending. |
| R2-03 | M01-S09; M01-AC09; `modules.policy.defaults`; `NXG-FX03-G02/G03` | **Partial — M01 portion fixed; post-M01 portion blocked by scope.** Current SELF evaluation no longer gates existing users on `RegistrationEnabled`; verification snapshot still uses it. Existing post-M01 direct consumers are not reworked under DEC-001. | `SqlSelfCapability`; `SqlIdentityService.GrantReadyModules`; no bulk code deletion | Source review and Release build exit 0 | Existing grant survives default off; new verification snapshot; SystemEnabled off; worker policy. SQL/runtime pending. |
| R2-04 | M01-S10/M01-AC10; `notifications.dispatch.deliver`; `NXG-M01-05`, `NXG-FX06-G02`; reminder/file queue contracts are post-M01 | **Confirmed at actual head; blocked by scope.** `SqlReminderService` and `SqlFileService` still claim with `Serializable` + `READPAST`; new local identity worker's valid pattern does not repair those queues. | No post-M01 queue patch; `AccountMessageDeliveryWorker` is limited to M01 account delivery | M01 worker compiles; no SQL proof | SQL Server isolation/RCSI, two workers, poison item and no duplicate; no queue enablement. |
| R2-05 | M01-S10/M01-AC10; `NXG-M01-05`; reminder/storage recovery contracts are post-M01 | **Confirmed at actual head; blocked by scope.** Attempts `< 8` plus claim crash can leave Pending at budget with no recovery in those post-M01 queues. | No post-M01 patch | Static source confirmation only | Crash after attempt 8, lease expiry, fencing and final disposition in real SQL; no runtime evidence. |
| R2-06 | FX-14-BR-001/FX-14-AC-001; reminder configuration actions; `NXG-FX14-G01/G02` | **Confirmed at actual head; blocked by scope.** Task reconciliation and FX14 upsert have different retry/lease reset semantics. | No Productivity/Reminder repair under DEC-001 | No affected build claim beyond overall compile | Exhausted schedule rescheduled through both entrypoints; old lease cannot complete new revision; blocked slice approval. |
| R2-07 | FX-14-BR-001; FX-14-AC-001; reminder configuration action; `NXG-FX14-G01` | **Confirmed at actual head; blocked by scope.** Task-form reconcile allows expired values for new configuration instead of distinguishing historical preservation. | No post-M01 Task form patch | Static finding retained | New past exact rejected; future-before-start accepted; historical title edit retained; SQL/browser pending. |
| R2-08 | FX-20-BR-003; FX-20-AC-004; `documents.page.save/publish/archive`; `NXG-FX20-G02` | **Confirmed at actual head; blocked by scope.** Existing transition handler can apply server DTO over an unsaved draft; M01 identity dirty handling does not approve Documents. | No Document transition patch | Frontend build does not prove this journey | Publish/Archive Save/Discard/Keep editing, 412/422/network, revoke and focus behavior; affected slice approval required. |
| R2-09 | FX-25-BR-001; FX-25-AC-001; `discovery.favorite.read/add/remove`; `NXG-FX25-G01/G03` | **Confirmed at actual head; blocked by scope.** Favorite route producer emits compact UUID while App/router expects dashed UUID. | No Discovery route patch | Frontend build only | Typed deep-link, reload/back/forward, foreign/revoked/trashed/invalid route; affected slice approval required. |
| R2-10 | FX-13-BR-002; FX-13-AC-003; calendar event actions; `NXG-FX13-G02` | **Confirmed at actual head; blocked by scope.** Browser-offset conversion is not a profile IANA resolver and does not define gap/overlap behavior. | No Calendar timezone patch | Frontend type/build only | DST gap/overlap, browser/profile zone mismatch, all-day end-exclusive; blocked slice approval. |
| R2-11 | FX-13-BR-002/003; FX-13-AC-003/004; calendar view actions; `NXG-FX13-G02/G03` | **Confirmed at actual head; blocked by scope.** Visible range is anchored to today with no complete period navigation. | No Calendar anchor/pagination patch | Frontend build only | +90-day event, period navigation, boundary overlap, empty state; blocked slice approval. |
| R2-12 | FX-11-AC-004 and Task source contract; `projects.project.read`; `NXG-FX11-G02`, `NXG-FX12-G02` | **Confirmed at actual head; blocked by scope.** Task picker consumes only the first 25 Project rows and does not use a source-picker continuation. | No Project picker patch | Frontend build only | 26+ projects, select/edit item 26, filtering/load-more/no duplicate/owner isolation; blocked slice approval. |
| R2-13 | FX-04-G01/G02; sharing link read projection; Task optional-priority contract | **Confirmed at actual head; blocked by scope.** Shared reader still `GetString` on nullable Task Priority. | No Sharing consumer patch | Infrastructure compile does not exercise nullable SQL row | NULL priority through shared projection without defaulting; Sharing remains gated. |
| R2-14 | Main M01 API contract: write Allow does not imply read; admin commit returns 204 if caller loses projection; `NXG-FX02-G01`; post-M01 Task/Document response rules | **Partial — M01 portion fixed; post-M01 portion blocked by scope.** Admin self-demotion now returns 204 and admin receipts replay only stored safe metadata; Productivity/Document mutation responses remain unpatched. | `SqlAdminAccessService.SetRole/SetActionGrant/SetModuleGrant/CheckReceipt`; `AdminAccessEndpoints`; `App.tsx` admin role handler | Backend and frontend Release builds exit 0 | Write Allow + Read Deny success/error/replay for every approved consumer; no private body/previous values; independent security review Pending. |
| R2-15 | M01 idempotency receipt contract; M01-AC02/04/10; `NXG-SYS-03/06`; FX08 restore/purge is post-M01 | **Partial — M01 identity/admin safe replay fixed; Trash/business replay blocked by scope.** Receipts now retain safe result status/body; verify and reset-confirm replay recheck current account/space state; Trash still reads source before its source-authority replay boundary. | `SqlRequestReceiptStore`; `SqlIdentityService`; `SqlAdminAccessService` | Infrastructure/API builds exit 0 | Lost response/same key/body/original safe result; changed body 409; revoke/disabled source; Trash receipt after purge; SQL/browser pending. |
| R2-16 | FX-07-BR-005; `NXG-FX07-G03`; Files gate | **Confirmed at actual head; blocked by scope.** Owner/handle validation was present in PR, but reservation/attempt metadata remains in a transaction crossing file I/O. | No Files recovery redesign | No runtime claim | Crash after reservation/copy/move/finalize, restart reconciliation and cross-owner path protection; FX07 gate stays closed. |
| R2-17 | FX-12-BR-006; FX-13-BR-006; `NXG-FX12-G02`, `NXG-FX13-G02` | **Confirmed at actual head; blocked by scope.** Calendar projection collapses Task statuses to manual-event statuses. | No Productivity/Calendar projection patch | Infrastructure build only | NotStarted/InProgress/Completed/Skipped vs manual event states; blocked slice approval. |
| R2-18 | M01-S05; M01-AC05; `settings.preference.read/update`; `NXG-FX09-G02/G03`, `NXG-M01-03` | **Fixed in source for M01 surface; build verified; runtime pending.** M01 auth/profile/settings/admin/shell labels use vi/en resources and theme binding; untranslated post-M01 screens are not claimed. | `web/Nexora.Web/src/i18n.ts`, `App.tsx`; profile locale shell | `npm run build` exit 0 | vi→en labels/errors/nav after reload/login, theme/system/contrast/focus, profile timezone unchanged; browser QA pending. |
| R2-19 | Database identity convention; M01-AC10/11; `NXG-SYS-14` | **Partial.** New identity/access writers distinguish `ActorUserId` and target `OwnerUserId`; no legacy inventory or correction migration was run/designed from ambiguous rows. | `SqlIdentityService`, `SqlAdminAccessService`; report records compatibility gap | Build/source review only | Synthetic correct/legacy/ambiguous inventory, PersonalSpace→User provenance, append-only correction; no blanket UPDATE. |
| R2-20 | M01-S02/S04/S10; M01-AC02/04/10; identity account actions; `NXG-M01-02/05`, `NXG-FX01-G01/G02` | **Fixed in source; build verified; runtime pending.** Metadata-only sink replaced by encrypted durable local capture/worker with purpose/expiry/attempt/lease boundaries; no fake Sent/Delivered or public mailbox. | `LocalAccountMessageSink`; `LocalAccountMessageEnvelopeProtector`; `AccountMessageDeliveryWorker`; `20260913_0026_identity_local_delivery.sql`; Local CLI; API composition | Infrastructure/Local/API/Bootstrap and compile-only test-project builds exit 0 | Clean local register→capture→verify/login and reset→capture→confirm; expiry/resend/reuse/race/session invalidation; SQL/browser/runtime and G05/G06 pending. |
| R2-21 | Modular-monolith boundary; `NXG-SYS-04`; M01 contracts; post-M01 cross-module consumers | **Confirmed at actual head; blocked by scope.** Cross-module SQL and large `App.tsx` remain in existing post-M01 code; this pass only extracted/centralized M01 policy and UI locale/CSRF paths. | M01 changes in `SqlSelfCapability`, identity receipt/delivery and `i18n.ts`; no broad rewrite | Builds cannot prove boundary compliance | Boundary/dependency review and consumer contract matrix; no microservices/ORM rewrite; independent architecture review Pending. |
| R2-22 | `NXG-SYS-14`; M01-AC11; `docs/goals/05-task-and-evidence-template.md` | **Fixed in evidence.** Current artifacts pin base/head and label old PR claims historical; they do not claim build as acceptance. | `pr4-r2-authority-manifest-20260913.md`; this report; current PR draft; historical banners | Exact revision and command log below; no CI claim | Owner verifies links/ref/status at final commit; CI/runtime/independent review remain open. |
| R2-23 | M01-S02/S03/S04; M01-AC02/03/04; versioned ASP.NET Identity PasswordHasher contract; `NXG-M01-02` | **Fixed in source; build verified; runtime/benchmark pending.** Custom format is no longer used for new hashes; bounded legacy parser returns rehash-needed and login/reauth upgrades after successful proof. | `Pbkdf2PasswordHasher`; `SqlIdentityService`; `PasswordHashService`; Identity Core package refs | Infrastructure/Local/API/Bootstrap builds exit 0 | New/legacy/wrong/malformed/rehash/benchmark and no secret leakage; SQL compatibility runtime pending. |
| R2-24 | M01 public idempotency/CSRF contract; M01-AC02/04; identity public actions; `NXG-SYS-03/05/06` | **Fixed in source; build verified; SQL/browser pending.** Anonymous receipt subject uses HMAC of a validated separately signed opaque session binding; the API signing secret is configured and stable across restart, CSRF rotation keeps the namespace, and credentials are not replayable. | `CsrfTokenService`; `IdentityEndpoints`; `SqlRequestReceiptStore`; `api.ts` | API/Infrastructure and frontend builds exit 0 | Two anonymous sessions same key are isolated; same session replay safe result; changed body 409; CSRF TTL/multitab/restart; no credential/cookie replay. |

## F01–F22 regression disposition

This table preserves the original finding set separately from R2. A source
repair or compile result does not promote a row to runtime-verified.

| Finding | Current status at actual head | Evidence / residual |
| --- | --- | --- |
| F01 | **Confirmed; post-M01 scope-blocked** | Trash source-action/lifecycle authorization still needs a source participant. |
| F02 | **Confirmed; post-M01 scope-blocked** | Calendar/search projection source lifecycle/revoke repair not implemented. |
| F03 | **Confirmed; post-M01 scope-blocked** | Purge dependency participant/FK ordering not implemented. |
| F04 | **Confirmed; post-M01 scope-blocked** | Canonical reminder/new-vs-historical policy not implemented for Productivity. |
| F05 | **Confirmed; post-M01 scope-blocked** | Reminder/file queue fairness/retry recovery not implemented. |
| F06 | **Partial; post-M01 scope-blocked** | M01 account/space current guard exists in identity delivery; reminder effect path still needs repair. |
| F07 | **Confirmed; post-M01 scope-blocked** | Document conflict/dirty transition remains outside DEC-001. |
| F08 | **Confirmed; post-M01 scope-blocked** | Files owner/handle source was reviewed; durable attempt recovery not implemented. |
| F09 | **Fixed in source; runtime pending** | Local operator capture is available; no SQL/browser functional run. |
| F10 | **Partial; M01 source/build verified, runtime pending** | M01 identity/profile/admin/settings key retention and safe replay improved; all post-M01 consumers remain. |
| F11 | **Fixed in source; runtime pending** | Bounded CSRF single-flight/one retry in API and upload fetch; no multitab/TTL QA. |
| F12 | **M01-compatible; post-M01 mismatch scope-blocked** | M01 64 KiB cap retained; Document 1 MiB path not changed. |
| F13 | **Already fixed at PR head; consumer regression blocked** | Core nullable Priority migration/code exists in PR; Sharing consumer remains unsafe. |
| F14 | **Confirmed; post-M01 scope-blocked** | Profile IANA/DST/all-day Calendar repair not implemented. |
| F15 | **Confirmed; post-M01 scope-blocked** | Typed resource deep-link repair not implemented. |
| F16 | **Partial fixed in M01 source; runtime pending** | Theme and M01 vi/en shell resources applied; full R1 localization not claimed. |
| F17 | **Confirmed; post-M01 scope-blocked** | Server cursors/range/source picker not implemented for affected lists. |
| F18 | **Confirmed; post-M01 scope-blocked** | Gradual boundary refactor remains backlog. |
| F19 | **Partial** | New writer ID semantics corrected; legacy audit inventory/provenance remains. |
| F20 | **Fixed in evidence** | Historical matrix/brief/draft are labelled; current artifacts pin actual refs. |
| F21 | **Confirmed; post-M01 scope-blocked** | Durable file cleanup tombstone/worker not implemented. |
| F22 | **Partial; post-M01 scope-blocked** | M01 does not include Task/FX14 reminder canonicalization; current two-source gap remains. |

## Verification log

All commands below were run against the uncommitted working tree on head
`6d5b58dbac330b0d0d9aceb9e0b2f4695b423316` unless noted otherwise.

| Command | Result |
| --- | --- |
| `git rev-parse --abbrev-ref HEAD` | `impl/m01-s00-scaffold` |
| `git rev-parse HEAD` | `6d5b58dbac330b0d0d9aceb9e0b2f4695b423316` |
| `git show-ref refs/heads/main refs/remotes/origin/main refs/remotes/origin/impl/m01-s00-scaffold` | main/origin-main at `8782f46…`; origin implementation at `6d5b58d…` |
| `git fetch --prune origin main impl/m01-s00-scaffold` | Exit 0 after approved repository-metadata elevation; refs matched, no source reset/overwrite |
| `dotnet restore src/Nexora.Api/Nexora.Api.csproj src/Nexora.Bootstrap/Nexora.Bootstrap.csproj src/Nexora.Infrastructure/Nexora.Infrastructure.csproj src/Nexora.Local/Nexora.Local.csproj tests/Nexora.UnitTests/Nexora.UnitTests.csproj tests/Nexora.IntegrationTests/Nexora.IntegrationTests.csproj --force-evaluate --ignore-failed-sources` | Initial sandbox attempt could not access the user NuGet config; rerun with approved elevation exited 0. Package locks were consequently refreshed. |
| `dotnet build src/Nexora.Infrastructure/Nexora.Infrastructure.csproj --configuration Release --no-restore` | Exit 0; 0 warnings, 0 errors |
| `dotnet build src/Nexora.Local/Nexora.Local.csproj --configuration Release --no-restore` | Exit 0; 0 warnings, 0 errors |
| `dotnet build src/Nexora.Api/Nexora.Api.csproj --configuration Release --no-restore` | Exit 0; 0 warnings, 0 errors |
| `dotnet build src/Nexora.Bootstrap/Nexora.Bootstrap.csproj --configuration Release --no-restore` | Exit 0; 0 warnings, 0 errors |
| `dotnet build tests/Nexora.UnitTests/Nexora.UnitTests.csproj --configuration Release --no-restore` | Exit 0; compile-only, 0 warnings, 0 errors; tests were not run |
| `dotnet build tests/Nexora.IntegrationTests/Nexora.IntegrationTests.csproj --configuration Release --no-restore` | Exit 0; compile-only, 0 warnings, 0 errors; tests were not run |
| `npm run build` in `web/Nexora.Web` | Exit 0; `tsc -b` + Vite 6.4.3, 31 modules |
| `git diff --check` | Exit 0 for the uncommitted working diff (Git also printed normal LF→CRLF normalization warnings). |
| `git diff --check main..HEAD` | Exit 2 with the PR range's existing trailing-whitespace/new-blank-line diagnostics; no whole-repository line-ending normalization was performed. This is not functional evidence. |

Not run: `dotnet test`, unit/integration/E2E/browser tests, SQL Server,
migration runner, Docker/services, browser/manual QA, provider/OAuth/network
worker, CI workflow, production data/secrets, and independent review. The
repository verification scripts were not run blindly because their contract
can invoke functional/runtime checks. No test pass, SQL constraint, queue,
concurrency, DST, security or usability acceptance is claimed from these
builds.

## Migration, recovery and rollback note

`database/migrations/20260913_0026_identity_local_delivery.sql` is a new
forward-only migration. It adds nullable authenticated `DeliveryEnvelope`,
delivery lease columns and a delivery index to the existing
`identity.AccountMessageIntent`; existing migrations were not edited and this
migration was not executed. `20260910_0017_favorites_refs.sql` already provides
the receipt safe-result columns used by the source.

Before local enablement, the owner must apply the ordered migration runner to
an empty or approved synthetic upgrade database and inspect its journal/checksum.
If deployment must be backed out, keep the added nullable columns and roll the
application forward or restore a database backup; do not edit an applied SQL
file, drop columns, delete receipts, or reset the migration journal. Preserve
the stable local envelope key while pending deliveries are being recovered.
Any orphaned/expired local capture is limited to the exact message file and is
not a reason to delete a directory or unresolved path.

## Human-owner QA handoff — F01–F22

These are action/input/expected-result cases, not executed records. Use only
synthetic local SQL/browser data and the approved captured/simulated transport.

| ID | Action / input category | Expected result |
| --- | --- | --- |
| F01 | Restore/purge Task/Project mixed batch; source action revoke/disable between preview and commit | Fail closed before mutation; no success audit/receipt on deny; valid source actions are checked per participant. |
| F02 | Task active→Trash→restore; Calendar/Search/Favorites; disable FX12/FX13 and revoke source | Trashed Task is hidden; source revoke/disable removes projection exposure; restore refreshes only after current authority. |
| F03 | Purge Task with no refs, Calendar projection, Planner pin, independent ref; forced SQL failure | Detach/block exactly by contract; ordered transaction rolls back all rows on failure; no orphan FK. |
| F04 | Reminder preset/exact before Start, future exact, historical expired while editing Title, complete/skip | New valid schedule accepted; historical expired edit preserved; no old alert; ETag/history correct. |
| F05 | 25 blocked + one eligible, poison item, transient SQL, restart, two workers | Eligible item is not starved; bounded retry/final disposition; no duplicate logical notification. |
| F06 | Disable/delete/suspend account or PersonalSpace immediately before and during dispatch | No new effect; stale alert is not replayed after re-enable; current account/space authority wins. |
| F07 | Dirty Document then Publish/Archive; choose Save/Discard/Keep editing; inject 412/422/network/revoke | Draft is preserved until explicit choice; failed save does not transition; confirmed revoke clears protected payload. |
| F08 | Wrong owner/handle, replay, concurrent same-session upload, cancellation cleanup on Windows/Linux | No staging I/O outside the owned attempt; concurrent attempt is not deleted; cleanup is exact-path and bounded. |
| F09 | Register→capture token→verify→login; reset→capture→confirm; resend/expiry/reuse/race | Correct purpose/TTL token reaches only local operator capture; old token invalidates; no raw token in app log/public HTTP. |
| F10 | Lost response then same logical mutation key; same key changed body; revoke before replay | One effect; original safe result only after current authorization; changed body 409; revoke denies. |
| F11 | CSRF TTL, two tabs, API restart, lost mutation response | Single bounded refresh; no duplicate effect or infinite retry; idempotency key is retained. |
| F12 | Payload below/at/above endpoint cap; UTF-8/escaped JSON; UI draft | 413 at intended cap with bounded body handling; draft remains; M01 cap is not globally raised. |
| F13 | Task create/edit/filter with no Priority plus existing P3 | NULL is selectable/filterable/sorted by explicit rule; existing P3 remains P3; all consumers read NULL safely. |
| F14 | Two browser/profile timezones; DST gap/overlap; overnight/all-day/multi-day | Profile IANA conversion is deterministic; gap is explained; overlap is explicit; all-day uses date-only exclusive end. |
| F15 | Favorite/Search open, refresh/back/forward, foreign/revoked/trashed/invalid return URL | Correct typed item opens; unsafe/foreign route fails closed without payload or silent no-op. |
| F16 | vi↔en, reload/login/owner switch, light/dark/system, browser timezone mismatch | M01 labels/errors/nav and date formatting follow locale; theme/focus/contrast hold; locale does not alter currency/timezone/instant. |
| F17 | More than one page, equal timestamps, insert/delete between pages, old event in visible range | Stable cursor/range returns complete visible data; load-more/partial/empty states are honest; no unbounded query. |
| F18 | Boundary/dependency scan plus source revoke across every consumer | Direct internal access is identified; source-owned participant/authority is consistent; no circular dependency. |
| F19 | Correct, known legacy and ambiguous audit rows with distinct User/PersonalSpace IDs | New audit fields are semantically correct; mapped legacy provenance is append-only; ambiguous rows remain unresolved, never guessed. |
| F20 | Inspect every report link/ref at final SHA | Current evidence points to actual base/head; build is not labelled acceptance; stale historical rows are visibly superseded. |
| F21 | Inject failure after metadata delete, receipt commit, file delete, process crash/restart | Durable exact cleanup intent survives; binary is deleted last or remains observable/retriable; no cross-owner deletion. |
| F22 | Create/edit/remove/preset reminder after migration; reload Task and Reminder views; dispatch | One canonical configuration/revision, one dispatch, source history present; no Task field/entity divergence. |

## Current unposted PR description

The draft is [`pr4-r2-description-draft-20260913.md`](pr4-r2-description-draft-20260913.md).
It is intentionally not posted. It states the M01-only authorization, exact
SHA, migration ceiling, build-only evidence, runtime/independent-review gates
and the post-M01 scope-blocked backlog. No R1-complete, CI-green,
production-ready or merge claim is made.
