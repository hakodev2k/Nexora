# Quyết định đã xử lý và proposal cần Product Owner

2026-09-08 · User ủy quyền nghiên cứu/làm rõ docs. Quyền này cho phép chốt kỹ thuật thông thường; không tự coi một lựa chọn thay đổi quyền đọc, recovery hoặc cam kết R1 là lời PO đã duyệt. Không cần trả lời tất cả trước khi làm M01 sau approval.

## Resolved delegated — dùng cho M01

| ID | Quyết định | Tác động / căn cứ |
| --- | --- | --- |
| D-H01 | M01 là milestone nội bộ nền tảng, không phải R1 phát hành | Không giảm catalog R1; [scope](01-current-scope.md) |
| D-H02 | Cùng origin, cookie opaque server-side, SQL authority, CSRF; không browser bearer persistence | Tiếp nối ADR-D08, hiện thực request contract cụ thể |
| D-H03 | RFC 9457 errors; strong ETag/If-Match; operation-scoped idempotency | [API](milestone-01/02-api-contracts.md); không thay semantic lifecycle |
| D-H04 | Verify mới tạo PersonalSpace + grant snapshot trong một SQL transaction | Hợp nhất feature cũ với ADR-D09; email verification mới cho dùng |
| D-H05 | UI vi mặc định, chọn en; timezone IANA riêng; không tự suy tiền tệ | Đúng Q09; language gate đã đóng |
| D-H06 | Deleted account không reuse email/không tự phục hồi trong M01 | UQ giữ dữ liệu đúng owner; không phải quyết định cấm phục hồi vĩnh viễn |
| D-H07 | Không handler cho unknown/Paused/Blocked action; registration chỉ default installed+Ready+active scope | Hợp nhất all-modules-on với pause và module extension |
| D-H08 | Các version/toolchain dưới environment là research pins; compatibility và security recheck ở S00 | Không gọi chưa cài là tested |
| D-H09 | Basic Finance luôn nhập Currency rõ ràng; không tạo default VND từ vi | Amount cần đơn vị, không tự thêm ledger semantics |

## Các proposal lớn — chưa cấp quyền thực thi

### P-H01 — Mất Google Authenticator

Context: TOTP optional đã Approved; mất thiết bị chưa có policy. GitHub dùng recovery codes và các factor đã đăng ký; không dùng riêng email làm bằng chứng đủ thay MFA. [Nguồn](03-reference-evidence.md).

**Khuyến nghị:** cấp 10 recovery codes ngẫu nhiên dùng một lần khi bật TOTP; lưu hash, chỉ hiển thị plaintext một lần, yêu cầu xác nhận đã lưu. Dùng password + một code để vào phiên recovery hạn chế; re-enroll TOTP, vô hiệu code/seed cũ, revoke mọi session và thông báo ba kênh. Có TOTP hợp lệ thì tự gỡ được bằng password + TOTP. Regenerate codes chỉ sau recent auth + TOTP. Các con số là technical proposal cho policy này.

Alternative: cho support xác minh thủ công khi mất cả codes; tăng khả năng phục hồi nhưng thêm social-engineering risk, nhân sự và bộ bằng chứng danh tính cần duyệt. Không chọn email-only reset MFA vì email bị chiếm sẽ bypass factor. Nếu mất cả codes và thiết bị, đề xuất mặc định từ chối tự động, dữ liệu vẫn giữ; quyền SuperAdmin phục hồi Vault không là quyền reset MFA.

Affected: FX01, security UX, MfaCredential/RecoveryCode; **không chặn M01 password-only nội bộ**, chặn phát hành optional MFA. Decision cần PO: có chấp nhận recovery codes và cách xử lý mất cả codes không?

### P-H02 — Khôi phục account đã soft-delete / email reuse

Khuyến nghị: owner tự yêu cầu khôi phục qua verified email và credential/factor đã có; cùng UserId/PersonalSpace, không auto-revive share links/support consents/sessions. Sau khi khôi phục phải đăng nhập mới; nếu MFA từng bật vẫn bắt buộc factor. Giữ NormalizedEmail unique kể cả Deleted để tránh owner mới nhận dữ liệu cũ. Không purge, không time window tự đặt làm mất dữ liệu.

Alternative: SuperAdmin review recovery với bằng chứng owner, thêm chi phí vận hành; email reuse thành account mới cần quy tắc tách identity tuyệt đối. Không áp dụng trong M01. Affected FX01/02/04/08; PO cần duyệt quyền phục hồi, không chỉ thao tác UI. Không suy từ Google/Drive Trash sang account lifecycle Nexora.

### P-H03 — Sensitive share/support projections

Khuyến nghị dùng projection allowlist riêng, không serialize owner DTO. Projects/Tasks/Documents giữ nội dung đã PO cho phép. Với Finance chỉ category label/date/currency và tổng hợp do owner chọn; **amount/counterparty/memo không public mặc định**. Assets ẩn serial/receipt/contact; Career ẩn salary/contact/private notes; Learning ẩn chứng chỉ có mã xác minh/định danh. Safe support chỉ IDs/type/state/timestamps/error codes, không body/secret. Secret fields, key wraps, TOTP seeds, tokens luôn cấm.

Alternative: owner preview chọn field nhạy cảm trước từng share; linh hoạt nhưng tăng accidental exposure và test matrix. Khuyến nghị triển khai preview trước khi mở sensitive share. Affected FX04/05/27/28/37/38/39/40; chưa cho phép cả safe projection ngoài các quyền đã chốt chỉ vì proposal tồn tại. M01 admin metadata chỉ User identity/role/module state, không personal domain payload.

### P-H04 — Vault portability và operator recovery

Recoverable encryption và SuperAdmin-authorized recovery đã chốt; no plaintext operator response. Khuyến nghị owner encrypted export là một package có manifest/key-version/integrity kiểm tra, re-auth, passphrase wrapping riêng và rehearsal import. Alternative không có portable export ở đợt đầu, giảm crypto surface nhưng giảm portability. Cả hai chưa quyết định. Không cấp quyền operator đọc safe metadata vượt scope đã chốt; không coi backup SQL là owner export. Chỉ chặn Vault portability/support projection, không nền tảng M01.

### P-H05 — Finance nâng cao

Basic scope đã rõ: category + amount + explicit currency + date/note, sửa có concurrency. Khuyến nghị chưa đưa ledger/accounts/transfer/debt/interest/budget/FX vào milestone basic. Nếu cần mở rộng, chốt signed amount vs income/expense, transfer balancing, rounding, correction/void và deletion trước API. Alternative ledger ngay sẽ thêm đối soát/migration và rule test. Không tự hủy các requirement nâng cao khỏi R1. Chỉ chặn nâng cao và financial deletion chưa có rule.

### P-H06 — Task/Reminder mở rộng (Q10)

Khuyến nghị giữ flat Tasks bắt buộc Project, một Reminder, preset15min; không recurrence/subtask/snooze/standalone reminders trong milestone Productivity đầu tiên. Alternative recurrence/subtask đòi occurrence identity, parent-close cascade, DST và reminder replacement contracts. TickTick/Todoist chỉ là tham khảo, không bằng chứng Nexora đã duyệt. Chỉ phần mở rộng Blocked; Task/Project core không Blocked vì Q10.

### P-H07 — Internal navigation và ingestion

Khuyến nghị phân biệt: User không rời Nexora; backend read-only public feed/GitHub metadata/HTTP monitoring có allowlist/SSRF guard/provider limits, không OAuth/write/payment. Alternative cấm mọi outbound: News/GitHub chỉ thủ công/snapshot, live monitoring không thể hoạt động. Đây là scope/integration/cost choice, chưa bật ingestion mặc định. Email/Web Push nền tảng đã được PO yêu cầu vẫn giữ. Price/Automation/Integrations Paused tiếp tục pause dù P-H07 được duyệt sau này.

### P-H08 — R1, capacity và vận hành

R1 chỉ complete sau mọi committed capability đạt acceptance hoặc PO đổi scope rõ ràng. Khuyến nghị giữ paused items là chưa hoàn tất, tách M01/M02 internal milestones. Không tự gọi phần active là R1 hoàn chỉnh. Capacity đề xuất đo theo workload 100/500/1000 concurrent sessions, không hứa các mức đã đạt. RPO15min/RTO4h có thể dùng **target proposal cho rehearsal production**, phải tính storage/hosting/on-call budget và PO duyệt trước SLA. M01 dùng synthetic local durability/restart tests, không cần mua provider.

## Cách đóng

Mỗi proposal sau xác nhận cập nhật source quote, current feature, DB, UX, action gate và acceptance; không chỉ đổi chữ Open. Chưa có câu trả lời thì giữ fail-closed đúng phần, không khóa toàn bộ hệ thống. [Readiness theo story](milestone-01/06-readiness-and-evidence.md).
