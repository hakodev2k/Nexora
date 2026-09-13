# Draft PR description — PR #4 review hardening

> Unposted draft. Do not publish or merge as part of this task.

## Scope

This continuation stays on `impl/m01-s00-scaffold` for PR #4 and applies
bounded local source fixes for the revalidated F01–F22 review input. It keeps
the modular-monolith/SQL Server authority model and does not claim R1 or
production readiness.

Implemented source areas include:

- source-action-aware Trash restore/purge, explicit Task Calendar/Planner/
  Reminder participants and owner-scoped lifecycle checks;
- Task projection read guards, canonical reminder reconciliation, optional
  priority handling, IANA/all-day Calendar boundaries and safe typed resource
  detail links;
- durable reminder claim/lease/retry/final disposition with current account,
  PersonalSpace, module and source checks; local InApp projection only;
- upload owner/handle/attempt scoping and durable exact-path file cleanup;
- draft/base/server-version conflict handling, accessible dirty-leave actions,
  bounded CSRF refresh and stable idempotency keys for repaired UI intents;
- shell theme binding, safe internal resource return-after-login, bounded
  keyset pagination/load-more for Projects/Tasks/Calendar, and current
  traceability/QA handoff documentation.

F09 remains unavailable because no approved local token-delivery adapter/inbox
contract exists. F16/F18/F20/F21 and parts of F10 remain explicitly partial or
gated where the full locale catalog, architecture work, committed CI/runtime
evidence, cleanup crash-boundary proof or independent review is not present.
Search continuation remains outside the current bounded FX25 search slice.

## Revision and migration ceiling

- Branch: `impl/m01-s00-scaffold`
- Repository `HEAD`: `fcb1f75fd8dccfa0ba1f15b49569e04c694569ce`
- Review baseline: same head; base `8782f46be51f4b0f3cb54f0f0f7a06eae3b0d3e3`
- Working tree: uncommitted changes on the above HEAD
- Authorization: `DEC-20260909-014` for bounded local slice implementation
- New migration: `database/migrations/20260913_0025_review_hardening.sql`
- Migration execution: **not run**; no existing migration was changed
- Real providers/OAuth, production secrets/data, paid services and external
  destructive actions: not used

The migration is forward-only. It adds nullable Task priority support without
rewriting existing P3 data, reminder dispatch lease/retry columns, upload
attempt columns and `files.StorageCleanup`. Deployment requires the normal
backup/checksum/journal process and an authorized disposable SQL environment.
Application rollback should leave additive columns in place; schema rollback
requires an approved forward compensating migration after impact review.

## Evidence

Run locally in Windows PowerShell at the working-tree revision:

- `dotnet build src/Nexora.Api/Nexora.Api.csproj --no-restore` — **Pass**, exit
  0, 0 warnings, 0 errors.
- `npm run build` from `web/Nexora.Web` — **Pass**, exit 0; TypeScript and Vite
  production bundle completed.
- `git -c safe.directory='D:/Projects/ASP_NET_Core_Developer_2026/Nexora'
  diff --check` — **Pass**, no whitespace errors; only normal line-ending
  warnings.

These are implementation-error checks only. No test, browser, SQL migration,
readiness, worker, storage fault-injection, provider or functional QA command
was run under the current code-only amendment. CI evidence from an older SHA is
not used as evidence for these edits.

## Review and acceptance gates

Please use [`pr4-review-fix-brief-20260913.md`](pr4-review-fix-brief-20260913.md)
for the F01–F22 row-by-row requirement/action/AC binding, source references,
status, exact human QA action/input/expected result and remaining gate.

Still required before any acceptance or enablement:

- independent security, migration, background-job and cross-module review;
- authorized SQL migration/readiness/FK/isolation/concurrency/fault-injection
  verification using synthetic local data only;
- human browser/accessibility/locale/theme/timezone/dirty-conflict and
  idempotency QA;
- completion of the missing F09 delivery contract and the explicitly partial
  F16/F18/F20/F21/F10 areas; search continuation remains contract-scoped.

Do not label this PR R1-complete, runtime-accepted or production-ready. Do not
merge PR #4 in this continuation.
