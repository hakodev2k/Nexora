# State và Action Matrices

Tài liệu bổ sung cho FX-11/12/13/20. “Cho phép” luôn cần owner/action/module/current-revision gates. Bảng là behavior contract, không bằng chứng đã implement.

## Project transitions

| Từ / Đến | NotStarted | InProgress | Completed | Skipped |
|---|---|---|---|---|
| NotStarted | No-op idempotent | Cho phép | Confirm; có Task chưa kết thúc thì cần lý do | Confirm terminal |
| InProgress | Cho phép + lý do (delegated) | No-op | Confirm; có Task chưa kết thúc thì cần lý do | Confirm terminal |
| Completed | Chặn | Chặn | No-op | Chặn |
| Skipped | Chặn | Chặn | Chặn | No-op |

Project terminal khóa cả field edits/Task mutation/create/restore. Complete/Skip không tự chuyển status Task. Xóa vào Trash không phải reopen. Restore Project terminal vẫn terminal.

## Task transitions khi Project hoạt động

| Từ / Đến | NotStarted | InProgress | Completed | Skipped |
|---|---|---|---|---|
| NotStarted | No-op | Cho phép | Cho phép | Cho phép |
| InProgress | Lý do bắt buộc | No-op | Cho phép | Cho phép |
| Completed | Lý do bắt buộc | Lý do bắt buộc | No-op | Chặn direct; quay lại InProgress có lý do |
| Skipped | Lý do bắt buộc | Lý do bắt buộc | Chặn direct; quay lại InProgress có lý do | No-op |

Chuyển trực tiếp Completed↔Skipped là delegated restriction để “chuyển ngược” không bị bypass. Metadata của Completed/Skipped Task vẫn sửa bình thường khi Project hoạt động. Nếu Project terminal, mọi mutation trong bảng bị chặn kể cả restore old version. No-op không tạo history version thay đổi giả; explicit Save riêng vẫn theo feature history policy.

## Calendar Event actions

| State/source | Edit form | Complete | Cancel/Delete | Reopen | Share |
|---|---|---|---|---|---|
| Manual Scheduled | Có | Có | Có, thành Canceled | Không áp dụng | Không |
| Manual Completed | Không | No-op | Không | Không | Không |
| Manual Canceled | Không | Không | No-op | Không | Không |
| Task projection | Không; mở Task nguồn | Qua Task | Qua Task | Qua Task rules | Calendar không share |

Personal Event qua End vẫn Scheduled nếu User chưa đổi; không overdue. Canceled luôn còn trên Calendar với kiểu gạch ngang. Task projection terminal giữ Task status; Task Trash ẩn projection.

## Documents state/action rules

| Action | Draft | Published | Archived | Trash |
|---|---|---|---|---|
| Edit/Save Title/body/Tag/visual | Có | Có | Không | Không |
| Đổi type/editor/folder/parent | Không | Không | Không | Không |
| Publish | Có | No-op | Không | Không |
| Return to Draft | No-op | Có; suspend share | Không | Không |
| Archive | Có | Có | No-op | Không |
| Unarchive | Không áp dụng | Không áp dụng | Về previous Draft/Published; parent/cohort gate | Không |
| Tạo share mới | Không | Có nếu module cho phép | Không | Không |
| Link đã active | Không | Resolve nếu còn hợp lệ | Giữ nếu link vốn active; không revive | Không |
| Xóa vào Trash | Có | Có | Có | No-op |
| Restore version | Có, new version | Có, new version | Không | Không |
| Restore Trash | Không áp dụng | Không áp dụng | Không áp dụng | Parent/deletion-batch gates |
| Purge | Không trực tiếp | Không trực tiếp | Không trực tiếp | Confirm, dependency checks |

## Documents tree examples

| Ban đầu | Thao tác | Kết quả |
|---|---|---|
| Parent Draft, Child A Published, Child B Archived riêng | Archive parent rồi Unarchive parent | Parent Draft, A Published; B vẫn Archived |
| Parent Published, Child A Trash riêng | Archive/Unarchive parent | A vẫn Trash; không được phục hồi ngầm |
| Parent Archived, Child A Trash | Restore A riêng | Chặn; Unarchive parent trước |
| Parent Trash, Child A trong cùng deletion batch | Restore parent | Khôi phục cả batch đúng prior states |
| Parent active, Child A Trash riêng | Restore A | Cho phép nếu parent ngoài Archived/Trash; A giữ state lúc xóa |
| Parent đã purge | Restore child riêng | Chặn vĩnh viễn, không promote thành root |

## Cross-module authority

| Dữ liệu | Nguồn có quyền ghi | Consumer | Điều bị cấm |
|---|---|---|---|
| Task time/status | Tasks trong active Project | Calendar/Search/Dashboard/Reminder | Calendar sửa projection trực tiếp |
| Document current version | Explicit Save/RestoreVersion command | Search/Share/File refs | Autosave hoặc share history ngầm |
| Money balances | Finance ledger transaction contract | Dashboard/Assets/Shopping reports | Asset/Order sửa balance |
| Secret values | Vault protected actions | Authorized secret-use adapter | Search/Share/Email/Admin đọc plaintext |
| User consent | User grant/revoke | Support access evaluator | Admin tự grant consent |
| Notification read state | Inbox commands | UI counters | Xóa Inbox xóa security audit |
| External observations | Approved provider | Price/Monitoring/Assets | Error thành giá0 hoặc manual field bị overwrite |
| Interview Calendar link | Chờ Q-12 | Calendar/Interviews | Tự thêm third source khi chưa duyệt |

## Acceptance mapping

Mỗi ô chặn phải có negative test qua direct API, không chỉ UI. Mỗi transition cho phép cần happy path + stale revision + replay + parent state đổi giữa preview/commit. Matrix kết hợp source AC và FX-COM-AC-001…006; không tạo một ticket cho mỗi ô khi có thể nhóm thành story cùng nghiệp vụ.

