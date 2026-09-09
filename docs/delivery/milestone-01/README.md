# M01 — Personal identity and access foundation

**Status: specification for review; implementation approval NOT granted.** M01 là milestone nội bộ, một phần Phase1; không phải public launch hoặc hoàn thành Phase1/R1.

Outcome: từ clean local candidate, operator bootstrap an toàn; User đăng ký/xác minh → đăng nhập → profile vi/en/timezone; SuperAdmin xem metadata, đổi role/module/action grants có preview, concurrency và audit; phiên cũ bị thu hồi đúng policy.

1. [Stories và scope từng story](01-stories.md)
2. [API contract và examples](02-api-contracts.md) · [OpenAPI design](openapi.json)
3. [DB và transaction mapping](03-data-and-transactions.md)
4. [UX và acceptance](04-ux-and-acceptance.md)
5. [Environment/runbook specification](05-environment-and-runbook.md)
6. [Readiness và evidence gates](06-readiness-and-evidence.md)

M01 chỉ chứa password/email flow. Google Authenticator đã approved về method nhưng enrollment/recovery chưa ship trong M01; không silently bypass nếu dữ liệu nhập/migrate có MFA enabled. Account soft-delete thiết kế đã chốt nhưng self-delete UI/restore thuộc milestone kế tiếp, không nói R1 không có chúng. Avatar upload, change-email/password khi đang đăng nhập, Files, Sharing, Support/Emergency, Vault, business modules, Notification Center full UI và outbound ingestion không thuộc M01. Notification intents/delivery foundation phục vụ security events vẫn có trong M01.

API shapes trong gói là **Resolved delegated technical contract**, không phải endpoints đang tồn tại. Chỉ các story có contract đầy đủ trong gói được cân nhắc code sau PO approval. Bất cứ thêm action/body field nào ngoài scope phải cập nhật story/contract trước.
