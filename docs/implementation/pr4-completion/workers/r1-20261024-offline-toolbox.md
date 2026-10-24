# Offline Toolbox worker handoff — 2026-10-24

Implemented scoped helpers/options/authority seam and additive migration; **not yet
integrated into the central catalog/switch/App or permission/manifest wiring by this
worker**. No commit/push. No claim of FX32/R1 completion or runtime acceptance.

## Task brief / source / authority

- Owner: offline Toolbox worker; coordinator owns central integration and schedules
  independent security/DB review. No independent agents started.
- Observed branch `impl/m01-s00-scaffold`; source HEAD
  `14c76bcb6960e4ce53369dce388a8480b1c0299a` (matches request).
- User explicitly authorizes full local R1, bounded to this worker's new files;
  DEC-20260909-014 and current PO D01–D09=A apply. Latest AGENTS code-only amendment
  supersedes older contract QA allowances. No providers/production/real secrets,
  tests/fixtures/runtime execution or commits/push performed.
- Contract written before implementation:
  [toolbox-more-offline-contract-20261024.md](../../toolbox-more-offline-contract-20261024.md).
  P06/RM14; FX32-S01/S02, `toolbox.text_diff.run` and `toolbox.color.run` only.
- Required startup read: AGENTS, `.agents/skills/nexora-engineering/SKILL.md`, profile,
  TL README/core, routing, verification, docs README/current delivery scope, goals
  README/system/phase/delivery/module/task-evidence sources, DEC014 and D01–D09,
  FX32 feature/action/UX, Phase 6 and current pure/extra-offline contracts.
  Specialist paths: `.ai/routing.json` backend/frontend/database/security routes;
  TL `skills/work-intake-and-decomposition.md`, `workflows/feature-delivery.md`.
  No agent-kit changes; baseline verifier not applicable to this code slice.
- No unresolved business decision for these operations. Integration, current-head
  compilation after coordinator edits, owner runtime acceptance and independent
  review remain pending. Other workers' files were observed and left untouched.

## Owned new files / behavior

| File | Implemented result |
| --- | --- |
| `web/Nexora.Web/src/ToolboxMoreOffline.ts` | Browser-only diff/color implementation; zero-argument current-authority callback, typed recoverable errors and cancellation |
| `web/Nexora.Web/src/ToolboxMoreOfflineOptions.tsx` | Controlled second diff input, mode/newline controls, color convert/palette/background controls, explicit limits/privacy text, optional Cancel |
| `src/Nexora.Infrastructure/DeveloperTools/LocalToolboxService.MoreOffline.cs` | Private partial `RunMoreOffline` rejects every nonempty input/option; no parsing/transmission of payloads; normal Run SQL gate remains mandatory |
| `database/migrations/20261024_0091_toolbox_more_offline.sql` | Insert missing two Permission definitions only; preserves existing statuses and every actor/module grant |
| `docs/implementation/toolbox-more-offline-contract-20261024.md` | Contract, task brief, bounds, errors, goal/requirement/action trace, recovery and acceptance obligations |
| This report | Integration handoff and factual evidence |

Diff: deterministic LCS, line or Unicode scalar mode (not grapheme); CRLF/CR → LF
normalization or exact terminator preservation; terminal newlines remain significant.
128 KiB UTF-8 per input, 4,096 tokens per side, 1,000,000 comparison cells; bounded
typed-array memory and cooperative event-loop yields/cancellation every 16,384 cells.
Grouped equal/delete/insert JSON is a single escaped comparison, with no HTML parsing.
Errors reject the whole result, retain drafts, and suggest reducing input/line mode.
No wall-clock latency guarantee; owner must measure responsiveness/cancellation.

Color: HEX 3/4/6/8, comma RGB(A)/HSL(A), alpha and finite/range validation. Convert
returns numeric RGB/HSL and canonical HEX. Palette supports complementary (2),
analogous (3), triadic (3), monochromatic (5 fixed lightnesses); alpha retained.
Contrast preview computes sRGB luminance after compositing foreground onto explicit
opaque background; AA/AAA normal/large thresholds use unrounded ratio. Quantization,
duplicate colors and absence of accessibility certification are explicitly labeled.
No crypto/trust/security verification claim. Color input <=256 code units; no CSS
names/variables/URLs/calc; output stays inert JSON. Global output cap 2 MiB.

## Coordinator integration — exact keys and snippets

In **LocalToolboxService.cs**, add these catalog entries and switch arm (worker did
not edit the file). ExecutionMode must be Browser to describe the actual location:

```csharp
new("text-diff", "Text diff", "Text", "Browser-only bounded line/Unicode scalar comparison.", "Browser", "toolbox.text_diff.run", true),
new("color", "Color converter / palette", "Color", "Browser-only HEX/RGB/HSL/alpha and numeric contrast preview.", "Browser", "toolbox.color.run", true)

// ExecutePure switch, after ordinary Run SQL authority resolution:
"text-diff" or "color" => RunMoreOffline(code, command.Input, command.Options),
```

In **ActionGrantPolicy.cs**, add exactly `toolbox.text_diff.run` and
`toolbox.color.run` beside existing SELF Toolbox entries. Preserve inactive-status
deny precedence and current SQL module/action/session guards. No broad action grant.
In **src/Nexora.Infrastructure/Local/M01MigrationManifest.cs**, append
`20261024_0091_toolbox_more_offline.sql` after coordinator's 0088/0089/0090 entries;
preserve exact order/embedded-resource convention. No Program/DI/new route needed.

In **App.tsx**, import:

```tsx
import { ToolboxMoreOfflineOptions, defaultToolboxMoreOfflineOptions } from './ToolboxMoreOfflineOptions';
import { MoreOfflineError, runMoreOfflineBrowser } from './ToolboxMoreOffline';
```

Add controlled `secondInput` and options using the same `extraOptions` map, plus
`const offlineAbort = useRef<AbortController | null>(null);`. For every tool change
and catalog fallback, set defaults from `defaultToolboxMoreOfflineOptions(code)`
for the two new tools, otherwise existing defaults; clear `secondInput`. Add these
tool codes to `optionsForTool()` returning `extraOptions`. Label main diff input
**Before**, and set main input maxLength/visible bound to 131072/128 KiB for diff,
256 for color. Helper validates UTF-8 byte bound regardless of HTML maxLength.

Place the new options beside existing `ToolboxExtraOptions`:

```tsx
<ToolboxMoreOfflineOptions toolCode={selectedCode} options={extraOptions}
  secondInput={secondInput} disabled={busy} running={busy}
  onChange={value => { setExtraOptions(value); setOutput(null); setError(null); setCopied(false); }}
  onSecondInputChange={value => { setSecondInput(value); setOutput(null); setError(null); setCopied(false); }}
  onCancel={() => {
    offlineAbort.current?.abort(); ++toolEpoch.current;
    setBusy(false); setOutput(null); setCopied(false);
  }} />
```

Within existing Run's try/stamp/error handling, route new codes before generic
server execution; never send their primary/second input or options to the API:

```tsx
offlineAbort.current?.abort();
const controller = new AbortController();
offlineAbort.current = controller;
const result = selectedCode === 'text-diff' || selectedCode === 'color'
  ? await runMoreOfflineBrowser(selectedCode, input, secondInput, extraOptions,
      () => runDeveloperTool(selectedCode, '', {}), controller.signal)
  : selectedCode === 'jwt'
    ? await runBrowserJwt(input, () => runDeveloperTool('jwt', '', {}))
    : await runDeveloperTool(selectedCode, input, optionsForTool());
if (stamp === toolEpoch.current && !controller.signal.aborted) setOutput(result);
```

Map helper errors in the existing catch before `asApiError`:

```tsx
const apiError = requestError instanceof MoreOfflineError
  ? new NexoraApiError(requestError.message, 422, requestError.code)
  : asApiError(requestError);
```

Retain existing current-stamp check before error handling; abort, increment epoch
and clear **both inputs/options/output/copied** on auth/module loss, tool switch,
unmount and Clear. Abort on cancel, clear old errors and announce cancellation
through the screen's status/notice convention. Preserve drafts on recoverable
limits/color errors. Authority callback must reject unavailable responses, not
return a false flag. Render result only with existing escaped `<pre>`; no raw HTML.
Keep copy explicit. Existing dirty-confirmation/reset flow includes second input.
Do not label an empty authority response as a computed result or expose Copy then.

## Evidence / review / rollback

| Command/check | Actual result / boundary |
| --- | --- |
| `git -c safe.directory=D:/Projects/ASP_NET_Core_Developer_2026/Nexora rev-parse HEAD` and `branch --show-current` | Match requested SHA and branch. Command-local safe.directory used because sandbox identity differs; no global git config changed. |
| `dotnet build src/Nexora.Infrastructure/Nexora.Infrastructure.csproj --no-restore` | Exit 0; build succeeded, 0 warnings, 0 errors; includes new partial. No functional tests or SQL execution. |
| `& .\node_modules\.bin\tsc.cmd -b --pretty false` from `web/Nexora.Web` | Exit 0, no diagnostics; checks new browser helper/component types. No browser execution. |
| Functional/unit/integration/E2E/browser/SQL migration/runtime checks | **NOT RUN**, no tests/fixtures/mock/demo data added, per code-only amendment. |
| Independent security/DB review | **Pending coordinator**; author inspection/build is not independent review. |

Evidence is for the uncommitted owned files on the source SHA in this shared tree,
not a new commit or a claim about other workers' changing code. Relevant coordinator
edits invalidate integration build evidence and require renewed checks.
Rollback: remove catalog/switch/UI wiring and new helper files; keep permission
definitions inert behind prior allowlists. No destructive rollback SQL or business
data change. Migration has not been applied; replay/lock/status preservation require
owner SQL verification. No restoration of stale authority on asynchronous completion.

## Exact remaining FX32 at this handoff

Already present at source: base64, URL/HTML codec, hash, UUID, password, JSON, regex,
XML formatting, UTC integer epoch conversion, CSV viewer and browser JWT decoder.
This worker adds diff/color source helpers only; their wiring/readiness remains with
coordinator until the steps above are applied.

| Action/capability | Remaining implementation/acceptance gap observed in current catalog/switch/UI |
| --- | --- |
| `toolbox.yaml.run` | YAML format/validate absent |
| `toolbox.data_convert.run` | General JSON/XML/YAML/CSV conversion and explicit lossy mapping warning absent; XML formatting/CSV viewing do not implement conversion |
| `toolbox.datetime.run` | Current converter handles integer epoch → UTC only; ISO ↔ epoch, IANA zone conversion and elapsed vs calendar arithmetic/DST ambiguity remain |
| `toolbox.cron.run` | Five-field dialect builder, timezone/range/next ten/DST preview absent |
| `toolbox.markdown.run` | Bounded sanitized Markdown preview absent |
| `toolbox.code_format.run` | Pinned SQL/HTML/CSS/JS/C# nonexecuting formatters/options absent |
| `toolbox.qr.run` | Text-only QR preview/download/error-correction contract/implementation absent; no dependency added here |
| `toolbox.url_parse.run` | Pure URL field parser with masked credentials absent |
| `toolbox.headers.run` | Pasted header inspection with sensitive-field handling absent |
| `toolbox.certificate.run` | Bounded public certificate parse/fingerprint/validity plus non-trust label absent |
| P06-ENC-002 | Unicode/code-point versus UTF-8 Hex/Binary operations absent; no new action key invented here |
| `toolbox.password.run` / `toolbox.uuid.run` | Existing password length and fixed alphabet/UUID v4 only; password character-set options/entropy caveat and explicit UUID version selection/description need full source acceptance reconciliation |
| `toolbox.output.copy` | Explicit copy UI exists, full action/current-authority acceptance still owner-owned |
| `toolbox.output.download` | Explicit safe output download absent |
| `toolbox.output.save_snippet` | Explicit warning and Snippets composite integration absent |
| Favorites and `toolbox.history.read/save/delete` | Catalog favorites/opt-in safe history/private clear/exclusion of secrets absent; no history dependency added |
| `toolbox.support.read` | Approved safe diagnostics projection absent from this worker; no input/output support access |
| `toolbox.network.http`, `toolbox.network.dns` | Remain PO-paused/unavailable; no provider or execution capability added |
| FX32-S01/S02/S03/S04 UX + all source AC | Catalog category/search/favorites/deep-link behavior, mobile/keyboard, reset/dirty/revoke and full family acceptance remain owner QA/integration obligations; not established by compile checks |

FX-32-AC-001/002/003 and common P06 gates remain unaccepted as whole-module claims.
New diff/color limits, malformed Unicode, palette formulas, rounding/contrast edges,
inert malicious text rendering, cancellation and denied/revoked capability scenarios
all require owner functional verification after integration. This inventory describes
observed source, does not delete committed R1 requirements or accept residual risk.
