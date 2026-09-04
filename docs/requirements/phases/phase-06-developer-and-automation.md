# Phase 6 — Developer Toolbox, GitHub Discovery and Automation

**Phase ID:** `NX-PH-06`  
**Version:** `1.1-draft`  
**Outcome:** User dùng utility an toàn, khám phá public GitHub repositories và cấu hình automation có giới hạn trong đúng Space/module authority, quan sát được và không tạo side effect trùng.  
**Depends on:** Module Platform/Space/membership, jobs/scheduler, notifications, Vault references, audit, search, external HTTP/SSRF policy.

## 1. Scope decisions

- `DEC-PRD-012`: khóa Developer Toolbox P0 tools và client/server execution location.
- `DEC-PRD-013`: chọn Automation v1 là scheduler/action pipeline giới hạn hay workflow graph.
- `DEC-TEC-008`: job engine và retry/concurrency model.
- `DEC-SEC-007`: egress/SSRF/webhook security.

Không bật generic server-side HTTP Client, webhook forwarding hoặc arbitrary code/script execution trước security review.

Mọi tool/trigger/action/provider trong phase này là contribution do Nexora developer đăng ký qua Module Platform. User/Admin chỉ enable, configure và sử dụng capability đã ship; không upload code, package, script hoặc tự tạo module executable.

## 2. Scope proposal

### Developer Toolbox P0

JSON formatter/validator, JSON↔XML/YAML (safe subset), Base64, URL/HTML encode/decode, hash/checksum, UUID/GUID, Unix timestamp/timezone/date calculator, Cron builder, Regex tester with safety limit, Markdown preview sanitized, text diff, QR code, color converter.

### Developer Toolbox P1

CSV viewer, JWT decoder, SQL/HTML/CSS/JavaScript/C# formatter, URL parser/header inspector, certificate viewer. Generic HTTP Client/DNS/IP/Webhook Tester chỉ sau network policy; không nhất thiết chạy server-side.

### GitHub Discovery P0

Top 10 New, Top 10 Weekly Popular (confirmed definition), repository detail, language/topic/date/min-stars filter, manual refresh, last updated/cache status, public read-only API.

### GitHub Discovery P1

Snapshots/ranking history, saved repositories/filters, trend deltas.

### Automation P0

Automation definitions, limited approved triggers/actions, schedule, enable/disable, manual run, run history/logs, retry/idempotency/concurrency, failure alerts, secret references, permission/audit.

### Automation P1

Inbound/outbound webhooks, multi-step workflow, data mapping, conditional branches, monitoring views, data sync và n8n integration after contract/security decision.

### Out of scope

Arbitrary user code/shell/SQL execution, unrestricted server network proxy, GitHub write/private data/OAuth, automated agent/LLM tool calling, User/Admin-created modules hoặc executable upload, unreviewed third-party plugin marketplace, passing Nexora master keys/database credentials to n8n.

## 3. Developer Toolbox — common requirements

| ID | Pri | Requirement | Acceptance criteria |
|---|---:|---|---|
| `P06-TBX-001` | P0 | Mỗi tool có input/output contract, size limit, deterministic behavior và clear/reset/copy controls. | Boundary/invalid/large input tests; no browser freeze/server exhaustion. |
| `P06-TBX-002` | P0 | Input không persist hoặc gửi server mặc định nếu tool có thể chạy client-side. | Network/storage inspection confirms; UI labels execution/privacy. |
| `P06-TBX-003` | P0 | Tool output được render như data, không execute HTML/script/Markdown unsafe content. | Malicious corpus cannot execute. |
| `P06-TBX-004` | P0 | Tool không tự log/store clipboard/input chứa potential secret. | Marker secret absent logs/audit/history/telemetry. |
| `P06-TBX-005` | P0 | Copy/download requires explicit user action and preserves declared encoding/newline semantics. | Round-trip fixtures pass; filename safe. |
| `P06-TBX-006` | P1 | Optional history/favorites are opt-in, private, clearable and warn about sensitive inputs. | Default off; cross-Space/user/storage tests pass. |
| `P06-TBX-008` | P0 | Tool manifest khai báo `supportedSpaces`, execution location, permissions, input classification và contribution routes. | Unsupported Space/disabled module/direct route bị chặn; manifest contract tests pass. |
| `P06-TBX-007` | P0 | Mobile UI supports paste/run/copy/error without horizontal page break; code panes may scroll internally. | Representative viewport and keyboard tests. |

## 4. Data, encoding và format tools

| ID | Pri | Requirement | Acceptance criteria |
|---|---:|---|---|
| `P06-DAT-001` | P0 | JSON formatter/validator reports parse location/path safely and supports compact/pretty output. | Fixture suite with Unicode, numbers, deep/large limits, duplicate keys policy. |
| `P06-DAT-002` | P0/P1 | JSON↔XML/YAML conversion documents lossy mappings, arrays, null, types, attributes và unsafe YAML/XML features. | XXE/entity expansion/custom object construction disabled; round-trip limitations visible. |
| `P06-DAT-003` | P1 | CSV viewer requires delimiter/quote/encoding limits and renders cells as text. | Formula injection warning/safe export; huge rows bounded. |
| `P06-DAT-004` | P0 | Diff viewer defines line/character mode, newline normalization and input size/time bounds. | Deterministic fixture output; malicious HTML safe. |
| `P06-ENC-001` | P0 | Base64/URL/HTML encode/decode distinguishes text encoding (UTF-8 default) and invalid input behavior. | Unicode/binary-invalid fixtures; no double-encode claim. |
| `P06-ENC-002` | P1 | Unicode/Hex/Binary conversions define code point vs UTF-8 byte semantics. | Emoji/surrogate/non-ASCII fixtures pass. |

## 5. Security, date/time và development tools

| ID | Pri | Requirement | Acceptance criteria |
|---|---:|---|---|
| `P06-SEC-001` | P1 | JWT Decoder decodes header/payload locally and clearly states decode ≠ signature verification. | `alg:none`/malformed token handled; token not transmitted/persisted. |
| `P06-SEC-002` | P0 | Hash/checksum tool labels algorithm and whether cryptographically secure; does not present fast hash as password storage. | UI warning/test vectors pass. |
| `P06-SEC-003` | P0 | Password generator uses CSPRNG, configurable length/sets and entropy caveat; generated value not logged. | Implementation review/test; value only client/user response. |
| `P06-SEC-004` | P1 | Certificate viewer parses supplied certificate locally/file-limited; verification claims require trust-store/time/hostname semantics. | Malformed/huge certificate safe; no false “secure” label. |
| `P06-SEC-005` | P0 | UUID/GUID generator uses selected version and accurate randomness/time/privacy description. | Standard format/version fixtures pass. |
| `P06-TME-001` | P0 | Unix converter declares seconds vs milliseconds, timezone and supported range. | Epoch/negative/DST boundary tests. |
| `P06-TME-002` | P0 | Timezone/date calculator uses IANA zone rules where applicable and handles ambiguous/nonexistent local times explicitly. | DST fixtures pass. |
| `P06-TME-003` | P0 | Cron builder declares cron dialect/field count/timezone and previews next runs. | Golden expressions; invalid/impossible schedule rejected. |
| `P06-DEV-001` | P0 | Regex tester has engine documented plus input/pattern/time/complexity guard against ReDoS. | Catastrophic pattern terminates within bound. |
| `P06-DEV-002` | P0 | Markdown preview uses same or stricter sanitization as Knowledge content. | XSS/unsafe link fixtures safe. |
| `P06-DEV-003` | P1 | Formatters are version-pinned, deterministic and never execute code/query. | Fixture snapshot tests; parse error safe. |
| `P06-DEV-004` | P0 | QR generator encodes explicit text only; no automatic URL navigation or tracking. | Unicode/size/error correction fixtures; download safe. |

## 6. Network/API tools security gate

| ID | Pri | Requirement | Acceptance criteria |
|---|---:|---|---|
| `P06-NET-001` | P1 | URL parser is pure/local and separates scheme, authority, credentials, host, port, path, query, fragment safely. | Confusable/IPv6/encoded fixtures pass; credential warning. |
| `P06-NET-002` | P1 | Server-side HTTP Client/Webhook Tester default disabled until egress allow/deny, DNS rebinding, redirect, port, method, header, body, timeout/size and audit policy approved. | SSRF suite blocks loopback/private/link-local/metadata targets through redirects/DNS. |
| `P06-NET-003` | P1 | User-supplied Authorization/Cookie/token headers use ephemeral secret fields and never enter history/log. | Marker secret redaction; browser storage clean. |
| `P06-NET-004` | P1 | Response body/header display bounded and sanitized; Set-Cookie/credential data not persisted by default. | Huge/stream/binary/malicious response safe. |
| `P06-NET-005` | P1 | DNS/IP information tool distinguishes resolver result, public data source and privacy limitations; private target rules apply. | Provider errors/rate limits visible, not fabricated. |

## 7. GitHub Discovery — access model

| ID | Pri | Requirement | Acceptance criteria |
|---|---:|---|---|
| `P06-GHA-001` | P0 | Chỉ dùng GitHub public repository read data; user không cần GitHub Login/OAuth/linking. | No account authorization screen/token request from end user. |
| `P06-GHA-002` | P0 | Không có Star/Fork/Create/Issue/PR/modify/private-repo action. | UI/API routes absent; connector client exposes approved read endpoints only. |
| `P06-GHA-003` | P0 | Nếu server token dùng để tăng rate limit, đó là system integration secret, không phải user GitHub identity. | Stored via secure setting; masked/rotatable; least scopes/no write; audit config change. |
| `P06-GHA-004` | P0 | GitHub response là untrusted external content và được sanitize/encode. | Malicious repo name/description/topic/homepage cannot XSS/open unsafe scheme. |

## 8. GitHub ranking definitions

### 8.1 Top 10 New Repositories

`PROPOSED` definition: public non-fork repositories visible to selected API/query, ordered by `created_at DESC` with stable tie-break, limited 10. Nếu cần quality/spam/minimum metadata filter phải được Product Owner chốt và hiển thị rõ; không gọi là toàn bộ GitHub nếu API coverage không bảo đảm.

### 8.2 Top 10 Weekly Popular Repositories

Confirmed semantic:

1. repository `created_at` nằm trong current week window;
2. sort theo **total current star count** giảm dần;
3. lấy 10;
4. không phải stars gained during week.

Week boundary `PROPOSED`: ISO week Monday 00:00–next Monday 00:00 UTC; UI hiển thị exact range. Product Owner có thể đổi timezone nhưng snapshot phải lưu range.

| ID | Pri | Requirement | Acceptance criteria |
|---|---:|---|---|
| `P06-GHR-001` | P0 | Ranking query lưu/display definition, time window, fetched-at và source/cache state. | User biết data stale và exact week range. |
| `P06-GHR-002` | P0 | Tie-break deterministic sau primary sort. | Same snapshot returns stable order/page. |
| `P06-GHR-003` | P0 | Fork/archived/template/spam filters nếu áp dụng phải explicit và testable. | No undocumented exclusion; count/top logic golden tests. |
| `P06-GHR-004` | P0 | GitHub API incomplete/rate-limited/error result không được hiển thị như complete fresh ranking. | Degraded/partial/error label; last valid cache separated. |
| `P06-GHR-005` | P1 | Snapshot lưu rank, metrics, query/rule version và captured-at. | Historical rank không thay đổi khi live repo metrics update. |

## 9. GitHub list, detail, filters và refresh

| ID | Pri | Requirement | Acceptance criteria |
|---|---:|---|---|
| `P06-GHD-001` | P0 | Card/detail supports name/full name, owner/avatar, description, URL, stars, forks, watchers semantics, open issues, language, topics, license, default branch, dates, homepage. | Missing fields labeled/omitted safely; numbers map correct API fields. |
| `P06-GHD-002` | P0 | Actions limited `Open on GitHub`, `Copy Repository URL`, `Copy Clone URL`. | URL scheme/domain validated; no credential injected. |
| `P06-GHD-003` | P0 | Filters language/topic/created range/min stars validated and reflected in query/URL/state. | Unsupported/invalid filter safe; reset works; ranking definition remains clear. |
| `P06-GHD-004` | P0 | Manual refresh idempotent/coalesced, rate-limit aware; UI shows last attempt/success/cache age. | Concurrent clicks do not storm API; 403/429 reset info safe. |
| `P06-GHD-005` | P0 | Cache key includes query/filter/window/rule version and has explicit freshness. | One filter cannot receive cached result of another; stale-while-error labeled. |
| `P06-GHD-006` | P1 | Saved repository/filter thuộc User hoặc Space được manifest hỗ trợ; saving không gọi GitHub write API. | Cross-Space isolation; deleted public repo handled as unavailable. |

## 10. Automation model

### 10.1 Core concepts

- `AutomationDefinition`: owning Space, creator, name, version, enabled state, trigger, approved actions, mappings, policy.
- `Trigger`: manual, schedule P0; event/webhook P1.
- `Run`: immutable execution attempt with status/times/version/correlation.
- `StepRun`: bounded input/output metadata with redaction.
- `SecretReference`: reference tới Vault/integration secret, never copied into definition/log.

### 10.2 Lifecycle

`Draft → Validated → Enabled ↔ Disabled → Archived/Deleted`; published/enabled version immutable for a run. Editing creates new version or safe atomic replacement; queued run records version used.

| ID | Pri | Requirement | Acceptance criteria |
|---|---:|---|---|
| `P06-AUT-001` | P0 | User tạo automation chỉ từ trigger/action types do trusted Nexora module đăng ký và được enable trong current Space. | Unknown/disabled/arbitrary code/action rejected server-side. |
| `P06-AUT-002` | P0 | Validate trước enable: schema, references, permissions, schedule/timezone, cycles/limits. | Invalid definition cannot enable/run. |
| `P06-AUT-003` | P0 | Schedule supports timezone/start/end/missed-run policy and next-run preview. | DST/restart/missed boundary golden tests. |
| `P06-AUT-004` | P0 | Manual run và scheduled run create unique run with idempotency/concurrency policy. | Duplicate trigger/retry does not duplicate business side effect. |
| `P06-AUT-005` | P0 | Run executes với explicit authority snapshot/delegation và rechecks current Space, membership, module, resource, permission và secret availability trước side effect. | Removed/disabled user, revoked membership/grant, disabled module hoặc deleted resource prevents action safely. |
| `P06-AUT-006` | P0 | Enable/disable/update/delete/retry/cancel/manual execute are separate authorized/audited actions. | Admin `view` cannot execute/configure; stale queued run obeys disable policy. |
| `P06-AUT-007` | P0 | Run states: `Queued`, `Running`, `Succeeded`, `PartiallySucceeded` (if multi-step), `Failed`, `Cancelled`, `TimedOut`, `Skipped`. | Valid transitions enforced; restart recovers stuck runs by policy. |
| `P06-AUT-008` | P0 | Per-automation/Workspace/user/system concurrency, timeout, retry/backoff and max attempts configurable within admin bounds. | Runaway/retry storm prevented; reason visible. |
| `P06-AUT-009` | P0 | Inputs/outputs/logs are bounded, redacted and retention-controlled. | Marker secret absent; huge payload not persisted. |
| `P06-AUT-010` | P0 | Failure emits idempotent Notification according to preference/severity. | One logical final failure alert; intermediate retry not spam unless configured. |
| `P06-AUT-011` | P1 | Dry-run/test mode must not claim no side effect unless every action supports verified simulation. | Unsupported action clearly blocks or labels test as real. |
| `P06-AUT-012` | P1 | Definition export/import excludes secret values and validates action availability/version. | Imported definition requires rebind secret refs; cannot elevate permission. |
| `P06-AUT-013` | P0 | Module disable/upgrade/uninstall validates active definitions, queued runs, schedules và contribution versions. | Disable stops new triggers, queued run follows explicit cancel/skip policy, data/history remains recoverable. |
| `P06-AUT-014` | P0 | Workspace Automation không tiếp tục dưới quyền creator sau khi membership/role mất hiệu lực. | Creator removal hoặc permission downgrade is detected before run/step; no ambient authority. |

## 11. Approved P0 automation use cases proposal

- Task/Event/Bill/Subscription reminder schedules (system-managed definition may remain internal).
- Shopee/News/GitHub refresh jobs with approved adapters.
- Backup trigger only for privileged Admin/SuperAdmin; restore never generic user action.
- User-defined P0: scheduled in-app reminder and approved refresh action; broader write workflows P1.

Internal jobs and user-visible automations must remain distinct even if sharing execution engine.

## 12. Webhooks và n8n P1

| ID | Pri | Requirement | Acceptance criteria |
|---|---:|---|---|
| `P06-WHK-001` | P1 | Inbound webhook endpoint uses unguessable ID plus signature/auth where supported, replay window, idempotency, size/rate limits. | Invalid/replayed/oversized request rejected and safely logged. |
| `P06-WHK-002` | P1 | Outbound webhook enforces egress policy, secret headers via references, signed payload option, timeout/retry/delivery history. | SSRF/redirect/DNS tests pass; secret absent logs. |
| `P06-N8N-001` | P1 | n8n integration contract defines direction, events/actions, auth, versioning, retry, idempotency and data classification. | No direct DB/master-key sharing; disconnect/revoke works. |
| `P06-N8N-002` | P1 | Nexora remains usable without n8n; integration outage is degraded, not core failure. | Core CRUD/jobs unaffected; status visible. |

## 13. Permissions và audit

- `developer_tools.view/execute/configure`; `github_discovery.view/execute/configure`; `automation.view/create/update/delete/execute/configure/access_all`.
- Network tool execution/configuration, automation enable/execute/retry/cancel, secret binding, webhook config, privileged runs and backup trigger audited.
- Utility input/output and secret value never copied into Audit Log.
- Saved GitHub/tool history and automation definitions follow declared Personal/Workspace support; per-user preferences remain private.

## 14. Critical edge cases

- Catastrophic regex, XML entity expansion, YAML unsafe object, huge diff/CSV, malicious Markdown.
- SSRF via redirect, DNS rebinding, encoded IP, IPv6, userinfo, alternate scheme/port.
- GitHub rate limit, empty/incomplete search, repo renamed/deleted, timezone week boundary, tied stars.
- Scheduler restart/DST/missed run, duplicate webhook, stale definition version, revoked secret/permission.
- Workflow partial success, cancel race, retry after side effect succeeded but acknowledgement failed.
- n8n disconnected or sends old schema/version.
- Workspace/module disabled, Member removed hoặc action contribution upgraded giữa enqueue và execute.

## 15. Verification scenarios

1. Marker secret processed by every relevant utility never enters network/storage/log unless user explicitly uses an approved server tool.
2. Malicious format/regex/Markdown/network fixtures meet safety/time/size bounds.
3. GitHub golden dataset produces exact Top 10 New/Weekly order, tie-break and week range.
4. GitHub 429/partial response shows stale/degraded, not fresh complete ranking.
5. Same automation trigger delivered twice yields one logical side effect and traceable run attempts.
6. Permission/secret revoked after queue but before run causes safe skip/fail, not unauthorized action.
7. Webhook/HTTP tests block all prohibited network targets if P1 ships.
8. Disable module hoặc remove Workspace Member sau enqueue làm run skip/fail đúng policy, không tạo side effect.
9. User/Admin không thể upload/đăng ký executable module, trigger hoặc action ngoài developer-shipped registry.

## 16. Exit criteria

- Toolbox P0 list/execution/privacy/security approved and golden fixtures pass.
- GitHub definition/API/rate-limit/cache tests pass; no GitHub auth/write path exists.
- Automation authority/idempotency/retry/concurrency/redaction/cancellation tests pass.
- SSRF/network surface disabled or passes approved security suite.
- Job/run/failure metrics, history and alerts are operational.
- Module contribution/version lifecycle, supported Space và removed-member/disabled-module tests pass.
- No Critical/High findings; responsive/accessibility P0 journeys pass.
