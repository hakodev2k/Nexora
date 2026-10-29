# FX32-S01/S02 extra offline tools — bounded implementation contract

Contract recorded before code, 2026-10-21. Accountable owner: Toolbox implementation agent.
Source: PR4 `impl/m01-s00-scaffold`, `33e68006efdc75ed86551679aaf7fa6ccd8b3a5a`;
isolated detached worktree `.worktrees/toolbox-extra`. Session explicitly authorizes
local code and synthetic unit/SQL/component QA, superseding the run-specific no-test
amendment without editing AGENTS. No push, merge, external providers or real data.

Authority: current D01–D09=A (`docs/requirements/13-owner-decisions-20261019-pr4-blockers.md`),
DEC-014, FX32 feature/action/UX sources and normative Phase 6. Phase P06/RM14;
not M01 acceptance or whole-module completion. D04 forbids outbound effects;
D09 keeps independent security/DB review and integration acceptance pending.

Exact new bindings: `xml` → `toolbox.xml.run`, `datetime` →
`toolbox.datetime.run`, `csv` → `toolbox.csv.run`, `jwt` → `toolbox.jwt.run`;
all LOCAL/SELF, Admin-grantable only when explicitly integrated and allowed.
No `toolbox.data_convert.run` implied: CSV renders a viewer representation, not a
general JSON converter. Other tools, history, network, diff and color remain outside this slice.

## API/data contract

Reuse GET `/api/v1/developer/tools` and POST `/api/v1/developer/tools/run` with
`{toolCode,input,options}` and existing response `{toolCode,output,warning,errorPath,durationMilliseconds}`.
Cookie/CSRF/rate limits remain existing route responsibilities. SQL current SELF
session/owner/module/dependency/action checks precede parsing every run; no injected
authorization delegate, caching or bypass. No business writes, Redis, files,
history, input/output logging or external calls. No optimistic persistent state,
receipt or ETag needed for pure processing. Catalog metadata is not an action grant.

Input <=1 MiB UTF-8 and output <=2 MiB. Extra options use exact case-sensitive keys
and enum values; unknown/empty options rejected, <=8 keys and <=64 characters/value.
Safe 422 codes: ValidationFailed, InputTooLarge, OutputTooLarge, XmlInvalid,
XmlLimitExceeded, CsvInvalid, CsvLimitExceeded, DateTimeInvalid. Existing denied
409 ModuleUnavailable and unknown-code 404 remain. Errors never echo input,
option values, parser messages or XML names; XML line/column and CSV row number
are numeric only. No successful partial output. Output text uses LF. All bounds
are engineering choices under delegated FX32 detail, not changed product scope.

| Tool | Options / input | Output / strict limits |
| --- | --- | --- |
| XML formatter | `indent=true|false` default true; XML document text | XmlReader DTD Prohibit, resolver null, no XInclude/XSLT/schema loading; max depth 64 and 50,000 nodes (attributes counted), attributes bounded before materialization. Preserve text/CDATA/whitespace/comments, no namespace/type conversion. Pretty layout only where whitespace can be added safely, no semantic round-trip guarantee; warning on every format. Numeric line/column on malformed input. No XML declaration encoding claim on string output. |
| UTC epoch converter | required `unit=seconds|milliseconds`; signed integer epoch only, optional leading minus, no whitespace/fractions/exponents/plus. No implicit magnitude guessing or host culture/timezone. | JSON `{unit,epoch,utc}`; epoch is decimal string to avoid JS rounding; UTC ISO `yyyy-MM-ddTHH:mm:ss.fffZ`. .NET UTC range year 0001–9999 (seconds -62135596800..253402300799; ms -62135596800000..253402300799999). No calendar arithmetic, floating dates, IANA conversion or claim to complete TME-002. |
| CSV viewer | `delimiter=comma|semicolon|tab` default comma; `quote=double` and `encoding=utf-8` fixed; `header=true|false` default false. RFC4180-style quoted cells, doubled quotes, CRLF/LF/CR records; embedded newlines preserved. | JSON `{header: string[] or null, rows: string[][]}`. No coercion, object-key/header dedup or spreadsheet export. Header removed from rows only when requested. Fixed rectangular width; <=1,000 total records including header, <=100 columns, <=16,384 UTF-16 code units/cell, <=50,000 cells. Terminal newline does not add record; empty input yields no rows; trailing delimiter adds empty cell. Formula prefixes remain literal; unconditional formula-text warning. Malformed quote/width rejects whole run. |
| JWT browser decoder | No token transmission. POST jwt with empty input/options is a payload-free current-authority check only; nonempty input rejected. Browser token <=32 KiB, exactly 3 compact JWS segments; canonical unpadded base64url, fatal UTF-8; header/payload objects <=16 KiB decoded each, depth <=32/nodes <=4096; signature syntax only, never verified. | Browser JSON `{header,payload,signatureVerified:false}` and unconditional Unverified warning. `alg:none` can be decoded, never trusted. No key/issuer/audience/expiry validation, JWE/decryption, signature or authentication claim. JWT catalog ExecutionMode=Browser; server returns empty output and warning only. |

## UX/integration contract

New controlled `ToolboxExtraOptions` reuses field-group/check-row styling and receives
`toolCode`, string option map, `onChange`, `disabled`. Explicit seconds/milliseconds
select is required, starts unselected; fixed UTC output/UTF-8 encoding/double-quote
are visibly stated. CSV delimiter/header and XML indent exposed with labels.
Changing options invalidates existing output. Keep input on failure; clear previous
output before Run. Render all results in existing escaped `<pre>` only; no HTML,
spreadsheet, automatic clipboard or download, URLs, local/session storage.
JWT badge must say Browser only / Unverified and explain decode is not verification.
Use supplied `runBrowserJwt` with an authorization callback that calls generic
`runDeveloperTool('jwt','',{})`; it must NEVER receive the token as callback input.
Clear protected input/output on auth loss/module denial/unmount/tool change; guards
in the integrating screen must prevent stale async completions repopulating data.
Existing tool controls, accessible loading/errors/mobile layout remain screen-owned.

## Migration/recovery/integration ownership

0087 is reserved here; 0086 belongs to Learning. New migration inserts missing
four exact permission rows as Resolved only; existing statuses (including Paused,
Blocked/Superseded) and AdminPermission/module grants remain untouched. No status
promotion of existing rows, schema changes, deletion or broad MERGE. Lock-protected
insert in transaction makes replay/concurrency safe. Missing prerequisite schema
fails rather than silently marking migration applied. Review current existing row
status before enablement; conflicts stay denied pending source reconciliation.
Rollback: revert this additive code/catalog/UI wiring; leave permission rows inert
under old allowlists. No destructive rollback SQL or deleted business data.

Integrator owns App.tsx, Program.cs, ActionGrantPolicy.cs, M01MigrationManifest.cs;
this worker must not edit them or existing tests. Manifest needs exact new migration
filename appended after 0086; Admin SELF allowlist needs four exact keys alongside
existing toolbox actions, preserving inactive deny precedence. Generic API needs
no new route/DI/Program registration. UI wiring and exact snippets appended in handoff.

## Trace/evidence plan

| Goals | Requirements / AC | Story/action | Positive and negative evidence |
| --- | --- | --- | --- |
| NXG-FX32-G01/G03, NXG-SYS-08 | P06-DAT-002, TBX-001/003/004; FX-32-BR-001/002, AC-001 | S02/toolbox.xml.run | Unicode, mixed content; reject DTD/internal/external entity, malformed input with secret marker absent error, depth/node/output bounds |
| NXG-FX32-G01/G02 | P06-TME-001; FX-32-BR-005 | S02/toolbox.datetime.run | epoch/negative/UTC range edges, same instant in sec/ms; reject missing/unknown units, overflow/fraction/exponent/unsafe magnitude |
| NXG-FX32-G01/G02/G03 | P06-DAT-003, TBX-001/003; FX-32-AC-003 | S02/toolbox.csv.run | quoted Unicode, commas/newlines/quotes/header/formulas retained; malformed quotes/row width/row/column/cell/total limits |
| NXG-FX32-G02/G03, NXG-SYS-08 | P06-SEC-001, TBX-002/004; FX-32-AC-002 | S02/toolbox.jwt.run | browser canonical JWS and alg:none unverified; malformed UTF-8/base64url/depth/size rejected; denied callback prevents decode; callback gets no token, network/storage spies |
| NXG-SYS-02/03/05, NXG-FX32-G03 | P06-TBX-008; action catalog SELF guards | S01 catalog + all four actions | real SQL current module/action/session revoke and mismatched owner; migration replay preserves existing nonresolved statuses; missing SQL is NOT RUN, never mocked proof |

Rules loaded: mandatory baseline plus `.ai/routing.json` backend/frontend/security/
database/verification paths (exact manifest paths); no agent-kit modifications.
Independent security/DB review unavailable here: pending integrator. Unit tests
prove pure parser behavior only; SQL/API tests require actual approved synthetic
SQL runtime, not an in-memory substitute. Build/typecheck are implementation checks,
not functional QA. Final command facts and revision boundaries appended after execution.
