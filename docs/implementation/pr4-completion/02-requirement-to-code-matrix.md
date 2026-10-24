# Requirement/action source matrix

Source baseline94fac585, plus current documented deltas.

[action-source-candidates.csv](action-source-candidates.csv) enumerates every
primary action catalog row with exactsource pointer and source token candidates.
This is LEXICAL INVENTORY, not semantic coverage or acceptance. A token in a
policy/SQLfile is not proof of a registered implemented action. No percentages.

Required semantic columns remain to be completed perAC: normativeRequirement/AC,
exact action, APIrequest/response/errors, DBconstraints/transaction/migration,
UIroute/form/dialog/state, owner/action/source security, executedtest/revision,
priority/owner and explicitimplementation status. UNMAPPED is retained honestly
in the CSV until manualreview verifies those relationships.

| Bounded slice | Requirements / source | Exact action binding | API/DB/UI change | Evidence / status |
|---|---|---|---|---|
| Goals eligibility | catalog16; migrations0077–0079 | goals.goal.history/archive/unarchive;goals.target.update/remove | Eligibilityonly; handlers/SQL alreadyshipped | workerbackend; unit54passed; SQLpending |
| Share revokedowner view | D07;decisioncontract05 | sharing.link.read | Testoracle matches inactive/token-null diagnostic projection; viewer404retained | SQLgatepending |
| Document source safety | FX20 currentMarkdown contract | documents.page.create | Literal persistence vs inert renderer separatelytested | SQL/build+component evidence distinct |
| Document compare/import | FX20-S07; current transfer contracts | existing history/compare/import actions unchanged | Real bounded differences; source/modegeneration | knowledgeworker |
| Dashboard/shell | NXG-SYS-13;FX26 | dashboard layout/read actions unchanged | Finite loading, typedresponse, distinctReactkeys | frontendworker |
| Finance dialog | D02;FX27manual | finance.manual_category/record existingactions | One title/form-associatedfooter | frontendworker |
| Time/Focusprivacy | NXG-SYS-02/03/08/13;FX18/19 | Existing exactread/history/purge/prefs/conversion actions | Revocation clearing/requeststamps/dirtydrafts | productivityworker |
| Toolbox | FX32-S01/S02;NXG-FX32-G01/G02 | toolbox.regex/base64/hash.run | No newAPIorSQLschema; boundedvalidatedoutputs | checkpoint94fac58; currentCI260pass2unrelatedfail |

## Reverse audit limits

The token inventory supplies candidates for code→catalog review. Full orphaned
endpoint/unreachableUI/unregisteredaction and acceptance mapping are IN_PROGRESS,
not completed by regexsearch. Current runtime registration is Program.cs Map*
endpoints; each candidate still needs service/SQL/UI and authority analysis.

## Oct23 integration delta

See [current source/requirement trace](09-continuation-20261023.md) and the module-owned
[Learning handoff](../../../src/Nexora.Application/Learning/LearningPlanImplementation.md).
FX40 Plan read/create/update/link/unlink/reorder/complete now have central API/DI,
SQL0086 and native list/detail routing; still gated, not Verified. D06 excludes Skill.
FX32 XML/datetime/CSV/JWT bindings now have API authority, bounded processing and
UI options; SQL0087 inserts missing definitions only. FX04/FX24 privacy repairs
have source changes and independent review; final SQL acceptance remains pending.
The recovered02-requirement-to-code-matrix.csv is an earlier lexical checkpoint,
not an executed coverage matrix or semantic proof on current source.

## Oct24 exact new command trace (code-only, not acceptance)

| Requirement / FX | Exact action(s) | API / SQL / UI | Security / evidence / owner |
|---|---|---|---|
| P03-ORG-002; FX24-S02/S03; BR004/005; AC003 | organization.collection.read/create/update/delete | CollectionEndpoints + SqlCollectionService;0090 owner container/member FK and rowversion; CollectionsScreen `/collections[/UUID]` metadata forms and delete confirmation | Live SELF/currentFX24; opaque digest receipt; delete only container; Anscombe/root; builds only, runtime pending |
| Same FX24 source references | organization.collection.add/remove/reorder | Members routes + source-owned Project/Document/Bookmark/Snippet participants; decimal physical slots; candidate picker and keyboard reorder | Source current-read/lifecycle, hidden refs omitted, exact visible-set reorder, original request retained; root; independent source report |
| P07-SKL-001; FX40-S01/S02; BR002 | learning.skill.merge (+learning.skill.read, Course read when evidence exists) | Skill preview-merge/merge/merge-history;0088 immutable history and merge tombstone; target picker/preview/history UI | Both owner ETags/current source authority, signed expiry/count proof, opaque replay; Hegel; source review separate from runtime |
| P07-LRN-001; FX40-S05; BR006; D06 | learning.plan.archive/unarchive (+learning.plan.read) | Lifecycle preview/commit;0089 exact prior-state/contract guards; lifecycle dialog | Readiness false/Gated; no source mutations; authenticated preview, original receipt/current authority; Franklin; independent bounded source review |
| FX40-S03/S05; D06 | learning.plan.read and learning.course.read plus existing Course writes | Typed available Goal/Task/Course open; App native Courses list/detail and finite login-return routes | Owner-context remount, capabilities before read, immutable recovery and operation-specific final ACK; root; source review, no browser run |
| P06-DAT-004/TBX001–004/007/008; FX32-S02; BR001 | toolbox.text_diff.run / toolbox.color.run | Browser helper/options; empty-payload RunMoreOffline authority seam;0091 definitions; workbench/cancel/discard | No payload server/storage/provider; bounds/cancellation/inert output/current policy; Kepler/root; source review, no functional/performance run |

Status of these bounded delivered commands is CODE_IMPLEMENTED / VERIFICATION_PENDING;
Plan execution remains deliberately Gated. This increment does not complete the
remaining semantic matrix for all40FX or replace older UNMAPPED rows with passes.
