# FX-10 — Import / Export / Backup: actions

Catalog v1 · 2026-09-07 · Docs-only. New key decomposition = Resolved delegated; source business decisions giữ nguyên; Blocked rows không được kích hoạt bằng grant.

## Sources và phạm vi

- [Feature / validation / state graph](../../features/10-import-export-and-backup.md) — FX-10-BR-001, FX-10-BR-002, FX-10-BR-003, FX-10-BR-004, FX-10-BR-005, FX-10-BR-006. Các BR này áp cho feature; không gán sai một BR duy nhất cho mọi row.
- [UX specification](../../ux-ui/modules/10-import-export-backup.md); [exact screen bindings](../06-screen-bindings.md).
- [Authorization contract](../00-authorization-contract.md), [semantic field guards](../01-composition-and-field-guards.md), [SDK contract](../04-module-action-contract.md).
- Namespace `transfer` là stable logical key, bind installed ModuleId trong manifest. **Provider-specific scope; không universal data dump**.

## Catalog

“All prerequisites” ở row bao gồm explicit Requires **và** source/dynamic dependencies trong guard; danh sách Requires trống không có nghĩa bỏ owner/module/lifecycle checks. Every action denies unknown fields and unapproved semantic changes. Source state matrix luôn kiểm tra ở handler, không suy quyền từ verb hoặc tên button.

| Action key / hành vi | Kind / context | Admin checkbox? | Risk (DB mapping) | Status / gate | UI entry |
| --- | --- | --- | --- | --- | --- |
| <a id="transfer-import-preview"></a>`transfer.import.preview` — Validate/preview import | COMPOSITE / SELF | Yes, gated | Normal (Normal) | Resolved delegated: action contract; source business rules unchanged | FX10-S01 |
| <a id="transfer-import-commit"></a>`transfer.import.commit` — Áp dụng import | COMPOSITE / SELF | Yes, gated | Normal (Normal) | Resolved delegated: action contract; source business rules unchanged | FX10-S02 |
| <a id="transfer-import-read"></a>`transfer.import.read` — Xem import result | QUERY / SELF | Yes, gated | Normal (Normal) | Resolved delegated: action contract; source business rules unchanged | FX10-S04 |
| <a id="transfer-import-cancel"></a>`transfer.import.cancel` — Hủy import chưa commit | COMPOSITE / SELF | Yes, gated | Normal (Normal) | Resolved delegated: action contract; source business rules unchanged | FX10-S02 |
| <a id="transfer-export-request"></a>`transfer.export.request` — Tạo export job | COMPOSITE / SELF | Yes, gated | Disclosure (Sensitive) | Resolved delegated: action contract; source business rules unchanged | FX10-S03 |
| <a id="transfer-export-read"></a>`transfer.export.read` — Xem export result | QUERY / SELF | Yes, gated | Disclosure (Sensitive) | Resolved delegated: action contract; source business rules unchanged | FX10-S04 |
| <a id="transfer-export-download"></a>`transfer.export.download` — Download export artifact | COMPOSITE / SELF | Yes, gated | Disclosure (Sensitive) | Resolved delegated: action contract; source business rules unchanged | FX10-S04 |
| <a id="transfer-backup-read"></a>`transfer.backup.read` — Xem recovery inventory | QUERY / ADMIN | Yes, gated | Normal (Normal) | Blocked Q-08 | FX10-S05 |
| <a id="transfer-backup-request"></a>`transfer.backup.request` — Yêu cầu system backup | COMMAND / SUPER | No | Operational (Administrative) | Blocked Q-08; key recovery Q-04 | FX10-S05 |
| <a id="transfer-restore-preview"></a>`transfer.restore.preview` — Preview isolated recovery | COMMAND / SUPER | No | Operational (Administrative) | Blocked Q-08; key recovery Q-04 | FX10-S05 |
| <a id="transfer-restore-request"></a>`transfer.restore.request` — Yêu cầu restore được phê duyệt | COMMAND / SUPER | No | Operational (Administrative) | Blocked Q-08; key recovery Q-04 | FX10-S05 |
| <a id="transfer-worker-backup"></a>`transfer.worker.backup` — Thực thi authorized backup | WORKER / SYSTEM | No | Operational (Administrative) | Blocked Q-08/Q-04 | Trusted worker/deployment; no user control |
| <a id="transfer-worker-restore"></a>`transfer.worker.restore` — Thực thi isolated/approved restore | WORKER / SYSTEM | No | Operational (Administrative) | Blocked Q-08/Q-04 | Trusted worker/deployment; no user control |

## Điều kiện riêng theo operation

| Action | Guard / validation / effects | Explicit dependencies bổ sung |
| --- | --- | --- |
| `transfer.import.preview` | Phải có source import action; schema/owner/dedupe; Project/Task importer không tồn tại; Provider-specific scope; không universal data dump | Common + dynamic source/provider guards |
| `transfer.import.commit` | Phải có source import action; schema/owner/dedupe; Project/Task importer không tồn tại; Provider-specific scope; không universal data dump | Common + dynamic source/provider guards |
| `transfer.import.read` | Phải có source import action; schema/owner/dedupe; Project/Task importer không tồn tại; Provider-specific scope; không universal data dump | Common + dynamic source/provider guards |
| `transfer.import.cancel` | Phải có source import action; schema/owner/dedupe; Project/Task importer không tồn tại; Provider-specific scope; không universal data dump | Common + dynamic source/provider guards |
| `transfer.export.request` | Phải có source export action ở request/worker/download; không Support/Emergency; Provider-specific scope; không universal data dump | Common + dynamic source/provider guards |
| `transfer.export.read` | Phải có source export action ở request/worker/download; không Support/Emergency; Provider-specific scope; không universal data dump | Common + dynamic source/provider guards |
| `transfer.export.download` | Phải có source export action ở request/worker/download; không Support/Emergency; Provider-specific scope; không universal data dump | Common + dynamic source/provider guards |
| `transfer.backup.read` | Operational metadata only; Provider-specific scope; không universal data dump | Common + dynamic source/provider guards |
| `transfer.backup.request` | SuperAdmin-only + separate approved recovery operation; không one-click live overwrite; Provider-specific scope; không universal data dump | Common + dynamic source/provider guards |
| `transfer.restore.preview` | SuperAdmin-only + separate approved recovery operation; không one-click live overwrite; Provider-specific scope; không universal data dump | Common + dynamic source/provider guards |
| `transfer.restore.request` | SuperAdmin-only + separate approved recovery operation; không one-click live overwrite; Provider-specific scope; không universal data dump | Common + dynamic source/provider guards |
| `transfer.worker.backup` | Trusted operational identity + approved run/environment; không User export; Provider-specific scope; không universal data dump | Common + dynamic source/provider guards |
| `transfer.worker.restore` | Trusted operational identity + approved run/environment; không User export; Provider-specific scope; không universal data dump | Common + dynamic source/provider guards |

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
