# FX32-S02 browser URL and pasted headers contract

Contract recorded before code, 2026-10-24. Accountable worker: URL/header Toolbox;
coordinator owns central integration, reviews/build, commit/push. Source observed:
`8eb5aaff54c493d27ae1a0baa7209bdadecda948`, `impl/m01-s00-scaffold`, existing PR #4.
User authorizes full local R1 with this worker limited to new disjoint files.
DEC-20260909-014 and D01–D09=A remain authority; latest AGENTS code-only amendment:
no new tests/fixtures, runtime, providers, dependencies/lockfiles, commit/push or agents.

## Source / goal / action binding

Read mandatory AGENTS/engineering skill/profile/TL README+core/routing/verification,
docs README/delivery README/current scope, goals README/system/P06/FX32/evidence
template, DEC014/D01–D09, FX32 feature/action/UX, Phase 6 and existing pure/extra/more
offline contracts. Previously loaded unchanged backend/frontend/security/database
rule/skill routes remain applicable; no agent configuration changes.

| Goal | Current source | Story/action / expected acceptance (not executed) |
| --- | --- | --- |
| NXG-FX32-G01/G03, NXG-P06-01, NXG-RM14-01 | P06-NET-001; P06-TBX-001/002/003/004/008; FX-32-BR-001 | FX32-S01/S02 `toolbox.url_parse.run`: bounded browser-only component parsing, masked userinfo, no fetch/navigation/storage |
| NXG-FX32-G01/G02/G03, NXG-SYS-08 | FX32 URL parser/header inspector matrix; P06-NET-003/004; P06-TBX-003/004 | FX32-S02 `toolbox.headers.run`: manual pasted headers, field-based credential redaction, case/duplicate/folding boundaries, escaped data only |
| NXG-SYS-03/05 | Current SELF action catalog and runtime policy | Both actions require normal central Run current SQL session/account/PersonalSpace/module/action policy before browser work; no grant bypass |

FX-32-AC-001/003 apply as common bounds/no external effects, not a claim that this
slice implements network SSRF or XML/regex/lossy conversion acceptance. AC002 JWT
behavior is unchanged. No missing product decision found for these local parsers.

## API / execution / data

Reuse POST `/api/v1/developer/tools/run` only for payload-free authority checks:
`{toolCode:'url-parse'|'headers',input:'',options:{}}`. C# partial method
`RunUrlHeadersAuthority` rejects nonempty input or options, returns empty output.
Coordinator catalog maps `url-parse` → `toolbox.url_parse.run`, `headers` →
`toolbox.headers.run`, execution Browser, AcceptsOptions false. Existing central
Run SQL policy is mandatory; this worker does not modify catalog/switch/policy,
App/native routes/search, migration manifest, tests or other workers' files.

Browser `runToolboxUrlHeaders(code,input,authorize,signal?)` awaits zero-argument
rejecting authorization callback; no payload passed to callback. Optional AbortSignal
checked before/after authority and processing. Pure `parseToolboxUrl` and
`inspectToolboxHeaders` helpers are exported without IO. No storage/history/audit
payload, provider, fetch, clipboard read, navigation, HTML interpretation or execute.
Result matches `{toolCode,output,warning,errorPath:null,durationMilliseconds}`.
No options or options component needed. Screen shows browser/redaction/limit notices,
keeps drafts on recoverable errors, clears stale output on edits, clears/cancels
protected state on auth loss/tool switch/unmount, and epoch-guards late completions.
Render output in escaped `<pre>` only; copy/download require existing explicit actions.

## URL behavior / privacy

- Absolute hierarchical `http`, `https`, `ws`, `wss`, `ftp` only; require literal
  `scheme://`, nonempty host; reject relative/protocol-relative/opaque/file/data/
  javascript URLs, backslashes, raw whitespace, invalid Unicode/control/bidi text,
  invalid percent/UTF-8 escapes. No base URL or implicit network resolution.
- Input <=16 KiB UTF-8, <=128 query pairs; <=256 decoded Unicode scalars per key,
  <=4096 per decoded value. Browser WHATWG URL normalization (lowercase/punycode
  hostname, default ports omitted, dot-segments normalized) is explicit. No DNS,
  TLS, reachability, public-IP, homograph, safety or trust assertion.
- JSON contains scheme, authority **without userinfo**, normalized hostname/port,
  path, query array in original order (duplicates retained), fragment presence with
  content always redacted, credential-presence flags with username/password masked.
  Never output original/full URL, origin with credentials, raw query or raw fragment.
- Query percent-decodes strictly (plus → space), preserving duplicate keys and empty
  values. Key classification is case/punctuation insensitive, recognizes auth,
  authorization, password/passwd/pwd, token, secret, cookie, credential, session,
  api-key/access-key/private-key/client-key, signature/sig, key/code/assertion/state
  and cloud signed-URL names. Repeated percent encoding up to four layers is
  inspected; deeper/ambiguous encodings and non-ASCII names redact values. Names
  containing credentials/credential-like bearer/basic text are themselves masked.
  Nonsensitive names and values stay visible after nested URL-userinfo inspection.
  Any value with sensitive assignment syntax or bearer/basic credential text is
  fully redacted. Fragment content always redacted because routing/token semantics
  are unknown. Each encoding layer is inspected before decoding can reinterpret
  authority delimiters. Nested scheme://userinfo@ or percent-encoded authority
  delimiters conservatively redact the whole visible field. The final decoded
  layer is checked too; no intermediate credential text is returned.
  A marker containing any encoded colon/slash component of `://` is conservatively
  ambiguous before decoding, even without known credentials. This intentionally
  masks some benign encoded URLs; it prevents one decode from creating a scheme
  marker and erasing userinfo boundaries simultaneously. Such output is not a
  positive assertion that a secret was detected.
  Literal `://` authorities are inspected even when the preceding scheme letters
  are encoded or malformed; no decoded alphabetic-prefix prerequisite can skip
  credential detection.
- Field classification is conservative and cannot identify arbitrary secrets in
  innocently named path/query fields. Output warning states it is not a secret
  scanner, URL allowlist or safe-to-open certification. Raw draft stays in editor
  only; no automatic safe URL hyperlink or navigation button is produced.

## Header behavior / privacy

- Pasted field block only, optionally first line `HTTP/1.0`, `HTTP/1.1`, `HTTP/2`,
  `HTTP/2.0`, `HTTP/3` or `HTTP/3.0` + three-digit 100–599 status. Reason phrase
  never echoed. Request lines, pseudoheaders, multiple blocks/body, bare CR,
  folded lines beginning SP/HTAB, invalid field-name tokens, control/bidi and
  malformed Unicode fail whole run with fixed error, numeric line only if available.
- <=64 KiB UTF-8 total, <=200 fields, <=8192 UTF-8 bytes per physical line,
  name <=128 ASCII token characters. LF/CRLF accepted, terminal blank lines allowed.
  Values trim surrounding SP/HTAB only; duplicate rows/order retained with lower-case
  names and duplicate count. No comma-joining, folding, cookie parsing or replay.
- Every occurrence of sensitive case-insensitive field names is fully redacted:
  authorization/proxy-authorization, cookie/set-cookie, API/access/private/client
  keys, secret/token/password/credential/session/signature-containing names, cloud
  signing fields. URL-bearing Location/Content-Location/Referer/Refresh/Link fields
  are redacted entirely to avoid reflected signed URLs. Unknown fields retain text
  only after credential-value detection and nested URL-userinfo scrubbing. Ambiguous
  Unicode field names fail; duplicates never weaken redaction. Folded secret lines
  cannot be reinterpreted as ordinary text. Redaction cannot certify remaining data
  free of unlabelled secrets; no authentication/trust/HTTP-delivery assertion.

Common output <=2 MiB UTF-8; JSON additionally escapes `<`, `>`, `&` and Unicode
line separators as data. Fixed recoverable codes: ValidationFailed, InputTooLarge,
OutputTooLarge, UrlInvalid, UrlLimitExceeded, HeadersInvalid, HeadersLimitExceeded,
Cancelled. No parser exception/raw input/name/value in error. Server 422 for
nonempty payload/options; normal 404 unknown tool and 409 unavailable policy remain.

## Migration / rollback / plan / gates

`database/migrations/20261024_0092_toolbox_url_headers.sql`: transaction with
UPDLOCK/HOLDLOCK inserts only missing exact two Permission rows as Resolved;
preserves all existing statuses/grants/module state. No DDL, business data, status
promotion or AdminPermission changes. Missing schema fails. Coordinator registers
manifest in order; owner SQL replay/concurrency/rollback verification NOT RUN.
Recovery removes new wiring/helpers; leave definitions inert under previous
allowlists. No destructive rollback SQL, production operation or sensitive data use.

Bounded plan: contract first → three new implementation/migration files → source
inspection and integration handoff appended here. Coordinator runs current-head
compile/build/typecheck and independent security/DB review. Worker runs no build
under this session's explicit coordinator ownership. Functional/Unicode/encoding/
credential/duplicate/folding/output-limit/denied/cancel/rendering/runtime scenarios
are owner QA obligations, **NOT RUN**. No tests/fixtures added. No gate marked passed.
