# FX32-S01/S02 — local pure Developer Toolbox slice

Status: implementation overlay for PR #4 under `DEC-20260909-014`.

This slice makes a bounded, memory-only subset of the Developer Toolbox
available to an authenticated owner. It does not enable network tools,
provider calls, code execution, history persistence, automatic clipboard
access or Save-to-Snippet.

## Goal and traceability

- `NXG-FX32-G01`: pure utilities are deterministic/bounded and never execute
  pasted input.
- `NXG-FX32-G02`: output labels preserve security meaning (legacy hashes are
  warnings; JSON is data; no JWT verification claim).
- `NXG-FX32-G03`: input/output remain in memory and network capability is not
  expanded.
- Source rows: `toolbox.catalog.read`, `toolbox.base64.run`,
  `toolbox.url_codec.run`, `toolbox.html_codec.run`, `toolbox.hash.run`,
  `toolbox.uuid.run`, `toolbox.password.run`, `toolbox.json.run`,
  `toolbox.regex.run`.

## API contract

- `GET /api/v1/developer/tools` returns the allowlisted tool catalog for the
  current authenticated PersonalSpace.
- `POST /api/v1/developer/tools/run` accepts `{ toolCode, input, options }`.
  `input` is UTF-8 bounded to 1 MiB. Output is bounded to 2 MiB. The service
  resolves the exact `FX32` action key from the server-side catalog and reads
  current module/action authority from SQL on every request.
- A successful response is `{ toolCode, output, warning, errorPath,
  durationMilliseconds }`. No input/output is logged or persisted.
- Invalid tool options, parser errors and regex timeouts are safe `problem+json`
  responses; no partial output is labelled successful.

## Implemented operations

| Tool | Options | Boundary |
| --- | --- | --- |
| Base64 | `operation=encode|decode` | UTF-8 only; invalid input rejected |
| URL codec | `operation=encode|decode` | Text transform only; never fetches |
| HTML entities | `operation=encode|decode` | Text transform only; rendered output remains escaped |
| Hash | SHA-256/384/512, MD5/SHA-1 checksum | MD5/SHA-1 carry a legacy warning |
| UUID | `count=1..20` | `Guid.NewGuid`, no persistence |
| Password | `length=15..128` | CSPRNG; output shown once in memory |
| JSON formatter | `indent=true|false` | `JsonDocument`; no code execution |
| Regex tester | bounded pattern/sample, optional `ignoreCase` | 100 KiB sample, 10,000-char pattern, 250 ms timeout, 100 matches |

Network HTTP/DNS, XML/YAML/CSV conversion, formatters, QR, certificate
inspection, history/favorites and Save-to-Snippet remain gated until their
own contract/evidence is complete. The UI states this boundary explicitly.

## Security and UX

- Session authority is the Secure/HttpOnly cookie; CSRF is required for the
  POST route. No bearer token or browser storage is used.
- The frontend exposes explicit tool/action labels, loading/empty/error states,
  bounded input, explicit copy, and a memory-only privacy notice. It never
  reads the clipboard automatically or assumes a client-side grant.
- The service is local-only and has no `HttpClient`, external URL navigation,
  secret persistence or provider adapter.

## Evidence

This code-only run does not add functional tests, fixtures or demo records.
Static/build checks are recorded in the PR body. SQL migration/runtime,
endpoint behavior, browser and QA verification remain owner-run dependencies.
