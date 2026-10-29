# Continued FX32 implementation — native catalog and URL/header workbench

Base `8eb5aaff54c493d27ae1a0baa7209bdadecda948`, existing Draft PR4 branch.
DEC014/D01–D09 unchanged. Latest user continues implementation; code-only AGENTS
still prohibits new tests/fixtures/local functional runtime. Root is accountable
for integration, recovery, compilation, review reconciliation and publication.

## Delivered source / requirement binding

| Source / goals / exact action | Implemented behavior and files | Status / verification |
|---|---|---|
| FX32-S01/S02; P06-TBX001/003/004/007/008; NXG-FX32-G01/G03, SYS03/08/13/14; toolbox.catalog.read | App native `/developer/tools/:toolCode`, finite safe login/expiry return, tool-context remount; actual catalog name/category/purpose filter and clear/no-result; scoped query/category restoration | CODE_IMPLEMENTED; navigation source review, compile only; browser/scroll/mobile NOT RUN |
| P06-NET001; FX32-BR001; toolbox.url_parse.run | Browser absolute hierarchical URL components, normalized host/port/path/query, duplicate parameters; bounded Unicode/percent decoding; top-level userinfo/fragment masking; nested encoded-authority masking; no fetch/open | CODE_IMPLEMENTED; independent privacy review required; no parser/runtime cases executed |
| P06-NET003/004; FX32-S02; toolbox.headers.run | Browser pasted headers/status, ordered duplicates/counts, sensitive-field group redaction, reject folding/body/malformed names; generic errors, inert JSON | CODE_IMPLEMENTED; independent privacy review required; no HTTP send/authentication/trust claim |
| NXG-SYS03/05/08; exact two new keys | Catalog Browser/AcceptsOptions=false, empty authority callback in App → existing real API/SQL capability → RunUrlHeadersAuthority; payload rejected server-side; effective ShippedSelf keys; migration0092 and exact manifest | Source-consistent; no Allow grants, status promotion, Plan/network activation or SQL execution |

Contracts precede code:
[navigation](../toolbox-catalog-navigation-contract-20261024.md),
[URL/header](../toolbox-url-header-contract-20261024.md).
Backend source: LocalToolboxService.UrlHeaders.cs and central catalog/switch;
frontend source: ToolboxUrlHeaders.ts and App. No dependency/lockfile change.
Existing migration filename assertion gains0092 only; no new test method/fixture.

## Review defects and corrections

- **R24-TN-01, Medium/P2:** tool-key remount lost catalog filters. Root retains only
  query/category in Shell ref scoped to account/PersonalSpace/role/module context;
  child restores on open/return; known denial resets parent and child view. Private
  input/output/options remain child-local and are discarded on confirmed navigation.
  Godel independently closed in source; full scroll restoration is not claimed.
- **UH-01, High/P1:** decoding nested URL userinfo before detecting its encoded
  delimiters could expose credentials in visible fields. Root checks recognizable
  authority at each encoding layer before decode and after the final layer;
  credential or ambiguous encoded authority redacts the entire field. No private
  intermediate text enters error/output. Independent re-review tracked separately.

Final independent disposition: Godel closed R24-TN-01 and Einstein closed UH-01
at the final recorded product hashes, including strict-200 API-client hardening.
No confirmed source blocker remains in these bounded assignments. They executed
no tests/runtime. Final nine-file changed-source LF digest:
`8ec9958a62f86bc3ce4c8a1132377e811a30e6785122d24ec342f8a449a017d3`.
Root refreshed the frontend build after the final helper correction.

Root also hardened `api.ts.runDeveloperTool`: only HTTP200 with the matching
normalized tool code and a valid result DTO resolves successfully. Pending202,
non-JSON and malformed output cannot serve as a positive browser authority check.
During source re-review, residual query-first-decode and encoded-marker/prefix
paths were identified and corrected before publication. The final algorithm
preserves raw query ambiguity, masks encoded `://` markers before decode, and
inspects every literal authority regardless of whether scheme letters are encoded.
This intentionally may mask benign encoded URLs; warnings disclose that tradeoff.

Both are source-derived findings, not injected/runtime reproductions. Arbitrary
secrets in innocently named fields cannot be detected reliably; the UI/output
warns field redaction is not a secret scanner or safe-to-open certification.

## Actual workers and recovery

No new distinct agents: Kepler was resumed for helper/seam0092/contract, then quota
interrupted before final handoff; root recovered and finished wiring/hardening.
Godel independently reviewed navigation and its fix. Einstein independently
reviewed helper/privacy plus central permission/manifest flow and raised UH-01.
Their new reports contain actual reviewed hashes and outcomes. Six distinct AI
agents remain the batch total; two additional review assignments reuse two of
the existing four independent reviewers. No manual browser/CI worker launched.
Kepler's interrupted handoff is not recorded as successful completed delivery.

## Verification and limits

Root commands: `dotnet build tests/Nexora.UnitTests/Nexora.UnitTests.csproj
--no-restore -v minimal`, then the equivalent IntegrationTests project: both
exit0/0warnings/0errors. `npm run build --prefix web/Nexora.Web`: TypeScript/Vite
pass,379modules; retained >500kB bundle advisory. Curated final logs/source map:
`evidence/oct24-tools/`. No local tests, SQL migration, browser, fixtures, provider
or performance run. Earlier automatically triggered CI is bound only to8eb5aaff
in [13](13-r1-checkpoint-ci-20261024.md), not these newer executable bytes.

Local configuration: Windows NT10.0.26300.0, SE Asia Standard Time; .NET SDK10.0.302,
Node24.17.0/npm11.13.0. Build configuration Debug for backend and production Vite
bundle for frontend. No browser viewport/runtime measured in this increment.

Rollback removes new wiring/routes/helpers, keeps permission definitions inert
and all existing data/receipts/history. No destructive down SQL, source writes,
payload persistence or live-provider capability. Full R1 remains incomplete;
remaining YAML/conversions/date arithmetic/cron/Markdown/formatters/QR/certificates,
history/favorites/output workflows and other all40 ledger gaps remain work.
