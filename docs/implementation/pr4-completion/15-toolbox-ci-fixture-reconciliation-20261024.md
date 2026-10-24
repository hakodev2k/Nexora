# Existing Toolbox fixture reconciled to current result DTO

CI [40111116959](https://github.com/hakodev2k/Nexora/actions/runs/40111116959),
source `08a8ef80baa06cd6e894e00476e4a4939213e58b`: frontend job120922855921
actually failed one existing case,200pass/1fail/1existing skip across25files.
Do not report this run green or substitute prior checkpoint evidence.

## TF-01 — incomplete existing UUID success fixture

Type TEST_INFRA; priority P1 for CI; severity Medium; confidence High, observed
assertion failure plus source-contract proof. Case:
`Toolbox removes the previous success when changed options fail and retains correction input`
at App.surface.test.tsx:105. Expected the first completed UUID result under
`Tool output`; actual UI never rendered it because the response failed validation.
One failure in one recorded CI attempt; no local reproduction executed.

The existing HTTP mock returned toolCode/output/durationMilliseconds but omitted
warning/errorPath. Current normative
`docs/implementation/developer-toolbox-pure-slice.md:31` specifies those fields;
`ToolboxServiceContracts.cs` declares nullable Warning/ErrorPath and the API does
not configure null omission. This is a fixture-contract mismatch exposed by the
new strict-200/result validator, not justification to weaken that validator.

Correction changes **one existing response object only**, adding `warning:null`
and `errorPath:null`. Same case, inputs, output, request branch, error response and
all assertions remain. No new test method, mock record, fixture/scenario, artificial
dataset or product behavior. This is an existing-contract repair, not new QA/data
design. Current code-only restriction on adding tests/fixtures remains honored;
no local tests executed. Independent source review is recorded as an addendum to
`workers/r1-20261024-review-url-headers.md`.

Closure requires the unchanged case and frontend suite pass on the new pushed
source. Preserve the failed run for history. Root runs compile/typecheck only;
authorized push may automatically trigger existing CI, whose results must be
read and reported separately. SQL/browser/new-tool acceptance is not inferred
from this fixture correction. PR4 stays Draft, no merge/provider/production.
