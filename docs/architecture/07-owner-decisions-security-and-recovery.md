# Security / recovery ADR sau quyết định PO

Docs-only · Source [DEC-20260907](../requirements/10-owner-decisions-20260907.md). Không lựa chọn dịch vụ trả phí hoặc chạy crypto/schema trong lượt này.

## ADR-PO-01 — Account soft delete

Technical resolution: identity.User IsDeleted bit not-null default0, DeletedAt UTC nullable và DeletedByUserId nullable actor FK. Khi xóa, State=Deleted đồng thời IsDeleted=true, rotate SecurityStamp, revoke sessions/one-time login/reset proofs, support consents/sessions và invalidate all owner share links. PersonalSpace/domain rows giữ owner/key/history. Every API/worker/share resolver kiểm tra User.IsDeleted, không chỉ IsActive cache.

Deletion command idempotent, recent-auth5min, explicit retention disclosure và protect last active SuperAdmin. Auth recovery email không tự undelete. NormalizedEmail vẫn unique kể cả deleted để không lấy lại ownership nhầm; email reuse/account restore là policy pending. Security cleanup của expired tokens không là purge retained business/Vault values.

## ADR-PO-02 — Optional Google authentication

Approved: bật/tắt được, email recovery mặc định khi không bật. **Working interpretation / Proposed**: TOTP tương thích Google Authenticator. [Google help](https://support.google.com/accounts/answer/1066447?co=GENIE.Platform%3DAndroid&hl=en) mô tả mã xác minh dùng ứng dụng Authenticator, có thể tạo offline; tham khảo2026-09-07, không kiểm thử tài khoản. Không dùng nghiên cứu này thay xác nhận thuật ngữ của PO.

Nếu xác nhận TOTP: pending enrollment secret encrypted, show QR/setup key chỉ trong authenticated setup memory, bật sau code proof, one-use time-step replay defense và rate limit. Disable yêu cầu current password + TOTP nếu đã enabled; email reset password không gỡ MFA. Mất MFA là policy riêng chưa tự hạ về email-only. Không mandatory MFA cho Admin/SuperAdmin từ proposal cũ; optional scope giữ đúng PO, nhưng recent-auth vẫn bắt buộc hành động đặc quyền. TOTP storage/implementation remain gated until interpretation confirmed.

## ADR-PO-03 — Permanent share invalidation

Technical resolution: ShareLink.IsDeleted + InvalidatedAt/reason + issued policy epoch; tombstone/hash only, không live URL object sau revoke/delete. Đổi SharingEnabled=false tăng SharingEpoch atomically; disable generation bất biến để enable lại không hồi sinh token cũ. Resolver checks current owner active, matching issued epoch, explicit tombstone, source lifecycle and audience. Large module-wide invalidation dùng epoch synchronous deny trước asynchronous row cleanup. No provider/queue delay leaks old links.

Document delete/Folder cohort delete marks source dead and invalidates its links in same transaction boundary; existing issued token is irrecoverable after restore. Keep a revocation/tombstone barrier against backup restore replay: recovery cannot roll back revoked token authority. Safe response404 contains no title/content/owner/reason. Archived and Draft transitions preserve their previously approved distinctions.

## ADR-PO-04 — Recoverable Vault with SuperAdmin authorization

Approved business: recoverable, SuperAdmin required, deleted values retained. Technical resolution: server-recoverable envelope encryption. Each owner data key has normal wrap and separate recovery wrap; wrapping keys live in protected key-provider boundary outside SQL payload tables. Ciphertext/history and needed wraps survive soft deletion. No zero-knowledge claim; platform recovery service has a controlled cryptographic capability, not an unrestricted SuperAdmin reveal UI.

Use vetted authenticated encryption (AES-256-GCM design, unique96-bit nonce per key,128-bit tag) with owner/item/version/schema bound as authenticated context. Keys never beside data in plaintext; key versions immutable; root recovery material backed up separately with restore verification. Exact key-provider deployment follows approved environment/budget, no new third-party user integration. Key/nonce ownership and rotation must be tested against the selected maintained library before production.

[OWASP storage](https://cheatsheetseries.owasp.org/cheatsheets/Cryptographic_Storage_Cheat_Sheet.html) and [key management](https://cheatsheetseries.owasp.org/cheatsheets/Key_Management_Cheat_Sheet.html), consulted2026-09-07: separation of keys, authenticated storage and documented recovery/lifecycle inform this technical choice. Nexora's recoverability/privacy scope comes from PO, not those references.

Recovery flow: owner authenticated request with kind DeletedItem/HistoricalVersion/KeyAccess → evidence of owner identity → SuperAdmin reviews minimal IDs/reason → fresh privileged auth + explicit authorization → durable audit and3-channel security intents → recovery service validates current owner/request/revision/envelopes → restore marker or create new version/rewrap to owner key access → completion audit + owner notification. Operator receives status only; never ciphertext download, plaintext, key or secret preview. Ordinary Admin, Support and Emergency do not acquire recovery mutation rights. A new RECOVERY operating context is purpose-bound to this request, not impersonation.

Unavailable account identity proof, missing/corrupt ciphertext or loss of every wrapping/root key blocks recovery; SuperAdmin permission cannot recreate destroyed cryptographic material. Recovery promises depend on backups retaining at least one usable path. Rollback/failure does not clear IsDeleted or claim success. Restore item and restore historical value require SuperAdmin; historical ciphertext remains append-only. Folder/container changes cannot bypass retained-item recovery rules. No Vault purge/crypto-erasure action in current product.

## ADR-PO-05 — Internal navigation and platform delivery

User UI handles owned data inside Nexora. No external-link button, iframe, external image auto-fetch, OAuth connect or conference join. Imported URLs remain inert text where needed. Internal resource links continue to enforce source access. Email/Push transport and operational backup/key infrastructure are foundational contracts, not the paused user Automation/Integrations product. Backend content ingestion/monitor probes are held unavailable until internal-first scope is clarified; never label News/GitHub live if data is not fetched.
