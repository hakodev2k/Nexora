# Quyết định Product Owner — 2026-09-07, sau Action Catalog v1

Baseline GitHub: 65d32801ba75f9649a239c1888ed957bf2feb6dd. **Approved chỉ áp cho lời quyết định trực tiếp của PO; diễn giải kỹ thuật ghi riêng. Docs-only, chưa có approval implement.** Tài liệu này có ưu tiên hơn phần proposal/câu chữ xung đột tại baseline trước. Các yêu cầu không liên quan giữ nguyên.

## Biên bản quyết định và phần còn mở

| ID | Chủ đề / trạng thái | Đã xác nhận | Giới hạn và follow-up |
| --- | --- | --- | --- |
| DEC-20260907-Q01 | Q-01: Partially resolved | Account delete = soft delete; không purge tài khoản/dữ liệu do thao tác này. | Khôi phục account đã đánh dấu xóa và tái sử dụng email chưa được PO quyết định; không auto-reactivate hoặc tạo owner mới bằng reset email. |
| DEC-20260907-Q02 | Q-02: Partially resolved | Google Authenticator TOTP đã được PO xác nhận ngày 2026-09-08; tính năng bật/tắt được, khi không bật thì recovery mặc định qua email. | Mất MFA khi đã bật chưa có recovery policy được duyệt; email reset không tự gỡ MFA. Google OAuth không thuộc quyết định này. |
| DEC-20260907-Q03 | Q-03: Partially resolved | Tắt sharing hoặc xóa nguồn: link cũ bị loại bỏ vĩnh viễn, không tồn tại với người truy cập. | Các field nhạy cảm của Finance/Assets/Career/Learning được chia sẻ vẫn cần policy riêng. Không tự mở toàn payload. |
| DEC-20260907-Q04 | Q-04: Partially resolved | Vault có thể phục hồi bằng quyền SuperAdmin; các value xóa mềm, không purge. | Đã chọn hướng server-recoverable bằng technical ADR; quyền đọc safe metadata trong Support và portable encrypted backup cho owner chưa được duyệt. Không cấp SuperAdmin quyền xem plaintext thường trực. |
| DEC-20260907-Q05 | Q-05: Partially resolved | Người dùng tự nhập danh mục và số tiền; baseline được thiết kế thành manual money records. | Chưa chốt ledger/budget/debt/interest/FX/financial deletion semantics. Không tự coi các tính năng nâng cao đã duyệt hoặc bị hủy. Currency của số tiền cần explicit; UI language không quyết định tiền tệ. |
| DEC-20260907-Q06 | Q-06: Paused by Product Owner | Price Tracking tạm dừng, chưa triển khai. | Giữ catalog/schema đề xuất để tiếp tục sau; không fetch, refresh, alert hoặc bật mặc định cho User. Resume cần PO cho phép. |
| DEC-20260907-Q07 | Q-07: Paused by Product Owner | Automation/Integrations tạm dừng, chưa triển khai. | Không chạy workflow, webhook/n8n, provider test hoặc tự triển khai connector. Core jobs/reminders/email/push của chức năng đã chốt vẫn là nền tảng riêng. |
| DEC-20260907-Q08 | Q-08: Open — capacity target undefined | Mục tiêu phục vụ càng nhiều người càng tốt; kỹ thuật được ủy quyền chọn thiết kế ổn định/mở rộng. | Chưa có workload, concurrency, storage/email budget, RPO/RTO/SLA cam kết. Benchmark profiles là giả thuyết kỹ thuật, không giới hạn người dùng hoặc capacity guarantee. |
| DEC-20260907-Q09 | Q-09: Partially resolved | UI mặc định tiếng Việt; User chuyển sang tiếng Anh trong Settings. | Currency mặc định chưa được chỉ định. Múi giờ vẫn browser-detected và User đổi được; không suy Asia/Ho_Chi_Minh hoặc VND từ ngôn ngữ. |
| DEC-20260907-Q10 | Q-10: Open — unanswered | Giữ các Task/Project/Reminder rules đã chốt. | PO chưa trả lời subtask/recurrence/snooze/reminder độc lập/Task attachments. Không tự đưa các proposal này vào scope. |
| DEC-20260907-Q11 | Q-11: Resolved scope; delegated fidelity contract | Document/Resume formats cơ bản DOCX và Markdown (.md). | Supported subset + preview/loss report được đặc tả kỹ thuật; không hứa Word round-trip hoàn hảo. PDF/HTML product export không thuộc baseline mới; ICS Calendar và định dạng import riêng module khác giữ scope riêng. |
| DEC-20260907-Q12 | Q-12: Resolved workflow; delegated ownership | Chỉ tạo/liên kết Event trong Nexora và thông báo. | Calendar sở hữu Personal Event; Career chỉ reference. Không phỏng vấn trực tuyến, conference URL, provider invite hoặc external interview workflow. |

## Lời PO làm nguồn

- Q-01: “Xóa này chỉ đánh dấu là đã xóa thôi chứ khồng xóa hẳn”.
- Q-02: “Có tính năng xác thực qua google authentication (cái này có thể bật tắt được), còn không sẽ default recover qua email”.
- DEC-20260908-Q02-TOTP — Approved clarification: “Google Authenticator tạo mã OTP nhé”. Xác nhận phương thức TOTP, đóng câu hỏi thuật ngữ; chưa quyết định recovery khi mất thiết bị MFA.
- Q-03: “Sau khi tắt sharing hoặc xóa docs đi thì các link được share sẽ bị xóa không tồn tại”.
- Q-04: “Mất có thể khôi phục nhưng phải dùng quyền super admin, toàn bộ value chỉ đánh dấu là isdeleted = true thôi”.
- Q-05: “để người dùng nhập danh mục và giá tiền”.
- Q-06/Q-07: “tạm thời pause lại chưa triển khai ngay”.
- Q-08: “Đáp ứng được nhiều người càng tốt”.
- Q-09: “Mặc định là Việt Nam nhé, có thể chuyển sang ngôn ngữ Tiếng anh ... có setting để chuyển”. Được hiểu ngữ cảnh Q-09 là ngôn ngữ UI tiếng Việt, không quốc tịch/timezone/currency.
- Q-11: “Cơ bản là docx, md thôi”.
- Q-12: “chỉ tạo event/ liên kết event rồi thông báo thôi chứ không có phỏng vấn hay link ra bên ngoài”.
- DEC-20260907-INTERNAL: “website này sẽ không link ra bên ngoài ... người dùng chỉ cần dùng website quản lý mọi thứ”.
- DEC-20260907-TECH: “phần kỹ thuật bạn có thử tự quyết định dựa trên docs đảm bảo mọi thứ ổn định, không lỗi”.

## Quy tắc lan truyền bắt buộc

1. **Account**: delete đặt IsDeleted=true/DeletedAt, vô hiệu đăng nhập/session/reset-to-login, jobs và quyền truy cập dữ liệu của account; giữ PersonalSpace, records, files và key material. Không chạy purge grace7ngày từ proposal cũ. Xóa mềm không có nghĩa tài khoản tiếp tục dùng được.
2. **Vault**: mọi giá trị/history được giữ encrypted. Không có owner/admin purge hoặc crypto-erasure từ nút Delete. Khôi phục là workflow đặc quyền SuperAdmin, tách khỏi Support/Emergency read-only. SuperAdmin phục hồi cho owner, không nhận secret trong UI/response. Chi tiết [security ADR](../architecture/07-owner-decisions-security-and-recovery.md).
3. **Các module khác**: Q-01/Q-04 không phải chỉ thị xóa mềm cho toàn bộ database. Project/Task/Documents Trash và manual permanent delete trước đây vẫn áp dụng; không sửa retention toàn hệ thống bằng suy diễn.
4. **Links**: explicit sharing off hoặc source delete làm URL cũ trả unavailable404, bỏ khỏi active links, không revive khi bật lại/restore. Tombstone/audit tối thiểu nội bộ có thể giữ để chống replay. Source Archived khác Delete; Document Published→Draft vẫn tạm khóa như rule đã duyệt, Archived vẫn read-only cho link chưa bị xóa.
5. **Scope paused**: FX-30/34/35 không thuộc phần được phép triển khai hiện tại, không bật bởi registration-default all-modules hoặc Admin grant. Không đánh dấu Cancelled/Done; resume cần PO. Pause không mặc nhiên chuyển module sang Release2 hoặc công nhận Release1 hoàn thành thiếu module đã cam kết; điều kiện hoàn thành release cần PO rescope hoặc resume rồi hoàn tất. Core scheduler và notifications không bị pause theo Automation module.
6. **Internal-first**: main user journeys, editor, appointment, data management đều trong Nexora. Không nút Open external, embedded external page, OAuth redirect hoặc provider write được tự thêm. Các URL nhập tay có thể giữ như text metadata; không auto-follow/fetch. Backend fetch cho News/GitHub/monitoring cần làm rõ ranh giới với Q-07 paused, tạm không kích hoạt phụ thuộc outbound chưa duyệt.
7. **Notification transports**: email/Browser Push đã chốt không bị âm thầm xóa bởi câu “không link ra ngoài”. Đây là hạ tầng phân phối thông báo, không luồng chuyển người dùng sang ứng dụng khác. Chi phí/provider và email khi mất MFA vẫn có gate riêng.
8. **Authority**: chọn boundaries, indexes, cryptography pattern, validation, retry và UX chi tiết thuộc technical delegation; phạm vi dữ liệu công khai, ngân sách, phục hồi mất danh tính và SLA vẫn cần quyết định có căn cứ. Không cam kết zero bugs/unlimited scale khi chưa có runtime tests. Không scaffold/code/migrate/deploy trong lượt docs này.

## Acceptance thay đổi

- POAC-001: Account IsDeleted=true → old sessions, share URLs, reset-token login và background user effects không còn truy cập được; DB vẫn giữ dữ liệu.
- POAC-002: Tắt sharing rồi bật lại/restore Document → token cũ vẫn404; tạo link mới dùng token mới.
- POAC-003: Xóa Folder/parent page invalidates links của mọi page thuộc deletion cohort; không background window cho resolve link cũ.
- POAC-004: Vault delete giữ ciphertext/history/keys; owner không purge hoặc tự restore bị xóa. Recovery SuperAdmin không trả plaintext cho operator.
- POAC-005: Paused module bị loại khỏi navigation/quick-create/default enablement/automation actions; direct command không thực thi dù có Allow.
- POAC-006: Switch vi↔en thay UI/messages/labels trên mọi module, không translate user content hoặc đổi currency/instant.
- POAC-007: DOCX/MD preview báo format không hỗ trợ; parser không fetch external links; không báo success nếu mất nội dung không được xác nhận.
- POAC-008: Career Create/Link Event chỉ gọi internal Calendar contract, một Event/một reminder source; không conference/interview execution.

[Decision status](../features/90-open-decisions.md) · [Database delta](../design-database/17-owner-decision-delta.md) · [Action changes](../action-catalog/08-owner-decision-changes.md)
