# Browser QA continuation — 2026-10-23

Source baseline: `fbafbaa482b094f26f067f1697d1f9d610e5367c`.
Baseline CI run: `39792570922`; checkout merge `2497194dc6672b76f65d0a83eb98fff4cf832200`.
User's current request explicitly authorizes continuing logic and browser tests using isolated synthetic data. AGENTS.md remains unchanged.

## Observed baseline

Downloaded browser JUnit contains 28 cases: 21 pass, 2 failure, 5 error, zero skipped. Identity fails on desktop/mobile. Reset errors on mobile. Auth and navigation scanners error on both viewports. Existing raw failure logs were deliberately sanitized, so source diagnoses require subsequent runtime confirmation.

Source confirms identity's `PERSONAL SPACE` oracle now points inside a closed native account disclosure, while the actual home heading is `#home-title`. Mobile logout was addressed through a global button locator despite sidebar logout being hidden and account logout requiring disclosure. Generic Playwright configuration also accidentally discovered all nine UIUX area scanners with a 45-second timeout; their dedicated configuration uses 240 seconds. Navigation modal route is explicitly FX25-S02 dialog UX, whereas generic scanner expected a normal main page.

## Changes

- Identity retains register → local operator code → verify → login, axe, search, real Snippet create and responsive checks. Home now asserts the semantic home heading and authenticated `/api/v1/me` success. Reset explicitly opens the native account menu and clicks its scoped logout on both viewports.
- Main suite excludes only the scanner file. All nine area shards remain in the dedicated UX configuration, with their existing route and screenshot completeness assertions. Root expands workflow triggers so application/API/schema changes run those shards.
- `/command-palette` alone uses its documented named dialog/combobox/listbox, focus, axe, Escape, home restoration contract. All other routes retain the visible main requirement. Root owns invalid-share semantic main fix and regression test.
- New FX16 journey performs UI create with numeric initial/current/target `0/2.5/10`, proves 25% and Draft via actual API response, activates, records 10 and a synthetic note in the real dialog, proves 100% remains Active via separate GET and progress-history evidence. Archive retains Active as prior state; reload proves archived persistence; Unarchive restores Active. A fresh tab in the existing authenticated context plus API read proves retained progress; it does not prove a new login/session. No request interception, MSW, real accounts, providers or ambient targets are used.
- Safe-step helper replaces exception details with static allowlisted stage IDs. JUnit sanitizer strips call logs/properties/streams and retains only those IDs, including repeated sanitization. Canary unittest proves secret text and untrusted stage values do not survive.

## Verification completed locally

- `npm run build`: PASS (existing bundle-size advisory only).
- Standalone TypeScript check of the four modified/new E2E files: PASS.
- Default Playwright discovery: 12 cases in four files, desktop/mobile.
- Dedicated UX discovery: 18 area cases in one file, nine areas × two viewports.
- `python scripts/dev/test-e2e-sanitizer.py`: PASS, one test covering canary redaction, unknown stage rejection, discarded attributes/streams/properties and idempotence.
- `git diff --check`: PASS at this checkpoint.

Discovery and build are not browser execution. Updated real SQL browser runtime remains pending root's isolated target/CI execution; record exact tested SHA, run URL, counts and failure stages from that run before declaring acceptance.

## Counting and remaining limits

Baseline 28 = 10 non-audit cases + 18 scanner cases. Updated suites contain 12 non-audit + 18 dedicated scanner cases = 30 planned distinct executions. Removing duplicate discovery from the wrong configuration is not improved coverage or a passing result. Six of the 12 non-audit cases are real identity/reset/Goals journeys; bootstrap and smoke cases use controlled dependency fixtures. Scanner route probes are not full CRUD/business acceptance.

Project/Task, Docs, Finance and Tags still need additional populated real browser workflows; this bounded change does not claim those features' complete E2E acceptance. Goals cross-owner/revocation/concurrency/receipt security, Boolean target management, target removal/preview and all lifecycle variants are not exercised by this browser scenario. Existing backend/API evidence must be reported separately. Full logic/E2E coverage and R1 completion remain unproven.

## Parent execution follow-up

See [continued-20261024.md](continued-20261024.md) for actual CI execution after this source checkpoint. Original pending/blocked statements above describe the author checkpoint and are retained as history.
