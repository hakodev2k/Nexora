# Ignore and publication task brief

Requested outcome: ignore generated/private local material and publish appropriate
implementation, migrations, contracts, tests and QA reports to the existing
`impl/m01-s00-scaffold` branch / draft PR #4. The user explicitly authorizes commit
and push; no merge or production/provider execution is authorized.

Initial local HEAD: `1cd27a46a2ba1f24ab2b6f32f84771230f4b1761`.
Remote PR head observed through the GitHub runtime connector:
`f0611fad590269fdc73a3397fd785a97d2c0ef5e`, five commits ahead of local HEAD.
Existing worktree implementation changes and remote commits must be preserved.

Rules selected: AGENTS.md, nexora-engineering skill, implementation profile,
Technical Lead README/core rules, `.ai/verification.md`, current delivery scope,
DEC-20260909-014 and system goals NXG-SYS-08/14. This repository maintenance
operation adds no new business action or acceptance criterion. The earlier QA
fingerprint and results remain historical evidence, not proof of a merged tree.

Plan: classify local files; ignore private snapshots/storage and generated test
output; retain source/migrations/contracts/lockfiles and sanitized QA deliverables;
review staged paths and sensitive material; synchronize without overwriting remote
changes; publish using the host GitHub connector. No force update or merge to main.

Pending gates: existing QA findings and independent security/DB review remain open.
Generated JUnit artifacts are untracked without deleting local copies; prior QA's
JUnit overwrite deviation remains documented. Historical curated evidence in docs
is retained. No new product functional testing is claimed by this publication.

Synchronization result: the remote five commits were merged locally with13
conflicted paths reviewed. Local additions (Dashboard, Discovery, Goal/WorkLog,
Documents topology and registered source adapters) were retained; remote Finance
security files and CI SQL lifecycle are preserved. Additive test-fixture cases and
file replay assertions were combined rather than discarded. The existing unit
migration-list defect remains open; no QA finding was silently fixed.

Implementation-error checks on the synchronized tree:
- dotnet build src/Nexora.Api/Nexora.Api.csproj -c Release --no-restore: exit0,
  0warnings/0errors.
- npm run build (web/Nexora.Web): exit0;367modules,6.98s Vite build; existing large
  chunk warning. This includes TypeScript compilation from the package script.
- git diff --cached --check with core.whitespace=cr-at-eol: exit0. The default
  whitespace check reports CRLF in unchanged incoming CSV evidence.

No functional suites were rerun on the synchronized tree. Earlier QA is historical,
not acceptance of the merge; independent review and QA failures remain pending.
