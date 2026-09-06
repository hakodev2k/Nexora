# Developer Toolbox

FX-32 · Feature specification · 2026-09-06 · Baseline requirements: d0d8418

**Trạng thái:** yêu cầu đã xác nhận được giữ nguyên; chi tiết bổ sung bên dưới là **Resolved (delegated)** theo DEC-GOV-001. Mục Q còn mở là proposal, chưa được duyệt. Tài liệu không cấp phép implement.

## Phạm vi và tham chiếu

Format/validate/convert JSON/XML/YAML/CSV; encoding/hash/UUID/password; date/time/regex/diff/text/QR/color; network tools có guard.

[DevToys](https://devtoys.app/): Các công cụ nhỏ chuyển đổi/định dạng dữ liệu; đây là sản phẩm desktop, chỉ tham chiếu hành vi tool.

**Áp dụng cho Nexora:** DevToys tham chiếu input/options/output; không chạy code User hoặc gửi nội dung đến AI.

**Màn hình:** `/developer/tools/:tool`. Routes là thiết kế đề xuất; không phải endpoint đã implement.

## Luồng sử dụng

1. Chọn tool → input/sample/options → Run/local preview → copy/download.
2. Parse error line/column, giữ input để sửa; reset có dirty warning.
3. Network tool explicit target/request preview → Send chỉ sau Q-07.

## Dữ liệu và validation

- ToolManifest version/schema/local-or-network/limits; pure input≤1MiB/output≤2MiB delegated.
- UTF-8/Base64/URL/HTML entities; SHA256/384/512, MD5/SHA1 chỉ checksum legacy; UUIDv4; ISO/epoch sec/ms.
- JSON/XML/YAML/CSV conversion options; regex bounded; textdiff; colorHEX/RGB/HSL/alpha; QRtext.
- JWT decode metadata không đồng nghĩa signature verified; generator dùng CSPRNG.

## Hành vi và lifecycle

- **FX-32-BR-001:** Pure processing local nếu khả thi; không persist input mặc định; SaveAsSnippet explicit và secret warning.
- **FX-32-BR-002:** No XML external entities/type deserialization/arbitrary SQL execution; lossy conversion warning trước apply.
- **FX-32-BR-003:** Regex input≤100KiB/timeout250ms proposal cần runtime verification; cancel hỗ trợ, không UI freeze.
- **FX-32-BR-004:** Network HTTP tester/DNS qua egress policy, private/linklocal/metadata blocked cả redirect/rebinding; credentials ephemeral/VaultRef không logs.
- **FX-32-BR-005:** Epoch units explicit, unsafe integers/loss warning; QR không auto navigate URL.

## Quyền, API và tích hợp

- ToolRegistry versioned pure execution; NetworkRequest shared egress adapter.
- Telemetry toolID/duration/code, không input/output payload.

Áp dụng [hợp đồng chung](00-shared-behavior.md): owner isolation, module/action gate, concurrency, idempotency, lỗi/loading/empty, phân trang và lifecycle. Support/Emergency chỉ read-only có grant; không thừa hưởng owner mutation, secret reveal hoặc export. API cụ thể phải theo command/query này và được chốt trong solution design.

## Tiêu chí nghiệm thu

- **FX-32-AC-001:** XXE/regex pathological không network request hoặc unbounded hang.
- **FX-32-AC-002:** JWT decode không báo verified giả.
- **FX-32-AC-003:** Lossy convert cảnh báo; privateIP redirect blocked.

Các AC nguồn và common gates vẫn bắt buộc; đây là các scenario bổ sung, không thay thế toàn bộ test specification.

## Tool capability matrix bổ sung

Các tools ghi P0/P1 trong phase nguồn phải có entry rõ; network tools vẫn Q-07, không coi là đã Approved.

| Tool group | Input/options/output contract |
|---|---|
| Cron builder | Five-field cron safe subset; User timezone/start/end; next10occurrences preview và lỗi unsupported expression. Không âm thầm dùng six-field seconds dialect |
| Markdown preview | Raw Markdown, safe preview, no scripts/arbitrary embeds; không autosave Documents |
| SQL/HTML/CSS/JavaScript/C# formatter | Language/dialect/version explicit, text in/out; deterministic whitespace formatting, không compile/execute. Unsupported syntax báo lỗi giữ input |
| Date calculator | Hai instants hoặc instant+duration; distinguish elapsed duration và calendar day/month arithmetic, DST preview |
| URL parser/header inspector | Parse scheme/authority/host/port/path/query/fragment; mask embedded credentials, không fetch khi chỉ Parse. Headers được nhập tay nếu không explicit Network Send |
| Certificate viewer | Bounded PEM/DER public certificate; subject/issuer/SAN/fingerprint/validity, no private-key persistence. Parse thành công không đồng nghĩa chain trust/hostname validation |
| CSV viewer | Header/delimiter/quote/encoding options, bounded rows, malformed-row report; formula cells render text không execute |
| Favorites/history | Favorite tool IDs không payload. History opt-in, default off, explicit sensitive-input warning/clear. Secret-like input không persistent history |

Formatting/validation test fixtures phải pin library/dialect behavior trong solution design; không claim round-trip semantic nếu converter đã cảnh báo lossy output.

## Traceability và phần còn mở

- [phase-06-developer-and-automation.md](../requirements/phases/phase-06-developer-and-automation.md): `P06-DAT-001`, `P06-DAT-002`, `P06-DAT-003`, `P06-DAT-004`, `P06-DEV-001`, `P06-DEV-002`, `P06-DEV-003`, `P06-DEV-004`, `P06-ENC-001`, `P06-ENC-002`, `P06-NET-001`, `P06-NET-002`, `P06-NET-003`, `P06-NET-004`, `P06-NET-005`, `P06-SEC-001`, `P06-SEC-002`, `P06-SEC-003`, `P06-SEC-004`, `P06-SEC-005`, `P06-TBX-001`, `P06-TBX-002`, `P06-TBX-003`, `P06-TBX-004`, `P06-TBX-005`, `P06-TBX-006`, `P06-TBX-007`, `P06-TBX-008`, `P06-TME-001`, `P06-TME-002`, `P06-TME-003`

Quyết định lớn cần PO: [Q-07](90-open-decisions.md#q-07). Các hành vi phụ thuộc chúng chưa đạt Definition of Ready.

