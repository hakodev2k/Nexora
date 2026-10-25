# Continuation evidence — 2026-10-25

These are selected, sanitized evidence records for [report19](../../19-completion-uiux-20261025.md).

- `sanitized-executions.json`: counters parsed from actual local TRX files, including failures and their SHA256 digests. Reruns are not additive case counts. Raw files stay in ignored `artifacts/test-results/completion-20261025/`; they are not uploaded CI artifacts.
- Three `before-*.png` images are **before-fix** screenshots of confirmed defects, captured on owned synthetic data at desktop1440/mobile390. They are not final-layout approval. Root visually opened all three; additional worker inspections are identified in their reports.
- `source-manifest.csv` fingerprints canonical LF-normalized changed code/test/script/migration bytes delivered with this checkpoint. It does not establish that historical executions used every final byte. Final late styles and FX18 recovery have build/typecheck evidence only.

Latest permitted implementation-error checks after late source changes:

| Command | Result |
|---|---|
| `dotnet build src/Nexora.Api/Nexora.Api.csproj --no-restore -c Debug` | PASS;0warnings/0errors;2.37s |
| `npm run build` in `web/Nexora.Web` | PASS; TypeScript then Vite6.4.3;387modules;5.04s bundler; main1,175.55kB /302.90kB gzip; size advisory remains |
| FX18 author `npx tsc -b --pretty false` | EXIT0; source implementation-error check only |
| Root `npx tsc -b --pretty false` after final create-only Template handoff / Bookmark408 correction | EXIT0; later than the last bundle build, no runtime check |

No test/browser/data execution was started after the latest user-supplied AGENTS code-only amendment. Prior frozen backend SQL run304PASS does not prove late frontend recovery/layout. The earlier9-area CI UIUX scanner is scoped to its prior checkout; this continuation ran only six areas across two viewports before the amendment. No exhaustive UX, accessibility, security, performance or R1 acceptance claim.

No credentials, generated keys, signed download URLs or private runtime environment are stored here. The owned disposable runtime was cleaned after runs completed through the existing marker/SID/path/PID-start guarded cleanup script.
