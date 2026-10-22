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
