# PR4 local R1 implementation continuation — 2026-10-22

Status: IN_PROGRESS, product INCOMPLETE, PR remains Draft.

Accountable owner: root Technical Lead. Source: branch `impl/m01-s00-scaffold`,
HEAD `33e68006efdc75ed86551679aaf7fa6ccd8b3a5a`, exact1696file tree
`d108fcfec38c8a99f9c5fa138be5ef3528141ed3` verified against GitHub blobs.
User supplied MASTER PROMPT in Pasted text(7).txt and then requested continuation.
This authorizes code, isolated synthetic QA, independent parallel work and push,
and retains D01–D09=A, DEC-20260909-014 and all release/security gates.

## Authorization and recovery

AGENTS.md's historical code-only amendment is preserved. Later explicit
synthetic QA authorization is documented in PR4 and DEC-20261019-PR4;
the current task explicitly requests that authorized verification. This is not
permission for real providers, production, paid services, secrets or a merge.

The initial working environment was reset before unpushed changes could be saved.
Its six AI worker attempts and their reported test counts are historical chat
observations only. This continuation reconstructs source changes and reruns checks;
lost files, screenshots or test artifacts are not current evidence.

## Work packages and critical path

| Package | Owner | Dependency | Complexity | Exit criteria |
|---|---|---|---|---|
| Exact source/PO contracts | root | GitHub read | medium | source hashes and gates recorded |
| Backend authority/oracles | restore_backend | contracts/migrations | high | exact five-key eligibility, D07/D08 negative tests, independent review |
| Dashboard/shell/Finance | restore_frontend | typed API/UX | high | no duplicate keys/loading loop; accessible coherent dialog/footer and mobile layout |
| Documents diff/import | restore_knowledge | FX20 contracts | high | bounded inert comparison, stale comparison/import cleared, regression evidence |
| Focus/Time privacy | restore_productivity | existing source authority | high | no late private response, no overlapping polls, dirty draft recovery |
| Toolbox bounds/privacy | root | FX32-S01/S02 | medium | safe UTF8/parser failure, capped regex, legacy warnings |
| 40FX inventory | inventory | source and normative catalogs | high | explicit gaps; lexical candidates distinguished from semantic proof |
| Independent review | review | frozen changes | high | named findings and author remediation; runtime limits explicit |
| Final checks/push | root | integrated reviewed code | high | actual commands/results, current-source fingerprint, commit/ref confirmation |

Critical path: contracts → independent edits → compilation/components →
SQL/API/browser/OS gates → review → traceability → commit/push. Missing runtime
blocks only affected verification. Uncontracted product/security behavior is
recorded separately from unimplemented authorized work; neither becomes accepted.

## Acceptance and rollback

Relevant goals: NXG-SYS-02/03/05/08/13/14/16 and module goals FX16/18/19/20/26/27/32.
Backend keys trace migrations0077–0079 and exact action catalog; UI/data flows
retain current server authorization, ETags and opaque ACK semantics.
Each worker report binds exact source/requirement/action and executed checks.
No migration/data reinterpretation is planned. Rollback is a coherent code revert
on the Draft branch; preserve prior SQL receipts, revocation tombstones and data.
No downgrade, production restore or destructive external operation is authorized.

## Continuation rule

Run successive permitted implementation/verification cycles. The full Release1
scope remains committed. A checkpoint is not successful R1 completion. Remaining
ordinary work is NOT_STARTED/IN_PROGRESS; genuine approval/runtime dependencies
remain explicit and resumable in08-pending-approvals-and-risks.md.

## Current Oct23 execution amendment

The latest user-supplied AGENTS restores code-only execution for this continuation.
Do not apply the earlier QA authorization paragraph above to new Oct23 work.
Actual integration, worker ownership, build-only evidence, preserved dirty work and
rollback are recorded in [the current continuation](09-continuation-20261023.md).
Baseline fbafbaa; default-false Plan readiness, all D01–D09 and Draft gates retained.
