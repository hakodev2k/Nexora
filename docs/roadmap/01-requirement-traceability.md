# Current requirement traceability

2026-09-08 · Docs-only. The previous line-number/quoted-paragraph matrix is preserved in [history](../history/20260908/snapshot/docs/roadmap/01-requirement-traceability.md). It is not a second source of executable requirements; its account-purge wording is superseded.

| Coverage | Current trace |
| --- | --- |
| Product Owner decisions | [Decision record](../requirements/10-owner-decisions-20260907.md) |
| Forty feature groups → source requirement IDs | [Requirement routing](../features/93-requirement-routing.md), each feature's traceability section |
| Exact action → UI | [Action catalog](../action-catalog/catalog.csv), [screen bindings](../action-catalog/06-screen-bindings.md) |
| Physical fields / invariants | [Database design](../design-database/README.md) and domain dictionaries |
| M01 requirement → story → API → DB → UX → acceptance | [M01 stories](../delivery/milestone-01/01-stories.md), [OpenAPI](../delivery/milestone-01/openapi.json), [evidence gates](../delivery/milestone-01/06-readiness-and-evidence.md) |
| Current execution slice vs Release1 commitments | [Scope register](../delivery/01-current-scope.md) |
| Remaining major decisions | [Concrete proposals](../delivery/02-decision-proposals.md) |

<a id="11-refinement-delta--2026-09-06"></a>
## Refinement routing

Do not use source line numbers as permanent requirement identifiers; content consolidation changes line numbers. Preserve named requirement/action/screen IDs. A superseded action or conditional advanced Finance requirement retains historical identity but does not become an active M01 story. Subsequent milestones must add the same cross-layer mapping before code approval, rather than generating one ticket per old quoted paragraph. The historical refinement delta is retained in the snapshot linked above; this anchor keeps incoming roadmap navigation valid.
