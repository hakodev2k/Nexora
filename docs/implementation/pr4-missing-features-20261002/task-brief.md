# PR4 missing features — task brief

User authorization: attached Pasted text(3).txt explicitly authorizes implementation, isolated synthetic SQL/API/browser tests, commits and push to the existing PR4 head. No merge, new PR or production deployment.

Requirements: origin/main at 8782f46be51f4b0f3cb54f0f0f7a06eae3b0d3e3, read independently using git show/archive origin/main. Implementation starts at PR4 b049b60702f1d8f7a501bdfc555a8dadf95a7810 on impl/m01-s00-scaffold; isolated checkout task/pr4-missing-api-20261002. Existing other checkouts and their uncommitted work are untouched.

QA snapshot: Nexora-Full-Scope-QA-2026-10-01.md, source 221603430c248013fb7912a0cd1c6c224aed294b. Original missing handler groups: 15. Current: 13. Time/Focus are Partial since the snapshot; other target handler groups Sharing/Support/Files remain gated.

Selected startup instructions: AGENTS.md, .agents/skills/nexora-engineering/SKILL.md, .ai/profiles/nexora-implementation-agent.md, .ai/roles/technical-lead/README.md and rules/core-rules.md, .ai/routing.json, .ai/verification.md. Selected routes: architecture, backend, database, security, verification, owner-isolation and frontend.

Initial batch: FX18-BR-005; FX19-BR-005/AC-002; NXG-SYS-02/03/05/08/09/14 and NXG-FX18-G03, NXG-FX19-G03. Exact keys time.entry.purge and focus.session.record_time; target time.entry.create is independently required. Source: main docs/features/18-time-tracking.md, 19-pomodoro.md; docs/action-catalog/modules/18-time.md,19-focus.md; docs/ux-ui/modules/18-time-tracking.md,19-pomodoro-focus.md; docs/design-database/05-productivity-calendar.md; common authorization/lifecycle contracts.

Technical decisions: keep Time and Focus within the existing shared time persistence boundary; additive conversion mapping has composite owner FKs and session/entry uniqueness. Conversion pins prevent purge that would invalidate the once-per-session reference. Completed work elapsed excludes pauses; conversion interval ends at completion and represents elapsed work, not the paused wall-clock span. Explicit overlap confirmation required. Wrapper returns only opaque IDs; it does not expose target content to a caller lacking time read. Purge receipts retain only opaque outcome IDs. Mutation, mapping, receipt and audit commit atomically.

Rollback: deploy preceding binary and keep additive table/permission data; no destructive down migration or production operation. SQL/API tests use the existing SqlApiFixture conventions, generated IDs/example.invalid accounts, clean database/migration replay/upgrade assertions and local failure triggers. Browser tests must use normal form login, actual pagination and all five viewports.

Open gates: independent security/migration/cross-module review remains Pending; self-review and test passes cannot close it. main retains FX30/34/35 PO pause, Vault crypto/key/package ADR and operational backup approval; these are precise source gates, not permission to invent contracts.

## Current Windows continuation, 2026-10-03
Current attachment: Pasted text.txt. Work continues in out/blocked-module-apis from PR4 d46ca6e52143677cbd22f778f1c3503b27d0deba; the earlier brief above records a historical implementation run, not current branch state. Current remote PR4 head is impl/m01-s00-scaffold at c5f37acca75b02005b2c63b71017f5fb14285c4e after Time query repair, Wishlist and Skills commits. Main authority remains 8782f46be51f4b0f3cb54f0f0f7a06eae3b0d3e3 exported explicitly from main. Current attached request supersedes code-only testing limits and authorizes real synthetic SQL/API/browser verification and push. Independent review was explicitly authorized; reviewer course_review supplied source review for Course. Course contract and exact source/goal/action/AC/evidence bindings are learning-course-contract.md and course-evidence.md. No new PO decisions, merge, production or paused provider execution. Full remaining action/module coverage and full pre-existing browser regression remain open.

Asset batch authorization clarification (2026-10-03): automatic approval review rejected SQL testing under the later AGENTS code-only amendment. The user explicitly answered: Yes—override the code-only amendment for isolated local testing. Real generated SQL/API/browser fixtures and tests are therefore authorized; no production/provider scope is added.


Next batch FX38: main feature/action/UX/P07/dictionary/classification/payload sources read; goals NXG-P07-01 and NXG-FX38-G01/G02/G03 bound in digital-assets-contract.md. Plan eleven SELF manual typed metadata/lifecycle/history/renewal keys, six owned subtype records, SQL migration0039, exact expiry clocks and owner-safe UI. Independent expiry/type/cohort source review found delegated technical choices possible; implementation has not begun. Main DEC008 excludes Digital outbound and requires separate approval; that gate is preserved. Existing PR4 current head e902bb20b0c5424223efe70b8f43c37db9a73cd5, no merge.

## User-requested pause, 2026-10-03

The user explicitly requested a temporary stop, push of all changes and saved continuation state. Implementation is paused. All 64 existing changed code/contract/evidence files compared to the remote Asset tree match exactly allowing only UTF-8 BOM/line-ending representation differences; task brief and Digital draft were the only unpublished content before this checkpoint. The checkpoint commit adds these planning updates and `resume-state.md` to existing PR4 without merge. Owned API/frontend processes were verified against their command lines and listener PIDs, stopped, and both sandbox ports have zero listeners. Synthetic SQL database, credentials and private files remain local. No further tests or product changes were made for the pause. Current details and remaining gates are in `resume-state.md`.

