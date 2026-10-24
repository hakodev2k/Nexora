# Skill merge worker handoff — 2026-10-24

Status: scoped local code implemented; compile/typecheck/bundle passed. Runtime
acceptance, migration execution, independent review and central integration pending.
No commit, push, tests, fixtures, provider calls or application runtime performed.

## Task brief and authority

Accountable scoped worker: Skill merge (Hegel); root Technical Lead integrates and
schedules independent review. Requested outcome: explicit FX40 Skill merge with
preserved evidence/history, both revisions/current authority, opaque replay and
meaningful native preview/confirm. Baseline HEAD
`14c76bcb6960e4ce53369dce388a8480b1c0299a`, branch
`impl/m01-s00-scaffold`, PR #4 stays draft. User authorized this bounded work directly;
DEC-20260909-014 and D01–D09=A apply. Current AGENTS code-only amendment governs
verification. Other workers' simultaneous edits were observed and left untouched.

Contract written before code:
[Skill merge contract](r1-20261024-skill-merge-contract.md).
Read startup AGENTS, engineering skill, profile, Technical Lead README/core,
`.ai/routing.json`, `.ai/verification.md`, docs README/delivery/current scope,
goals README/system/phase/module/evidence template, current DEC014/D01–D09,
Phase07, feature40/shared behavior, action40, UX40, DB learning dictionary,
existing Skill contract/code/migrations and Course evidence participant.
Engineering routes loaded: architecture/backend/frontend/security/database/
owner-isolation (all referenced rule/skill/control files in routing manifest).
No delegated agents used. No agent configuration changes.

## Exact delivered scope and files

| File | Implemented behavior |
| --- | --- |
| `src/Nexora.Application/Learning/SkillContracts.cs` | Source/target selection, confirmed merge, preview/history DTOs, MergedIntoId in read DTO, ISkillService commands |
| `src/Nexora.Api/Features/Learning/SkillEndpoints.cs` | POST source preview-merge, POST source merge, GET skill merge-history; existing identity/CSRF/API result boundary reused |
| `src/Nexora.Infrastructure/Learning/SqlSkillService.cs` | Exact action capability, signed actor/owner/revision/count/expiry preview, atomic merge, typed receipt/replay, current Course source guards, history paging, source lifecycle lock, history purge pins |
| `database/migrations/20261024_0088_skill_merge.sql` | Replace NULL-only marker constraint with Archived/nonself marker constraint; same-owner SkillMergeHistory FKs/count/JSON constraints/indexes; exact Resolved merge metadata, no Allow grants |
| `web/Nexora.Web/src/skillApi.ts` | Typed preview/confirm/history client; confirms only validated opaque target acknowledgement |
| `web/Nexora.Web/src/SkillScreen.tsx` | Source card → paged/searched Active target picker → preview both names/levels/revisions/counts/expiry → explicit confirmation; frozen uncertain retry; retained source evidence/history inspection |
| `docs/implementation/pr4-completion/workers/r1-20261024-skill-merge-contract.md` | Contract-first API/DB/UX/security/acceptance/rollback and trace |
| This report | Build evidence, central wiring handoff, remaining gates |

No edits to App.tsx, Program, ActionGrantPolicy, M01MigrationManifest, tests,
SkillEvidence helper files or other worker files. Existing SkillEvidence and
Course source participant are reused.

## Transaction and lifecycle details for review

Exact new action: `learning.skill.merge`; prerequisites `learning.skill.read`,
and current `learning.course.read` plus available same-owner source for every
existing evidence ref on either skill. Both skills distinct, Active and unmerged.
Both exact rowversion ETags required. Five-minute HMAC preview binds actor,
PersonalSpace OwnerId, source/target IDs and revisions, counts and expiry.

One Serializable SQL transaction takes existing PersonalSpace UPDLOCK/HOLDLOCK,
rechecks current account/session/module/action authority, claims receipt, checks
both revisions/preview/evidence providers, writes history/copies missing evidence,
archives source with MergedIntoId, touches target revision, updates both registry
rows, writes audit/typed receipt, rechecks authority/providers, commits. Failed
checks or SQL/audit errors dispose the transaction without a partial commit.

Original source metadata/evidence IDs/notes/attribution and existing audit history
remain retained. Target metadata/proficiency/lastUsed do not change. Evidence
present only on source is copied with new target evidence identity and original
note/creation attribution. Overlapping target evidence stays unchanged; source
retains its separate interpretation. Existing generic ResourceLinks stay at source.
History stores both before snapshots/counts/merge instant/actor. No artificial
pre-install history, no unmerge and no source mutation after tombstoning. Target
may use ordinary lifecycle; retained merge history pins purge, as do evidence refs.

Receipt stores a versioned actor/owner/source/target envelope plus required Course
read dependency, not Skill DTO/ETag. Exact replay proves digest and original current
write/read prerequisites then returns only `{id,committed:true,detailAvailable:false}`.
Replay can resolve an expired original preview after a proven prior commit; an
expired preview cannot start a new merge. Malformed/unprovable receipt fails closed.
No historical payload is returned by replay. Fresh history reads require current
skill.read and remain owner-only.

## Goal → source → command → evidence

| Goals | Binding sources | Exact command/surface | Code/evidence and open assertions |
| --- | --- | --- | --- |
| NXG-FX40-G01, NXG-P07-01 | P07-SKL-001; FX-40-BR-003; FX40-S02 | learning.skill.merge; learning.skill.read | Preview/merge/history code; no inferred level, distinct evidence interpretations retained. Compile-only; human evidence-preservation QA pending |
| NXG-SYS-02/03/05/08 | FX-COM-001/002/006/007; FX-COM-AC-001…005 | learning.skill.merge + skill.read and dynamic course.read | Owner-scoped SQL/current capability/typed receipt/ETags/transaction; cross-owner/revoke/race/rollback/replay assertions pending |
| NXG-SYS-09/13/14 | FX-40-BR-006; FX-COM-010/013; FX-COM-AC-006 | Source tombstone, history, preview/confirm | SQL lifecycle guard and native UI; keyboard/mobile/conflict/uncertainty runtime QA and independent DB/security review pending |

FX40 AC001–004 concern Course milestones, Course links, Certification reminders
and TimeEntry dedupe; they are not claimed satisfied by this Skill merge slice.

## Executed implementation checks

Commands executed from repository root unless noted. These detect implementation
errors only; they are not functional/runtime/security acceptance.

| Command | Actual result |
| --- | --- |
| `dotnet build src/Nexora.Api/Nexora.Api.csproj --no-restore -m:1 -p:UseSharedCompilation=false` | Final backend state: exit 0, 0 warnings, 0 errors; 11.74 seconds. Earlier compile also passed, before receipt dependency refinement |
| `.\node_modules\.bin\tsc.cmd -b --pretty false` in `web/Nexora.Web` | Exit 0, no diagnostics, including final target-query pagination refinement |
| `npm run build` in `web/Nexora.Web` | Exit 0 (`tsc -b && vite build`). Vite warns about chunks above 500 kB; no bundling error. Repeated after final picker edit |
| `git -c safe.directory=D:/Projects/ASP_NET_Core_Developer_2026/Nexora diff --check -- src/Nexora.Application/Learning/SkillContracts.cs src/Nexora.Api/Features/Learning/SkillEndpoints.cs src/Nexora.Infrastructure/Learning/SqlSkillService.cs web/Nexora.Web/src/SkillScreen.tsx web/Nexora.Web/src/skillApi.ts` | Exit 0, no whitespace errors |

Git was used read-only with command-local safe.directory because workspace
ownership differs from execution identity; no global git config changes.

Final scoped code SHA256 (baseline plus these uncommitted files identifies this
worker's checked source; builds also saw concurrently changing shared files):

```text
SkillContracts.cs    72FF4CBF725F4F74DE0CF3993D4335D272F5ABC353A209C22B21113CEB447CFA
SkillEndpoints.cs    A760614D99FE28E7F82D58D7979608A1F74251B24BC3DD81A3949D77BFEFD94D
SqlSkillService.cs   AC9E8E2F75E2BB9BFD3E4F32870A3F254D0944A302B4F3300602F206C778F924
SkillScreen.tsx     BC6392F8243B4EFC9E7E379B159AE1872064EAAA96B2FEC68CBB09164D4FDBF5
skillApi.ts         2BBC76584C540B389D9AD704171964F396B9C23AF9168B5101B50C031B95FF8F
0088 SQL            977888207B418C0E388674FB996BF90C97BB549AEA632C640605A09A901ED14D
```

## Coordinator handoff and remaining gates

1. Add `20261024_0088_skill_merge.sql` in order to
   `src/Nexora.Infrastructure/Local/M01MigrationManifest.cs`. Current Skill
   reference guards/history queries depend on its table; do not run the new
   binary against an unmigrated database.
2. Add exact `learning.skill.merge` to the installed Admin-grantable action set
   in `src/Nexora.Domain/Access/ActionGrantPolicy.cs`; preserve grant defaults.
   No Program/DI wiring needed: existing SqlSkillService and MapSkillEndpoints
   carry these commands. Existing native Skills route renders the UI.
3. Root schedules independent authorization/DB/source-contract review, including
   retained Archived merge tombstone, duplicate-note preservation, lifecycle/purge
   pins, dynamic source authority and transaction/replay behavior. This worker's
   self-review is not independent approval.
4. Human owner performs all SQL migration, functional/API/concurrency/browser,
   fixture/QA/acceptance work per current code-only instruction. None run here.
   Root must obtain current integrated build/CI evidence on the final committed
   revision; concurrent worker compilation is not a final current-head CI claim.
5. Rollback: disable FX40 before reverting binaries; preserve pointers, history
   and evidence/schema. Old lifecycle handlers lack tombstone guard and must not
   be used after merges exist. No down migration/data repair/unmerge executed or
   approved by this handoff. Production, real providers and merge remain gated.

No missing business decision blocked this bounded local implementation. Central
wiring, independent review and runtime acceptance remain explicit incomplete gates;
this report does not label FX40, P07 or R1 complete.
