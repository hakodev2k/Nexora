# E25 current-authority, IME and legacy compatibility negative QA

Accountable worker: templates/UI foundation; root owns runtime scheduling, App and
delivery. Existing user instruction + DEC014 automated-QA authority permit only
isolated synthetic execution. Same PR4 branch/base a0215f7 plus coordinated working
tree. Bindings: NXG-FX25-G01/G02/G03, NXG-SYS03/08/13/14; FX25 BR003/004/005/006,
AC001/003; concrete DISC4 current-authority/keyboard and SavedQuery schema contracts.
No product behavior, gates, live provider or production data changed.

## Source and authored scenarios

Read current SqlCommandPaletteService exact403 ActionDenied/404 ResourceUnavailable,
SavedSearches failure projection clearing and migration UI, SavedQuery migration0060,
FunctionalTestOperator, LocalSqlTarget and real isolated bootstrap/seeder.

`functional/discovery-negative.spec.ts` adds three cases:

1. Native palette shows own authorized Bookmark; genuine SuperAdmin preview/commit
   revokes UserA FX21 module grant after listing; Execute must404 ResourceUnavailable
   and clear all options/private projected title without source mutation. Restored
   source grant; populated SavedQuery list then FX25 revoke must409 ModuleUnavailable
   and clear list/title while SQL unchanged. Finally restores both exact module dispositions,
   asserts full module/action snapshots and unchanged role; aggregates cleanup failure.
2. DOM compositionstart/end represent IME lifecycle; real browser Enter/Escape keys
   during active composition must neither execute nor close. End composition then
   normal Escape closes. This proves browser event handling, not physical OS IME UI.
3. Persisted static schema99 fixture must show MigrationRequired, disabled Run,
   exact API409 even after reload; explicit native version1 replacement persists
   current definition and enables Run; normal UI confirmed delete removes fixture.
   API schema99 rejection is retained separately in discovery-state.spec.ts.

## Narrow test-only fixture writer and independent review

One command added to `tests/Nexora.FunctionalTestOperator/Program.cs`:
`seed-legacy-saved-query USER_UUID`. It accepts no SQL, query, title, schema or
authority input. Existing LocalSqlTarget validates Development/loopback/no attached
files or failover, then exact `Nexora_Test_<32lowerhex>` database. New command requires
process run exactly `nexora-e2e-audit-` + database suffix and database-level
`nexora.test.run` extended property exact ordinal equality. Only active exact
`qa-usera-{database last8}@example.invalid` user and active mapped PersonalSpace
qualify. Serializable transaction inserts exactly one newly generated row with
fixed schema99, fixed valid harmless JSON, synthetic title, owner/user FK. No
authentication/grant/secret changes, updates or deletes. Native UI repairs/removes it.

Root independent review caught initial Guid.TryParse(run) incompatible with actual
bootstrap prefix; corrected before any fixture execution. Verified real bootstrap
`scripts/dev/start-isolated-browser.ps1` lines69–71 invokes this exact seeder whose
email formula is source-derived, not guessed. Test includes wrong-run and unknown-user
rejections, with process stdout/stderr private. Shopping independent reviewer was
given final source fingerprint; their outcome belongs to their report.

Actual compile: strict TypeScript test compile exit0; operator Release build exit0,
0warnings/0errors3.85s initially, corrected-run guard rebuild2.23s. No fixture executed
by build. Final operator SHA256
`8136DC0EFFAC3BD1E33E81B9EAFDF5A67B2A41B46E2E8F03FC9D5E9E76C2D9C6`;
test authoring SHA256 `3367BE74B86B7B5D8C50651445EEBB36A1C21CAD24B95E8C52C7C4006603CCBB`.

## Runtime status

Coordinator approved exclusive desktop3 then mobile3 window after guard correction
and build. Initial desktop3: legacy fixture/UI PASS5.0s, authority and IME cases
timed out180s/90s before palette existed (early shortcut issued before mounted shell
shortcut effect). Preserved `artifacts/discovery-negative-initial-desktop/` JUnit/JSON.
Revocation occurs only after the missing option wait; no initial grant mutation was
reached. Changed only test readiness to actionably click real mounted header button,
added15s action timeout; existing normal suite retains real Ctrl+K coverage.

Readiness-corrected desktop run: IME/legacy2PASS; revocation failed exact403 oracle
against observed409. Preserved `artifacts/discovery-negative-oracle-desktop/`.
SqlSavedSearchService.Denied line219 explicitly returns409 ModuleUnavailable for
module/action unavailability; corrected exact status AND code, without loosening
server gates or accepting arbitrary denial. Source clear branch recognizes this
code. Source-resource execute retains exact404 ResourceUnavailable. Entire finally
module/action/role restoration assertions passed even on this failing run.

Final actual results:

| Scenario | Desktop1366x768 | Mobile390x844 | Real verification |
|---|---|---|---|
| Listed-source revoke + saved list revoke/private clear | PASS4.8s | PASS3.8s | Genuine preview/commit grant changes, current404/409, protected UI cleared, SQL source/definition unchanged, exact finally restoration |
| IME composition event handling | PASS1.4s | PASS1.4s | Enter/Escape during composition send0commands and keep modal; after composition normal Escape closes |
| Fixed legacy schema compatibility | PASS3.2s | PASS2.6s | Wrong run/unknown synthetic user rejected, actual static99 SQL row, disabled Run +409, reload, native explicit schema1 update and confirmed delete |

Final desktop **3PASS11.6s**, mobile **3PASS10.0s**: three unique scenarios/six
viewport executions, not six features. Reporters safely copied before reuse to
`artifacts/discovery-negative-final-desktop/{junit.xml,results.json}` and
`artifacts/discovery-negative-final-mobile/{junit.xml,results.json}`. No added
product source changes were necessary. In particular actual React capture protects
composition Escape; no shared modal IME defect was observed. The prior normal
three scenarios/six viewport executions remain separate, so together these two
final suites comprise six unique E25 cases/twelve viewport executions.

Final strict compile of both discovery files exit0. Executed source hashes SHA256:

| Source | Hash |
|---|---|
| discovery-negative.spec.ts | 7C8CF0BF8AB9BF2EE05D367CF602A9040380EA61328E9BF9919A615B7737CDED |
| FunctionalTestOperator/Program.cs | 8136DC0EFFAC3BD1E33E81B9EAFDF5A67B2A41B46E2E8F03FC9D5E9E76C2D9C6 |
| CommandPalette.tsx | 62A4FF62EEA4BB3E82A552A75ABFFF55EDA71042DEF864A1C1060473DB47BB77 |
| SavedSearches.tsx | 276B986A9002033500E13C39D36A4A401F7A84507F253B594778D6E20E86C1DA |
| ResourceFormDialog.tsx | 006A6220944A2D0C0C3E000C0195E29943BCF29880D2EEE91DBCCE5CA5ECF58A |

Remaining scope is explicit: direct Audit-row retention oracle, palette Create
required-field/dirty guard variations, stale async authority races and uncertain
response/retry scenarios are not implied PASS by these cases. Root current-head CI,
full requirements matrix and human final UI/product acceptance remain separate gates.
No screenshots were generated by this added negative suite; actual safe positive
screen captures remain in `evidence/discovery-20261027/`. Runtime writer released;
no active child browser execution.
