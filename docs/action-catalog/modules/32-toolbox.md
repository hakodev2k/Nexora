# FX-32 — Developer Toolbox: actions

Catalog v1 · 2026-09-07 · Docs-only. New key decomposition = Resolved delegated; source business decisions giữ nguyên; Blocked rows không được kích hoạt bằng grant.

## Sources và phạm vi

- [Feature / validation / state graph](../../features/32-developer-toolbox.md) — FX-32-BR-001, FX-32-BR-002, FX-32-BR-003, FX-32-BR-004, FX-32-BR-005. Các BR này áp cho feature; không gán sai một BR duy nhất cho mọi row.
- [UX specification](../../ux-ui/modules/32-developer-toolbox.md); [exact screen bindings](../06-screen-bindings.md).
- [Authorization contract](../00-authorization-contract.md), [semantic field guards](../01-composition-and-field-guards.md), [SDK contract](../04-module-action-contract.md).
- Namespace `toolbox` là stable logical key, bind installed ModuleId trong manifest. **Local pure tools không lưu input mặc định; network tools là quyền/capability riêng**.

## Catalog

“All prerequisites” ở row bao gồm explicit Requires **và** source/dynamic dependencies trong guard; danh sách Requires trống không có nghĩa bỏ owner/module/lifecycle checks. Every action denies unknown fields and unapproved semantic changes. Source state matrix luôn kiểm tra ở handler, không suy quyền từ verb hoặc tên button.

| Action key / hành vi | Kind / context | Admin checkbox? | Risk (DB mapping) | Status / gate | UI entry |
| --- | --- | --- | --- | --- | --- |
| <a id="toolbox-catalog-read"></a>`toolbox.catalog.read` — Tìm/xem tools | QUERY / SELF | Yes, gated | Normal (Normal) | Resolved delegated: action contract; source business rules unchanged | FX32-S01 |
| <a id="toolbox-base64-run"></a>`toolbox.base64.run` — Base64 encode/decode | LOCAL / SELF | Yes, gated | Normal (Normal) | Resolved delegated: action contract; source business rules unchanged | FX32-S02 |
| <a id="toolbox-url-codec-run"></a>`toolbox.url_codec.run` — URL encode/decode | LOCAL / SELF | Yes, gated | Normal (Normal) | Resolved delegated: action contract; source business rules unchanged | FX32-S02 |
| <a id="toolbox-html-codec-run"></a>`toolbox.html_codec.run` — HTML entities | LOCAL / SELF | Yes, gated | Normal (Normal) | Resolved delegated: action contract; source business rules unchanged | FX32-S02 |
| <a id="toolbox-hash-run"></a>`toolbox.hash.run` — Hash/checksum | LOCAL / SELF | Yes, gated | Normal (Normal) | Resolved delegated: action contract; source business rules unchanged | FX32-S02 |
| <a id="toolbox-password-run"></a>`toolbox.password.run` — Generate password | LOCAL / SELF | Yes, gated | Normal (Normal) | Resolved delegated: action contract; source business rules unchanged | FX32-S02 |
| <a id="toolbox-uuid-run"></a>`toolbox.uuid.run` — Generate UUID | LOCAL / SELF | Yes, gated | Normal (Normal) | Resolved delegated: action contract; source business rules unchanged | FX32-S02 |
| <a id="toolbox-datetime-run"></a>`toolbox.datetime.run` — Timestamp/date calculator | LOCAL / SELF | Yes, gated | Normal (Normal) | Resolved delegated: action contract; source business rules unchanged | FX32-S02 |
| <a id="toolbox-json-run"></a>`toolbox.json.run` — JSON format/validate | LOCAL / SELF | Yes, gated | Normal (Normal) | Resolved delegated: action contract; source business rules unchanged | FX32-S02 |
| <a id="toolbox-xml-run"></a>`toolbox.xml.run` — XML format/validate | LOCAL / SELF | Yes, gated | Normal (Normal) | Resolved delegated: action contract; source business rules unchanged | FX32-S02 |
| <a id="toolbox-yaml-run"></a>`toolbox.yaml.run` — YAML format/validate | LOCAL / SELF | Yes, gated | Normal (Normal) | Resolved delegated: action contract; source business rules unchanged | FX32-S02 |
| <a id="toolbox-csv-run"></a>`toolbox.csv.run` — CSV view/convert | LOCAL / SELF | Yes, gated | Normal (Normal) | Resolved delegated: action contract; source business rules unchanged | FX32-S02 |
| <a id="toolbox-data-convert-run"></a>`toolbox.data_convert.run` — JSON/XML/YAML/CSV conversion | LOCAL / SELF | Yes, gated | Normal (Normal) | Resolved delegated: action contract; source business rules unchanged | FX32-S02 |
| <a id="toolbox-regex-run"></a>`toolbox.regex.run` — Regex test | LOCAL / SELF | Yes, gated | Normal (Normal) | Resolved delegated: action contract; source business rules unchanged | FX32-S02 |
| <a id="toolbox-text-diff-run"></a>`toolbox.text_diff.run` — Text diff | LOCAL / SELF | Yes, gated | Normal (Normal) | Resolved delegated: action contract; source business rules unchanged | FX32-S02 |
| <a id="toolbox-color-run"></a>`toolbox.color.run` — Color conversion | LOCAL / SELF | Yes, gated | Normal (Normal) | Resolved delegated: action contract; source business rules unchanged | FX32-S02 |
| <a id="toolbox-qr-run"></a>`toolbox.qr.run` — QR generation | LOCAL / SELF | Yes, gated | Normal (Normal) | Resolved delegated: action contract; source business rules unchanged | FX32-S02 |
| <a id="toolbox-jwt-run"></a>`toolbox.jwt.run` — JWT decode only | LOCAL / SELF | Yes, gated | Normal (Normal) | Resolved delegated: action contract; source business rules unchanged | FX32-S02 |
| <a id="toolbox-cron-run"></a>`toolbox.cron.run` — Cron expression builder | LOCAL / SELF | Yes, gated | Normal (Normal) | Resolved delegated: action contract; source business rules unchanged | FX32-S02 |
| <a id="toolbox-markdown-run"></a>`toolbox.markdown.run` — Markdown preview | LOCAL / SELF | Yes, gated | Normal (Normal) | Resolved delegated: action contract; source business rules unchanged | FX32-S02 |
| <a id="toolbox-code-format-run"></a>`toolbox.code_format.run` — SQL/HTML/CSS/JS/C# formatter | LOCAL / SELF | Yes, gated | Normal (Normal) | Resolved delegated: action contract; source business rules unchanged | FX32-S02 |
| <a id="toolbox-url-parse-run"></a>`toolbox.url_parse.run` — URL parser | LOCAL / SELF | Yes, gated | Normal (Normal) | Resolved delegated: action contract; source business rules unchanged | FX32-S02 |
| <a id="toolbox-headers-run"></a>`toolbox.headers.run` — Inspect pasted headers | LOCAL / SELF | Yes, gated | Normal (Normal) | Resolved delegated: action contract; source business rules unchanged | FX32-S02 |
| <a id="toolbox-certificate-run"></a>`toolbox.certificate.run` — Parse pasted public certificate | LOCAL / SELF | Yes, gated | Normal (Normal) | Resolved delegated: action contract; source business rules unchanged | FX32-S02 |
| <a id="toolbox-network-http"></a>`toolbox.network.http` — HTTP request test | COMMAND / SELF | Yes, gated | Network (Administrative) | Blocked Q-07 | FX32-S04 |
| <a id="toolbox-network-dns"></a>`toolbox.network.dns` — DNS lookup | COMMAND / SELF | Yes, gated | Network (Administrative) | Blocked Q-07 | FX32-S04 |
| <a id="toolbox-output-copy"></a>`toolbox.output.copy` — Copy current output | LOCAL / SELF | No | Disclosure (Sensitive) | Resolved delegated: action contract; source business rules unchanged | FX32-S02 |
| <a id="toolbox-output-download"></a>`toolbox.output.download` — Download current output | LOCAL / SELF | No | Disclosure (Sensitive) | Resolved delegated: action contract; source business rules unchanged | FX32-S02 |
| <a id="toolbox-output-save-snippet"></a>`toolbox.output.save_snippet` — Lưu output vào Snippet | COMPOSITE / SELF | Yes, gated | Normal (Normal) | Resolved delegated: action contract; source business rules unchanged | FX32-S02 |
| <a id="toolbox-history-read"></a>`toolbox.history.read` — Xem opted-in tool history | QUERY / SELF | Yes, gated | Normal (Normal) | Resolved delegated: action contract; source business rules unchanged | FX32-S03 |
| <a id="toolbox-history-save"></a>`toolbox.history.save` — Opt-in lưu safe input/output | COMMAND / SELF | Yes, gated | Normal (Normal) | Resolved delegated: action contract; source business rules unchanged | FX32-S03 |
| <a id="toolbox-history-delete"></a>`toolbox.history.delete` — Xóa saved run | COMMAND / SELF | Yes, gated | Normal (Normal) | Resolved delegated: action contract; source business rules unchanged | FX32-S03 |
| <a id="toolbox-support-read"></a>`toolbox.support.read` — Xem safe support projection của module | QUERY / SUPPORT | Yes, gated | Sensitive (Sensitive) | Resolved delegated: diagnostic projection only; private body excluded unless source scope explicitly permits | FX05-S03, FX05-S05 |

## Điều kiện riêng theo operation

| Action | Guard / validation / effects | Explicit dependencies bổ sung |
| --- | --- | --- |
| `toolbox.catalog.read` | Local pure tools không lưu input mặc định; network tools là quyền/capability riêng; Local pure tools không lưu input mặc định; network tools là quyền/capability riêng | Common + dynamic source/provider guards |
| `toolbox.base64.run` | Pure local processing; bounded input/time; escape preview; không code execution, JWT signature verification claim hoặc private-key upload; Local pure tools không lưu input mặc định; network tools là quyền/capability riêng | Common + dynamic source/provider guards |
| `toolbox.url_codec.run` | Pure local processing; bounded input/time; escape preview; không code execution, JWT signature verification claim hoặc private-key upload; Local pure tools không lưu input mặc định; network tools là quyền/capability riêng | Common + dynamic source/provider guards |
| `toolbox.html_codec.run` | Pure local processing; bounded input/time; escape preview; không code execution, JWT signature verification claim hoặc private-key upload; Local pure tools không lưu input mặc định; network tools là quyền/capability riêng | Common + dynamic source/provider guards |
| `toolbox.hash.run` | Pure local processing; bounded input/time; escape preview; không code execution, JWT signature verification claim hoặc private-key upload; Local pure tools không lưu input mặc định; network tools là quyền/capability riêng | Common + dynamic source/provider guards |
| `toolbox.password.run` | Pure local processing; bounded input/time; escape preview; không code execution, JWT signature verification claim hoặc private-key upload; Local pure tools không lưu input mặc định; network tools là quyền/capability riêng | Common + dynamic source/provider guards |
| `toolbox.uuid.run` | Pure local processing; bounded input/time; escape preview; không code execution, JWT signature verification claim hoặc private-key upload; Local pure tools không lưu input mặc định; network tools là quyền/capability riêng | Common + dynamic source/provider guards |
| `toolbox.datetime.run` | Pure local processing; bounded input/time; escape preview; không code execution, JWT signature verification claim hoặc private-key upload; Local pure tools không lưu input mặc định; network tools là quyền/capability riêng | Common + dynamic source/provider guards |
| `toolbox.json.run` | Pure local processing; bounded input/time; escape preview; không code execution, JWT signature verification claim hoặc private-key upload; Local pure tools không lưu input mặc định; network tools là quyền/capability riêng | Common + dynamic source/provider guards |
| `toolbox.xml.run` | Pure local processing; bounded input/time; escape preview; không code execution, JWT signature verification claim hoặc private-key upload; Local pure tools không lưu input mặc định; network tools là quyền/capability riêng | Common + dynamic source/provider guards |
| `toolbox.yaml.run` | Pure local processing; bounded input/time; escape preview; không code execution, JWT signature verification claim hoặc private-key upload; Local pure tools không lưu input mặc định; network tools là quyền/capability riêng | Common + dynamic source/provider guards |
| `toolbox.csv.run` | Pure local processing; bounded input/time; escape preview; không code execution, JWT signature verification claim hoặc private-key upload; Local pure tools không lưu input mặc định; network tools là quyền/capability riêng | Common + dynamic source/provider guards |
| `toolbox.data_convert.run` | Pure local processing; bounded input/time; escape preview; không code execution, JWT signature verification claim hoặc private-key upload; Local pure tools không lưu input mặc định; network tools là quyền/capability riêng | Common + dynamic source/provider guards |
| `toolbox.regex.run` | Pure local processing; bounded input/time; escape preview; không code execution, JWT signature verification claim hoặc private-key upload; Local pure tools không lưu input mặc định; network tools là quyền/capability riêng | Common + dynamic source/provider guards |
| `toolbox.text_diff.run` | Pure local processing; bounded input/time; escape preview; không code execution, JWT signature verification claim hoặc private-key upload; Local pure tools không lưu input mặc định; network tools là quyền/capability riêng | Common + dynamic source/provider guards |
| `toolbox.color.run` | Pure local processing; bounded input/time; escape preview; không code execution, JWT signature verification claim hoặc private-key upload; Local pure tools không lưu input mặc định; network tools là quyền/capability riêng | Common + dynamic source/provider guards |
| `toolbox.qr.run` | Pure local processing; bounded input/time; escape preview; không code execution, JWT signature verification claim hoặc private-key upload; Local pure tools không lưu input mặc định; network tools là quyền/capability riêng | Common + dynamic source/provider guards |
| `toolbox.jwt.run` | Pure local processing; bounded input/time; escape preview; không code execution, JWT signature verification claim hoặc private-key upload; Local pure tools không lưu input mặc định; network tools là quyền/capability riêng | Common + dynamic source/provider guards |
| `toolbox.cron.run` | Pure local processing; bounded input/time; escape preview; không code execution, JWT signature verification claim hoặc private-key upload; Local pure tools không lưu input mặc định; network tools là quyền/capability riêng | Common + dynamic source/provider guards |
| `toolbox.markdown.run` | Pure local processing; bounded input/time; escape preview; không code execution, JWT signature verification claim hoặc private-key upload; Local pure tools không lưu input mặc định; network tools là quyền/capability riêng | Common + dynamic source/provider guards |
| `toolbox.code_format.run` | Pure local processing; bounded input/time; escape preview; không code execution, JWT signature verification claim hoặc private-key upload; Local pure tools không lưu input mặc định; network tools là quyền/capability riêng | Common + dynamic source/provider guards |
| `toolbox.url_parse.run` | Pure local processing; bounded input/time; escape preview; không code execution, JWT signature verification claim hoặc private-key upload; Local pure tools không lưu input mặc định; network tools là quyền/capability riêng | Common + dynamic source/provider guards |
| `toolbox.headers.run` | Pure local processing; bounded input/time; escape preview; không code execution, JWT signature verification claim hoặc private-key upload; Local pure tools không lưu input mặc định; network tools là quyền/capability riêng | Common + dynamic source/provider guards |
| `toolbox.certificate.run` | Pure local processing; bounded input/time; escape preview; không code execution, JWT signature verification claim hoặc private-key upload; Local pure tools không lưu input mặc định; network tools là quyền/capability riêng | Common + dynamic source/provider guards |
| `toolbox.network.http` | Server-enforced destination policy, SSRF/egress limits; credentials only Vault ref; không arbitrary intranet probe; Local pure tools không lưu input mặc định; network tools là quyền/capability riêng | Common + dynamic source/provider guards |
| `toolbox.network.dns` | Server-enforced destination policy, SSRF/egress limits; credentials only Vault ref; không arbitrary intranet probe; Local pure tools không lưu input mặc định; network tools là quyền/capability riêng | Common + dynamic source/provider guards |
| `toolbox.output.copy` | Current tool.run capability; no extra data fetch/storage; no secret toast; Local pure tools không lưu input mặc định; network tools là quyền/capability riêng | Common + dynamic source/provider guards |
| `toolbox.output.download` | Current tool.run capability; no extra data fetch/storage; no secret toast; Local pure tools không lưu input mặc định; network tools là quyền/capability riêng | Common + dynamic source/provider guards |
| `toolbox.output.save_snippet` | Explicit preview, no secret auto-save; actual snippet create and enabled module; Local pure tools không lưu input mặc định; network tools là quyền/capability riêng | `snippets.snippet.create` |
| `toolbox.history.read` | Local pure tools không lưu input mặc định; network tools là quyền/capability riêng; Local pure tools không lưu input mặc định; network tools là quyền/capability riêng | Common + dynamic source/provider guards |
| `toolbox.history.save` | No sensitive payload/browser persistent storage; owner server history policy; local pure runs không tự log input; Local pure tools không lưu input mặc định; network tools là quyền/capability riêng | Common + dynamic source/provider guards |
| `toolbox.history.delete` | No sensitive payload/browser persistent storage; owner server history policy; local pure runs không tự log input; Local pure tools không lưu input mặc định; network tools là quyền/capability riêng | Common + dynamic source/provider guards |
| `toolbox.support.read` | Qualified Admin + support.session.open + current one-module consent OR authorized SuperAdmin Emergency; target enabled; approved redacted projection only; no owner history/export/reveal/linked-module expansion; Local pure tools không lưu input mặc định; network tools là quyền/capability riêng | Common + dynamic source/provider guards |

## Deny và UX contract

- Module off, grant missing/deny, resource wrong owner, disallowed lifecycle, current Q gate hoặc source dependency fail: không side effect; không dùng hidden button thay authorization.
- Before/after field diff được kiểm tra cho Save, import, version restore, bulk, scheduler và automation. Form không được gửi status/reveal/export/owner trong generic Update.
- Safe capability reason: ModuleUnavailable, ActionDenied, LifecycleLocked, DependencyUnavailable, DecisionBlocked hoặc StepUpRequired; unknown/wrong-owner resource trả unavailable chung để không enumerate.
- Grant không thay đổi state graph. Chỉ quyền đã cấp và hợp lệ mới xuất hiện enabled; permission editor có thể hiển thị blocked row để giải thích, không cho bật.
- Revocation và support/share/system contexts áp toàn bộ [common contract](../00-authorization-contract.md). Readonly projections không reuse full owner DTO.

## Acceptance tối thiểu

1. Với mỗi row: positive case đúng context/current state; wrong-owner và wrong-context negative; absent/deny Admin grant; module off; stale revision; lifecycle/Q gate.
2. COMMAND/COMPOSITE: request replay/idempotency, before-commit recheck; affected fields cần đủ action. QUERY: owner-scoped filtering trước count/pagination/projection, cache không rò source revoked.
3. LOCAL: keyboard/menu và tool entry cùng capability gate; không network/persist ngầm. SYSTEM: trusted caller, original authority và no UI grant.
4. Row nhạy cảm: no secret in response preview, toast, logs, URL, search, browser persistent storage; current recent-auth gate nếu required.
5. Nếu handler/source projection chưa có approved contract, action phải báo Blocked/Unavailable, không tự thực thi fallback rộng hơn.
