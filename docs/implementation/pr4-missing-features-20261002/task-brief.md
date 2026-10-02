# PR4 missing features — task brief

User authorization: attached Pasted text(3).txt explicitly authorizes implementation, isolated synthetic SQL/API/browser tests, commits and push to the existing PR4 head. No merge, new PR or production deployment.

Requirements: origin/main at 8782f46be51f4b0f3cb54f0f0f7a06eae3b0d3e3, read independently using git show/archive origin/main. Implementation starts at PR4 b049b60702f1d8f7a501bdfc555a8dadf95a7810 on impl/m01-s00-scaffold; isolated checkout task/pr4-missing-api-20261002. Existing other checkouts and their uncommitted work are untouched.

QA snapshot: Nexora-Full-Scope-QA-2026-10-01.md, source 221603430c248013fb7912a0cd1c6c224aed294b. Original missing handler groups: 15. Current: 13. Time/Focus are Partial since the snapshot; other target handler groups Sharing/Support/Files remain gated.

Selected startup instructions: AGENTS.md, .agents/skills/nexora-engineering/SKILL.md, .ai/profiles/nexora-implementation-agent.md, .ai/roles/technical-lead/README.md and rules/core-rules.md, .ai/routing.json, .ai/verification.md. Selected routes: architecture, backend, database, security, verification, owner-isolation and frontend.

Initial batch: FX18-BR-005; FX19-BR-005/AC-002; NXG-SYS-02/03/05/08/09/14 and NXG-FX18-G03, NXG-FX19-G03. Exact keys time.entry.purge and focus.session.record_time; target time.entry.create is independently required. Source: main docs/features/18-time-tracking.md, 19-pomodoro.md; docs/action-catalog/modules/18-time.md,19-focus.md; docs/ux-ui/modules/18-time-tracking.md,19-pomodoro-focus.md; docs/design-database/05-productivity-calendar.md; common authorization/lifecycle contracts.

Technical decisions: keep Time and Focus within the existing shared time persistence boundary; additive conversion mapping has composite owner FKs and session/entry uniqueness. Conversion pins prevent purge that would invalidate the once-per-session reference. Completed work elapsed excludes pauses; conversion interval ends at completion and represents elapsed work, not the paused wall-clock span. Explicit overlap confirmation required. Wrapper returns only opaque IDs; it does not expose target content to a caller lacking time read. Purge receipts retain only opaque outcome IDs. Mutation, mapping, receipt and audit commit atomically.

Rollback: deploy preceding binary and keep additive table/permission data; no destructive down migration or production operation. SQL/API tests use the existing SqlApiFixture conventions, generated IDs/example.invalid accounts, clean database/migration replay/upgrade assertions and local failure triggers. Browser tests must use normal form login, actual pagination and all five viewports.

Open gates: independent security/migration/cross-module review remains Pending; self-review and test passes cannot close it. main retains FX30/34/35 PO pause, Vault crypto/key/package ADR and operational backup approval; these are precise source gates, not permission to invent contracts.
