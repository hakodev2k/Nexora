# Bounded E25 actual UI evidence

Four byte-identical copies of final actual local synthetic browser screenshots,
all personally opened by the QA author. Desktop1366x768 and mobile390x844; real
application CSS and native flows, no CSS injection, positive mocks, private storage
or credentials. Full-page Saved Search images are tall scrollable pages, not viewport
fit claims. Current results include a Bookmark created after saving the definition.
Other images show the bounded-clear confirmation and actual keyboard selection;
successful handoff/cancel/SQL results are proven by tests, not static screenshots.

Executed final desktop3PASS15.5s/mobile3PASS15.6s (three unique scenarios,
six viewport executions) through owned runtime f47aadb1a93742e09a162148b65818fe.
Report: `../../workers/discovery-qa-authoring-20261027.md`. Reporters retained locally
at `web/Nexora.Web/artifacts/discovery-final-{desktop,mobile}/`. No human final
UI/product acceptance or full feature/release acceptance is implied.

| Image | SHA256 |
|---|---|
| palette-mobile.png | 70F228D38EC0124BA33DD87CD20032263B7C8754EC9BD3782C0C4CDD702C183A |
| recents-mobile.png | B6882B559DA789512F2E4F3F50F1A12F9F5BB51A215A48D1FB4107A6BB021E7C |
| saved-desktop.png | C2C911ED219996B6310FC436DB94931E7D3637BEFE82429DDEA8FBC2BFC72912 |
| saved-mobile.png | E98A2015C02E93E9EE4F2ADA9D47EDE9412F83EE93483133F5915C88487455C6 |

Execution used base a0215f7 plus coordinated working tree, not an immutable delivered
commit. Reconcile recorded source hashes to coordinator's final commit/current-head
CI; relevant later edits invalidate affected evidence.
