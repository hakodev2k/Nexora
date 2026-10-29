# FX32-S02 text diff and color — contract before implementation

2026-10-24. Accountable owner: offline Toolbox worker; coordinator owns integration
and independent review. Source `14c76bcb6960e4ce53369dce388a8480b1c0299a`, branch
`impl/m01-s00-scaffold`, PR #4. Current session authorizes these scoped new files,
DEC-20260909-014 and D01–D09=A authorize contract-complete local code only.
Latest AGENTS code-only amendment governs: no tests, fixtures, runtime or SQL execution,
commit/push or independent agents. Compile/typecheck only for implementation errors.

Sources: `docs/features/32-developer-toolbox.md`,
`docs/action-catalog/modules/32-toolbox.md`, `docs/ux-ui/modules/32-developer-toolbox.md`,
`docs/requirements/phases/phase-06-developer-and-automation.md`, existing
`toolbox-extra-offline-contract.md` and `developer-toolbox-pure-slice.md`.
P06/RM14, stories FX32-S01/S02. Goal bindings:

| Goals | Source requirements | Exact action / expected acceptance |
| --- | --- | --- |
| NXG-FX32-G01/G03, NXG-P06-01, NXG-RM14-01 | P06-DAT-004; P06-TBX-001/002/003/004/007/008; FX-32-BR-001 | `toolbox.text_diff.run`: bounded deterministic line/scalar diff, inert escaped output, browser-only payload, recoverable failure |
| NXG-FX32-G01/G02/G03 | FX32 color HEX/RGB/HSL/alpha and UX contrast; P06-TBX-001/002/003/004 | `toolbox.color.run`: explicit color conversion/palette/contrast assumptions, no security claim |
| NXG-SYS-03/05/08 | FX32 SELF guards, current SQL authority, FX-32-AC-001/003 shared bounds/security | Both actions denied before computation if current authority fails; no storage/network effects, no partial success |

## API / architecture / input-output

Browser execution avoids transmitting either input or options. Existing POST
`/api/v1/developer/tools/run` receives only `{toolCode:'text-diff'|'color',input:'',options:{}}`
for a current SQL SELF capability check via the normal service. New partial method
accepts only empty payload/options and returns empty output. Coordinator adds exact
catalog, switch and action allowlist entries; helpers alone do not grant authority.
Browser wrapper takes a zero-argument rejecting authorization callback and optional
AbortSignal; it checks cancellation after authority and before returning results.
No new endpoints, persistence, Redis, providers, crypto, QR dependency or history.

Common: memory-only UTF-8 input <=1 MiB/output <=2 MiB; specific tighter bounds below.
Output is JSON text in the existing escaped `<pre>`; never HTML, URLs, style strings
from input, clipboard reads or automatic navigation/download. Options exact keys/enums;
unknown or malformed values reject. Keep inputs on errors, clear stale output before
Run/options edits, clear protected state and cancel pending runs on tool/auth loss/unmount.
No input/option/parser values in errors. UI renders labeled fields and cancel action.

| Operation | Inputs/options and bounds | Successful output |
| --- | --- | --- |
| Text diff | Primary input = before, controlled second textarea = after; <=128 KiB UTF-8 each, <=4096 tokens each. `mode=line|character` default line (character = Unicode scalar, not grapheme); `newline=normalize|preserve` default normalize. Normalize CRLF/CR to LF; preserve retains terminators including final newline. Character mode rejects unpaired surrogates. LCS <=1,000,000 comparison cells, Uint16 matrix <=about 2 MiB; reject beyond cap. Work yields every <=16,384 comparison cells and observes cancellation; no wall-clock timeout claim. | JSON `{mode,newline,characterUnit,identical,operations:[{kind:'equal'|'delete'|'insert',text}]}`; grouped ordered operations reconstruct both normalized sources; ties choose deletion first. Line tokens include terminators, empty input has zero tokens. No partial/truncated success. |
| Color convert | Input <=256 UTF-16 units, `#RGB/#RGBA/#RRGGBB/#RRGGBBAA`, comma `rgb(r,g,b)`/`rgba(r,g,b,a)` or `hsl(h,s%,l%)`/`hsla(h,s%,l%,a)`; finite decimal numbers only, RGB integer 0..255, alpha 0..1, S/L 0..100, hue 0..360 with 360=0. No names, CSS variables, calc, URLs or mixed/modern CSS syntax. `operation=convert|palette` default convert. `background` default #ffffff, must parse opaque. | Canonical HEX (alpha byte rounded), RGB and HSL numeric values, alpha retained to six decimals. Contrast composites foreground alpha in sRGB onto explicit opaque background, WCAG relative luminance ratio plus AA normal/large and AAA normal/large booleans. Round display only; thresholds use unrounded ratio. Warn that preview is numeric sRGB, alpha HEX quantizes and source HSL to 8-bit RGB rounds; no accessibility certification. |
| Palette | Same color input; `operation=palette`, `scheme=complementary|analogous|triadic|monochromatic` default complementary. Fixed respective sizes 2/3/3/5; alpha retained. Complement hue +180; analogous -30/0/+30; triadic 0/+120/+240; monochromatic fixed lightness 10/30/50/70/90%. | Same conversion of source plus labeled derived colors, each with contrast against explicit background. Deterministic derivation only; no promise of perceptual uniformity, distinctness or accessible pairings. |

Recoverable browser codes: ValidationFailed, InputTooLarge, OutputTooLarge,
DiffLimitExceeded, ColorInvalid, Cancelled. Message contains only safe fixed text.
Server 422 ValidationFailed rejects nonempty payload/options; normal 404 unknown
tool and 409 unavailable authority remain. Never call failed authority successful.

## Migration / recovery / gates / plan

`20261024_0091_toolbox_more_offline.sql` inserts only missing two exact Permission
definitions as Resolved, under UPDLOCK/HOLDLOCK transaction. Existing status,
AdminPermission and module grants untouched. No schema/data transform. Missing
schema fails closed. Replay concurrency and locks require owner SQL verification;
independent DB/security review remains pending, scheduled by coordinator.
Rollback removes wiring/new code; rows stay inert behind old allowlists. No delete SQL.

Plan: (1) this contract/task brief; (2) browser helpers, payload-free partial, controlled
options, additive migration; (3) compile/typecheck if available, inspect scoped diff;
(4) report exact snippets and missing FX32 inventory. Functional/boundary/authority/
rendering/cancellation/migration QA are owner-owned NOT RUN, not inferred from build.
Rule paths loaded: mandatory baseline `.ai/routing.json` paths; backend and frontend
routes plus database route; TL work-intake-and-decomposition and feature-delivery.
No required new business decision found; integration/review/runtime gates remain open.
