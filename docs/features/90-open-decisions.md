# Các quyết định lớn còn cần chốt

Đây là **12 nhóm quyết định**, không phải 12 câu hỏi phải trả lời ngay hay danh sách đầy đủ mọi ADR kỹ thuật. Các phương án dưới đây đã được chuẩn bị để PO duyệt theo nhóm. Không hỏi lại thao tác nhỏ đã delegated. Trạng thái tất cả Q: **Open / proposal**, không là câu trả lời của User.

<a id="q-01"></a>
## Q-01 — Account deletion và portability

**Cần chốt:** xóa tài khoản có thời gian chờ/khôi phục không, phạm vi export toàn tài khoản, disclosure dữ liệu còn trong backup/audit. **Đề xuất:** request deletion có recent-auth, chờ 7ngày owner hủy được; sau đó purge active data theo dependency, audit tối thiểu tách payload; export per-module approved formats. Không áp 7ngày cho Trash riêng của Project/Task/Documents. **Tác động:** irreversible loss, privacy và storage; FX-01/08/10. Backup retention phụ thuộc Q-08.

<a id="q-02"></a>
## Q-02 — Account security, MFA và recovery

**Cần chốt:** bắt buộc MFA cho ai, lost-device/account recovery, recent-auth cho hành động nhạy cảm. **Đề xuất:** MFA bắt buộc SuperAdmin/Admin, optional User; recent-auth trước security changes/Vault; recovery codes owner quản lý. Passkeys/social login không tự thêm scope. **Tác động:** onboarding/support/security; FX-01/05/28. Password hash/session/cookie/CSRF implementation do security ADR thiết kế, không hỏi PO chọn thuật toán.

<a id="q-03"></a>
## Q-03 — Share link và dữ liệu nhạy cảm

**Cần chốt:** tắt sharing policy có chặn link đã tạo không; restore Trash có tự phục hồi link không; Finance/Assets/Career field projections. **Đề xuất:** tắt sharing chặn resolve link ngay và không tự hồi sinh sau Trash restore; owner chủ động enable lại link còn hợp lệ. Sensitive defaults chỉ safe metadata, preview trước share. Giữ Project toàn bộ Task details, không per-task hide; Documents lifecycle đã Approved không thay. **Tác động:** disclosure/revocation; FX-04/27/31/37/38/39/40.

<a id="q-04"></a>
## Q-04 — Vault recovery, portability và access

**Cần chốt:** mất khóa có thể khôi phục bằng cách nào; operator có khả năng decrypt hay owner-held recovery; encrypted export/import; Support/Emergency có được đọc safe metadata. **Đề xuất:** không mở reveal/copy/export cho Admin hoặc Emergency; không public share; thiết kế owner recovery và encrypted portability riêng trước khi chọn key architecture. Chưa khẳng định zero-knowledge hoặc recoverable khi chưa thiết kế. **Tác động:** dữ liệu secret, khả năng phục hồi và kiến trúc không thể retrofit nhẹ; FX-05/10/28.

<a id="q-05"></a>
## Q-05 — Finance semantics

**Cần chốt:** envelope budgeting hay spending-limit tracking; đa tiền tệ/FX; debt direction/interest; corrections/delete/restore financial history. **Đề xuất:** spending limits theo tháng/category/currency, no rollover mặc định; reports tách currency; transfer FX nhập amount hai vế thủ công; interest adjustment nhập tay; posted corrections có journal/history, không sửa balance trực tiếp. Savings progress từ selected accounts hoặc manual phải chọn mode rõ. **Tác động:** các con số người dùng dựa vào; FX-27. Toàn bộ công thức và deletion policy tài chính trong spec là proposal gắn Q này, không mặc nhiên delegated approved.

<a id="q-06"></a>
## Q-06 — Shopee provider và price contract

**Cần chốt:** nguồn được phép/khả dụng, budget refresh, thị trường/variant/currency và định nghĩa giá. **Đề xuất:** một approved provider cho Shopee, item price công khai đúng variant không voucher/shipping/member pricing; poll6h, manual refresh có rate limit; feature báo stale/unknown. Adapter marketplace khác trong phase cũ là conditional extension, đề xuất chưa cam kết ngoài Shopee trước khi PO chọn. Không bypass CAPTCHA/login hoặc gọi manual-only fallback là tracking hoàn chỉnh. **Tác động:** module có thể chưa khả thi nếu thiếu nguồn; FX-30/31.

<a id="q-07"></a>
## Q-07 — Automation/n8n và quyền ra mạng

**Cần chốt:** workflow depth, trusted trigger/action catalog tối thiểu, data mapping/branching; n8n inbound/outbound và dữ liệu được ra ngoài; network tools/monitor target scope. **Đề xuất:** bounded DAG≤20steps, linear+conditions, no arbitrary code/loops; manual/schedule/domain event/webhook; initial actions notification + approved owner module commands + explicit outbound projection. n8n không DB/master key, core độc lập n8n. Network chỉ public approved targets qua guard. **Tác động:** phạm vi R1, data egress/SSRF/side effects; FX-32/34/35/36/38.

<a id="q-08"></a>
## Q-08 — Capacity, retention và vận hành

**Cần chốt:** mức User đồng thời/dữ liệu/files/jobs dự kiến, storage/email/provider budget, audit/log/backup retention, RPO/RTO/availability. **Đề xuất để thảo luận:** test profile100 concurrent users, files quota1GiB/User, RPO24h/RTO8h, encrypted backups30ngày, redacted job logs30ngày; chưa cam kết production hoặc ghi thành quota đã duyệt. User-owned Trash/Notification retention đã chốt không bị đổi. **Tác động:** chi phí và production design; FX-07/08/10/34/36. Deployment provider chỉ chọn sau Local-Stable theo roadmap.

<a id="q-09"></a>
## Q-09 — Ngôn ngữ và locale

**Cần chốt:** UI languages và currency mặc định. **Đề xuất:** Vietnamese + English, browser locale fallback Vietnamese, currency mặc định VND nhưng Finance accounts explicit; IANA timezone browser detect đã Approved. **Tác động:** translation/testing và audience; FX-01/09/27. Không dùng vị trí User hiện tại làm quyết định PO đã xác nhận.

<a id="q-10"></a>
## Q-10 — Productivity extensions

**Cần chốt:** independent reminders, snooze, subtasks/recurrence/Task attachments nếu các proposal lịch sử còn cần R1. **Đề xuất:** giữ Task/Project flow đã chốt làm baseline; chỉ thêm extension được PO chọn với state/history/calendar contract riêng. Không nhận “mọi module hoàn chỉnh” là quyền thêm mọi tính năng TickTick. **Tác động:** workflow/data model; FX-12/14. Không thay max one Task/Event reminder, no recurring ICS import.

<a id="q-11"></a>
## Q-11 — Documents và Resume file formats

**Cần chốt:** import/export Markdown/HTML/PDF/DOCX, hỗ trợ attachments/structure và mức fidelity. **Đề xuất:** Markdown+asset manifest cho Markdown mode; safe HTML+assets cho Block mode; PDF export cho cả hai; DOCX round-trip chỉ khi PO thực sự cần và có loss-report acceptance. Resume upload file là core riêng, không phụ thuộc có DOCX editor/export. **Tác động:** đáng kể tới editor/schema/conversion scope; FX-10/20/39. Không thay manual Save/type/editor immutable hoặc Project/Task no import/export.

<a id="q-12"></a>
## Q-12 — Interview và Calendar ownership

**Xung đột cần chốt:** Phase7 yêu cầu Calendar link/reschedule/cancel cho Interview, còn Calendar đã chốt Manual Event + Task Event. **Đề xuất:** owner chủ động tạo linked Personal Event từ Interview, Calendar tiếp tục sở hữu event; interview reschedule phải preview/confirm cập nhật cả hai qua contract và giữ terminal-event rule. Phương án source Interview riêng sẽ mở rộng Calendar/ICS/search và cần PO duyệt. **Tác động:** một nguồn sự thật, tránh hai reminders/schedules; FX-13/39. Chưa implement tự động source thứ ba.

## Cách đóng quyết định

PM gửi một nhóm lớn với phương án và tradeoff; PO trả lời; cập nhật Q status + source DEC-ID + affected feature rules/AC + roadmap. Technical ADR không được biến một proposal scope/privacy/cost thành Approved thay PO. Những phần không phụ thuộc Q vẫn có thể được review/tách backlog; code chỉ sau approval riêng.

