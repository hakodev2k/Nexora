# FX-28 — Vault: actions

Catalog v1 · 2026-09-07 · Docs-only. New key decomposition = Resolved delegated; source business decisions giữ nguyên; Blocked rows không được kích hoạt bằng grant.

## Sources và phạm vi

- [Feature / validation / state graph](../../features/28-vault.md) — FX-28-BR-001, FX-28-BR-002, FX-28-BR-003, FX-28-BR-004, FX-28-BR-005, FX-28-BR-006. Các BR này áp cho feature; không gán sai một BR duy nhất cho mọi row.
- [UX specification](../../ux-ui/modules/28-vault.md); [exact screen bindings](../06-screen-bindings.md).
- [Authorization contract](../00-authorization-contract.md), [semantic field guards](../01-composition-and-field-guards.md), [SDK contract](../04-module-action-contract.md).
- Namespace `vault` là stable logical key, bind installed ModuleId trong manifest. **Owner personal-only; encrypted payload/keys/recovery Q-04; no support plaintext or share/export**.

## Catalog

“All prerequisites” ở row bao gồm explicit Requires **và** source/dynamic dependencies trong guard; danh sách Requires trống không có nghĩa bỏ owner/module/lifecycle checks. Every action denies unknown fields and unapproved semantic changes. Source state matrix luôn kiểm tra ở handler, không suy quyền từ verb hoặc tên button.

| Action key / hành vi | Kind / context | Admin checkbox? | Risk (DB mapping) | Status / gate | UI entry |
| --- | --- | --- | --- | --- | --- |
| <a id="vault-session-unlock"></a>`vault.session.unlock` — Mở khóa Vault | COMMAND / CONTROL | No | Security (Administrative) | Blocked Q-04; recent auth Q-02 | FX28-S01 |
| <a id="vault-session-lock"></a>`vault.session.lock` — Khóa Vault | COMMAND / CONTROL | No | Security (Administrative) | Blocked Q-04; recent auth Q-02 | FX28-S01, FX28-S06 |
| <a id="vault-item-read"></a>`vault.item.read` — Xem Vault item | QUERY / SELF | Yes, gated | Normal (Normal) | Blocked Q-04; recent auth Q-02 | FX28-S01, FX28-S02 |
| <a id="vault-item-create"></a>`vault.item.create` — Tạo Vault item | COMMAND / SELF | Yes, gated | Normal (Normal) | Blocked Q-04; recent auth Q-02 | FX28-S03 |
| <a id="vault-item-update"></a>`vault.item.update` — Sửa Vault item | COMMAND / SELF | Yes, gated | Normal (Normal) | Blocked Q-04; recent auth Q-02 | FX28-S03 |
| <a id="vault-item-reveal"></a>`vault.item.reveal` — Hiện secret field | COMMAND / SELF | Yes, gated | Secret (Sensitive) | Blocked Q-04; recent auth Q-02 | FX28-S02 |
| <a id="vault-item-copy"></a>`vault.item.copy` — Copy secret field | COMMAND / SELF | Yes, gated | Secret (Sensitive) | Blocked Q-04; recent auth Q-02 | FX28-S02 |
| <a id="vault-item-trash"></a>`vault.item.trash` — Đưa item vào Thùng rác | COMMAND / SELF | Yes, gated | Lifecycle (Normal) | Blocked Q-04; recent auth Q-02 | FX28-S06 |
| <a id="vault-item-restore"></a>`vault.item.restore` — Khôi phục item từ Thùng rác | COMMAND / SELF | Yes, gated | Lifecycle (Normal) | Blocked Q-04; recent auth Q-02 | FX28-S06 |
| <a id="vault-item-purge"></a>`vault.item.purge` — Xóa vĩnh viễn item | COMMAND / SELF | Yes, gated | Destructive (Administrative) | Blocked Q-04; recent auth Q-02 | FX28-S06 |
| <a id="vault-item-history"></a>`vault.item.history` — Xem lịch sử item | QUERY / SELF | Yes, gated | Sensitive (Sensitive) | Blocked Q-04; recent auth Q-02 | FX28-S04 |
| <a id="vault-item-restore-version"></a>`vault.item.restore_version` — Khôi phục version thành bản mới | COMMAND / SELF | Yes, gated | Lifecycle (Normal) | Blocked Q-04; recent auth Q-02 | FX28-S04 |
| <a id="vault-folder-read"></a>`vault.folder.read` — Xem Vault folder | QUERY / SELF | Yes, gated | Normal (Normal) | Blocked Q-04; recent auth Q-02 | FX28-S01 |
| <a id="vault-folder-create"></a>`vault.folder.create` — Tạo Vault folder | COMMAND / SELF | Yes, gated | Normal (Normal) | Blocked Q-04; recent auth Q-02 | FX28-S01 |
| <a id="vault-folder-rename"></a>`vault.folder.rename` — Đổi tên folder | COMMAND / SELF | Yes, gated | Normal (Normal) | Blocked Q-04; recent auth Q-02 | FX28-S01 |
| <a id="vault-folder-remove"></a>`vault.folder.remove` — Xóa folder container | COMMAND / SELF | Yes, gated | Normal (Normal) | Blocked Q-04; recent auth Q-02 | FX28-S01 |
| <a id="vault-tag-manage"></a>`vault.tag.manage` — Quản lý Vault tags | COMMAND / SELF | Yes, gated | Normal (Normal) | Blocked Q-04; recent auth Q-02 | FX28-S01 |
| <a id="vault-recovery-code-mark-used"></a>`vault.recovery_code.mark_used` — Đánh dấu recovery code đã dùng | COMMAND / SELF | Yes, gated | Secret (Sensitive) | Blocked Q-04; recent auth Q-02 | FX28-S02 |
| <a id="vault-reference-resolve"></a>`vault.reference.resolve` — Giải secret cho authorized consumer | WORKER / SYSTEM | No | Secret (Sensitive) | Blocked Q-04; recent auth Q-02 | Trusted worker/deployment; no user control |
| <a id="vault-generator-generate"></a>`vault.generator.generate` — Tạo password ngẫu nhiên local | LOCAL / SELF | Yes, gated | Secret (Sensitive) | Resolved delegated: action contract; source business rules unchanged | FX28-S05 |
| <a id="vault-generator-copy"></a>`vault.generator.copy` — Copy password vừa tạo | LOCAL / SELF | No | Secret (Sensitive) | Resolved delegated: action contract; source business rules unchanged | FX28-S05 |
| <a id="vault-support-read"></a>`vault.support.read` — Xem safe support projection của module | QUERY / SUPPORT | Yes, gated | Sensitive (Sensitive) | Blocked Q-04 | FX05-S03, FX05-S05 |

## Điều kiện riêng theo operation

| Action | Guard / validation / effects | Explicit dependencies bổ sung |
| --- | --- | --- |
| `vault.session.unlock` | Own vault session; unlock recent auth + key proof Q-04; lock clears rendered secrets; Owner personal-only; encrypted payload/keys/recovery Q-04; no support plaintext or share/export | Common + dynamic source/provider guards |
| `vault.session.lock` | Own vault session; unlock recent auth + key proof Q-04; lock clears rendered secrets; Owner personal-only; encrypted payload/keys/recovery Q-04; no support plaintext or share/export | Common + dynamic source/provider guards |
| `vault.item.read` | Owner personal-only; encrypted payload/keys/recovery Q-04; no support plaintext or share/export; Owner personal-only; encrypted payload/keys/recovery Q-04; no support plaintext or share/export | Common + dynamic source/provider guards |
| `vault.item.create` | Owner personal-only; encrypted payload/keys/recovery Q-04; no support plaintext or share/export; Owner personal-only; encrypted payload/keys/recovery Q-04; no support plaintext or share/export | Common + dynamic source/provider guards |
| `vault.item.update` | Owner personal-only; encrypted payload/keys/recovery Q-04; no support plaintext or share/export; Owner personal-only; encrypted payload/keys/recovery Q-04; no support plaintext or share/export | `vault.item.read` |
| `vault.item.reveal` | Own unlocked Vault; recent authentication policy Q-02/Q-04; no logs/toasts/URL/storage payload; Owner personal-only; encrypted payload/keys/recovery Q-04; no support plaintext or share/export | Common + dynamic source/provider guards |
| `vault.item.copy` | Own unlocked Vault; recent authentication policy Q-02/Q-04; no logs/toasts/URL/storage payload; Owner personal-only; encrypted payload/keys/recovery Q-04; no support plaintext or share/export | Common + dynamic source/provider guards |
| `vault.item.trash` | Owner personal-only; encrypted payload/keys/recovery Q-04; no support plaintext or share/export; preview aggregate, không purge; Owner personal-only; encrypted payload/keys/recovery Q-04; no support plaintext or share/export | Common + dynamic source/provider guards |
| `vault.item.restore` | Owner personal-only; encrypted payload/keys/recovery Q-04; no support plaintext or share/export; đúng deletion cohort, parent hợp lệ; Owner personal-only; encrypted payload/keys/recovery Q-04; no support plaintext or share/export | Common + dynamic source/provider guards |
| `vault.item.purge` | Owner personal-only; encrypted payload/keys/recovery Q-04; no support plaintext or share/export; chỉ Trash, preview pins/dependencies, xác nhận không thể hoàn tác; Owner personal-only; encrypted payload/keys/recovery Q-04; no support plaintext or share/export | Common + dynamic source/provider guards |
| `vault.item.history` | Owner-only history, cùng owner/module; không share/support; Owner personal-only; encrypted payload/keys/recovery Q-04; no support plaintext or share/export | Common + dynamic source/provider guards |
| `vault.item.restore_version` | Owner personal-only; encrypted payload/keys/recovery Q-04; no support plaintext or share/export; editable hiện tại, giữ immutable identity/topology, tái kiểm tra quyền trên field diff; Owner personal-only; encrypted payload/keys/recovery Q-04; no support plaintext or share/export | Common + dynamic source/provider guards |
| `vault.folder.read` | Owner personal-only; encrypted payload/keys/recovery Q-04; no support plaintext or share/export; Owner personal-only; encrypted payload/keys/recovery Q-04; no support plaintext or share/export | Common + dynamic source/provider guards |
| `vault.folder.create` | Own unlocked Vault; removal requires Unfiled/replacement strategy; không purge items; Owner personal-only; encrypted payload/keys/recovery Q-04; no support plaintext or share/export | Common + dynamic source/provider guards |
| `vault.folder.rename` | Own unlocked Vault; removal requires Unfiled/replacement strategy; không purge items; Owner personal-only; encrypted payload/keys/recovery Q-04; no support plaintext or share/export | Common + dynamic source/provider guards |
| `vault.folder.remove` | Own unlocked Vault; removal requires Unfiled/replacement strategy; không purge items; Owner personal-only; encrypted payload/keys/recovery Q-04; no support plaintext or share/export | Common + dynamic source/provider guards |
| `vault.tag.manage` | Vault namespace only; encrypted metadata policy Q-04; Owner personal-only; encrypted payload/keys/recovery Q-04; no support plaintext or share/export | Common + dynamic source/provider guards |
| `vault.recovery_code.mark_used` | Own unlocked item, audited history; không tự thực hiện recovery bên ngoài; Owner personal-only; encrypted payload/keys/recovery Q-04; no support plaintext or share/export | Common + dynamic source/provider guards |
| `vault.reference.resolve` | Trusted purpose-scoped invocation; owner/module/action + Vault policy; no generic Admin endpoint; Owner personal-only; encrypted payload/keys/recovery Q-04; no support plaintext or share/export | Common + dynamic source/provider guards |
| `vault.generator.generate` | CSPRNG local, no persistence; không đọc Vault; Owner personal-only; encrypted payload/keys/recovery Q-04; no support plaintext or share/export | Common + dynamic source/provider guards |
| `vault.generator.copy` | Chỉ current authorized local buffer; generic toast; Owner personal-only; encrypted payload/keys/recovery Q-04; no support plaintext or share/export | Common + dynamic source/provider guards |
| `vault.support.read` | Qualified Admin + support.session.open + current one-module consent OR authorized SuperAdmin Emergency; target enabled; approved redacted projection only; no owner history/export/reveal/linked-module expansion; Owner personal-only; encrypted payload/keys/recovery Q-04; no support plaintext or share/export | Common + dynamic source/provider guards |

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
