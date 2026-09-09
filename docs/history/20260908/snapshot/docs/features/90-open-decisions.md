# Decision status — cập nhật theo Product Owner 2026-09-07

[Biên bản lời PO và quy tắc ưu tiên](../requirements/10-owner-decisions-20260907.md). Các proposal trước đây không tự trở thành Approved. Không còn ghi cả12 nhóm là Open như baseline cũ; mỗi nhóm có trạng thái riêng bên dưới. Chưa có approval implement.

<a id="q-01"></a>
## Q-01 — Partially resolved

**Đã xác nhận:** Account delete = soft delete; không purge tài khoản/dữ liệu do thao tác này.

**Còn lại / giới hạn:** Khôi phục account đã đánh dấu xóa và tái sử dụng email chưa được PO quyết định; không auto-reactivate hoặc tạo owner mới bằng reset email.

Nguồn: DEC-20260907-Q01.

<a id="q-02"></a>
## Q-02 — Partially resolved

**Đã xác nhận:** Google Authenticator TOTP đã được PO xác nhận ngày 2026-09-08; tính năng bật/tắt được, khi không bật thì recovery mặc định qua email.

**Còn lại / giới hạn:** Mất MFA khi đã bật chưa có recovery policy được duyệt; email reset không tự gỡ MFA. Google OAuth không thuộc quyết định này.

Nguồn: DEC-20260907-Q02.

<a id="q-03"></a>
## Q-03 — Partially resolved

**Đã xác nhận:** Tắt sharing hoặc xóa nguồn: link cũ bị loại bỏ vĩnh viễn, không tồn tại với người truy cập.

**Còn lại / giới hạn:** Các field nhạy cảm của Finance/Assets/Career/Learning được chia sẻ vẫn cần policy riêng. Không tự mở toàn payload.

Nguồn: DEC-20260907-Q03.

<a id="q-04"></a>
## Q-04 — Partially resolved

**Đã xác nhận:** Vault có thể phục hồi bằng quyền SuperAdmin; các value xóa mềm, không purge.

**Còn lại / giới hạn:** Đã chọn hướng server-recoverable bằng technical ADR; quyền đọc safe metadata trong Support và portable encrypted backup cho owner chưa được duyệt. Không cấp SuperAdmin quyền xem plaintext thường trực.

Nguồn: DEC-20260907-Q04.

<a id="q-05"></a>
## Q-05 — Partially resolved

**Đã xác nhận:** Người dùng tự nhập danh mục và số tiền; baseline được thiết kế thành manual money records.

**Còn lại / giới hạn:** Chưa chốt ledger/budget/debt/interest/FX/financial deletion semantics. Không tự coi các tính năng nâng cao đã duyệt hoặc bị hủy. Currency của số tiền cần explicit; UI language không quyết định tiền tệ.

Nguồn: DEC-20260907-Q05.

<a id="q-06"></a>
## Q-06 — Paused by Product Owner

**Đã xác nhận:** Price Tracking tạm dừng, chưa triển khai.

**Còn lại / giới hạn:** Giữ catalog/schema đề xuất để tiếp tục sau; không fetch, refresh, alert hoặc bật mặc định cho User. Resume cần PO cho phép.

Nguồn: DEC-20260907-Q06.

<a id="q-07"></a>
## Q-07 — Paused by Product Owner

**Đã xác nhận:** Automation/Integrations tạm dừng, chưa triển khai.

**Còn lại / giới hạn:** Không chạy workflow, webhook/n8n, provider test hoặc tự triển khai connector. Core jobs/reminders/email/push của chức năng đã chốt vẫn là nền tảng riêng.

Nguồn: DEC-20260907-Q07.

<a id="q-08"></a>
## Q-08 — Open — capacity target undefined

**Đã xác nhận:** Mục tiêu phục vụ càng nhiều người càng tốt; kỹ thuật được ủy quyền chọn thiết kế ổn định/mở rộng.

**Còn lại / giới hạn:** Chưa có workload, concurrency, storage/email budget, RPO/RTO/SLA cam kết. Benchmark profiles là giả thuyết kỹ thuật, không giới hạn người dùng hoặc capacity guarantee.

Nguồn: DEC-20260907-Q08.

<a id="q-09"></a>
## Q-09 — Partially resolved

**Đã xác nhận:** UI mặc định tiếng Việt; User chuyển sang tiếng Anh trong Settings.

**Còn lại / giới hạn:** Currency mặc định chưa được chỉ định. Múi giờ vẫn browser-detected và User đổi được; không suy Asia/Ho_Chi_Minh hoặc VND từ ngôn ngữ.

Nguồn: DEC-20260907-Q09.

<a id="q-10"></a>
## Q-10 — Open — unanswered

**Đã xác nhận:** Giữ các Task/Project/Reminder rules đã chốt.

**Còn lại / giới hạn:** PO chưa trả lời subtask/recurrence/snooze/reminder độc lập/Task attachments. Không tự đưa các proposal này vào scope.

Nguồn: DEC-20260907-Q10.

<a id="q-11"></a>
## Q-11 — Resolved scope; delegated fidelity contract

**Đã xác nhận:** Document/Resume formats cơ bản DOCX và Markdown (.md).

**Còn lại / giới hạn:** Supported subset + preview/loss report được đặc tả kỹ thuật; không hứa Word round-trip hoàn hảo. PDF/HTML product export không thuộc baseline mới; ICS Calendar và định dạng import riêng module khác giữ scope riêng.

Nguồn: DEC-20260907-Q11.

<a id="q-12"></a>
## Q-12 — Resolved workflow; delegated ownership

**Đã xác nhận:** Chỉ tạo/liên kết Event trong Nexora và thông báo.

**Còn lại / giới hạn:** Calendar sở hữu Personal Event; Career chỉ reference. Không phỏng vấn trực tuyến, conference URL, provider invite hoặc external interview workflow.

Nguồn: DEC-20260907-Q12.

## Các việc cần PO thực sự quyết định tiếp

- Chốt quy trình khi MFA đã bật nhưng mất thiết bị/proof. Phương thức Google Authenticator TOTP đã xác nhận (DEC-20260908-Q02-TOTP).
- Khôi phục account deleted/email reuse; không tự áp7ngày grace hoặc tự phục hồi qua reset.
- Sensitive share/support field scope, owner encrypted portability; recovery SuperAdmin đã xác nhận nhưng không đồng nghĩa xem plaintext.
- Currency và các Finance nghiệp vụ nâng cao; không ép chọn Income/Expense trong form nhập category/amount đơn giản.
- Quy mô/ngân sách/RPO/RTO đo được; Q-10 chưa trả lời.
- Backend ingestion của News/GitHub/monitoring có nằm trong lệnh pause integrations hay chỉ cấm đưa User ra ngoài. Mặc định chưa kích hoạt outbound chưa rõ.

Các chi tiết kỹ thuật còn lại do technical owner tự chốt thành ADR/acceptance có thể kiểm chứng. Price/Automation/Integrations chỉ quay lại khi PO yêu cầu resume; không tiếp tục hỏi thông số provider trong lúc paused.
