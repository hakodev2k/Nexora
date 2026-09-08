# FX-37 — Personal Assets: actions

Catalog v1 · 2026-09-07 · Docs-only. New key decomposition = Resolved delegated; source business decisions giữ nguyên; Blocked rows không được kích hoạt bằng grant.

## Sources và phạm vi

- [Feature / validation / state graph](../../features/37-personal-assets.md) — FX-37-BR-001, FX-37-BR-002, FX-37-BR-003, FX-37-BR-004, FX-37-BR-005, FX-37-BR-006. Các BR này áp cho feature; không gán sai một BR duy nhất cho mọi row.
- [UX specification](../../ux-ui/modules/37-personal-assets.md); [exact screen bindings](../06-screen-bindings.md).
- [Authorization contract](../00-authorization-contract.md), [semantic field guards](../01-composition-and-field-guards.md), [SDK contract](../04-module-action-contract.md).
- Namespace `assets` là stable logical key, bind installed ModuleId trong manifest. **Own inventory; lifecycle history; không team assignment/ownership transfer**.

## Catalog

“All prerequisites” ở row bao gồm explicit Requires **và** source/dynamic dependencies trong guard; danh sách Requires trống không có nghĩa bỏ owner/module/lifecycle checks. Every action denies unknown fields and unapproved semantic changes. Source state matrix luôn kiểm tra ở handler, không suy quyền từ verb hoặc tên button.

| Action key / hành vi | Kind / context | Admin checkbox? | Risk (DB mapping) | Status / gate | UI entry |
| --- | --- | --- | --- | --- | --- |
| <a id="assets-asset-read"></a>`assets.asset.read` — Xem Personal Asset | QUERY / SELF | Yes, gated | Normal (Normal) | Resolved delegated: action contract; source business rules unchanged | FX37-S01, FX37-S03 |
| <a id="assets-asset-create"></a>`assets.asset.create` — Tạo Personal Asset | COMMAND / SELF | Yes, gated | Normal (Normal) | Resolved delegated: action contract; source business rules unchanged | FX37-S01, FX37-S02 |
| <a id="assets-asset-update"></a>`assets.asset.update` — Sửa Personal Asset | COMMAND / SELF | Yes, gated | Normal (Normal) | Resolved delegated: action contract; source business rules unchanged | FX37-S02 |
| <a id="assets-asset-transition"></a>`assets.asset.transition` — Đổi trạng thái Asset | COMMAND / SELF | Yes, gated | Lifecycle (Normal) | Resolved delegated: action contract; source business rules unchanged | FX37-S03 |
| <a id="assets-asset-archive"></a>`assets.asset.archive` — Archive asset | COMMAND / SELF | Yes, gated | Lifecycle (Normal) | Resolved delegated: action contract; source business rules unchanged | FX37-S01 |
| <a id="assets-asset-unarchive"></a>`assets.asset.unarchive` — Unarchive asset | COMMAND / SELF | Yes, gated | Lifecycle (Normal) | Resolved delegated: action contract; source business rules unchanged | FX37-S01 |
| <a id="assets-asset-trash"></a>`assets.asset.trash` — Đưa asset vào Thùng rác | COMMAND / SELF | Yes, gated | Lifecycle (Normal) | Resolved delegated: action contract; source business rules unchanged | FX37-S06 |
| <a id="assets-asset-restore"></a>`assets.asset.restore` — Khôi phục asset từ Thùng rác | COMMAND / SELF | Yes, gated | Lifecycle (Normal) | Resolved delegated: action contract; source business rules unchanged | FX37-S06 |
| <a id="assets-asset-purge"></a>`assets.asset.purge` — Xóa vĩnh viễn asset | COMMAND / SELF | Yes, gated | Destructive (Administrative) | Resolved delegated: action contract; source business rules unchanged | FX37-S06 |
| <a id="assets-asset-history"></a>`assets.asset.history` — Xem lịch sử asset | QUERY / SELF | Yes, gated | Sensitive (Sensitive) | Resolved delegated: action contract; source business rules unchanged | FX37-S06 |
| <a id="assets-serial-reveal"></a>`assets.serial.reveal` — Hiện serial | COMMAND / SELF | Yes, gated | Sensitive (Sensitive) | Resolved delegated; recent-auth timing Blocked Q-02 | FX37-S03 |
| <a id="assets-serial-copy"></a>`assets.serial.copy` — Copy serial | COMMAND / SELF | Yes, gated | Sensitive (Sensitive) | Resolved delegated; recent-auth timing Blocked Q-02 | FX37-S03 |
| <a id="assets-purchase-update"></a>`assets.purchase.update` — Sửa purchase metadata | COMMAND / SELF | Yes, gated | Normal (Normal) | Resolved delegated: action contract; source business rules unchanged | FX37-S02 |
| <a id="assets-warranty-update"></a>`assets.warranty.update` — Sửa warranty | COMMAND / SELF | Yes, gated | Normal (Normal) | Resolved delegated: action contract; source business rules unchanged | FX37-S04 |
| <a id="assets-warranty-set-reminder"></a>`assets.warranty.set_reminder` — Đặt warranty reminder | COMMAND / SELF | Yes, gated | Normal (Normal) | Resolved delegated: action contract; source business rules unchanged | FX37-S04 |
| <a id="assets-repair-read"></a>`assets.repair.read` — Xem Repair record | QUERY / SELF | Yes, gated | Normal (Normal) | Resolved delegated: action contract; source business rules unchanged | FX37-S04 |
| <a id="assets-repair-create"></a>`assets.repair.create` — Tạo Repair record | COMMAND / SELF | Yes, gated | Normal (Normal) | Resolved delegated: action contract; source business rules unchanged | FX37-S04 |
| <a id="assets-repair-update"></a>`assets.repair.update` — Sửa Repair record | COMMAND / SELF | Yes, gated | Normal (Normal) | Resolved delegated: action contract; source business rules unchanged | FX37-S04 |
| <a id="assets-repair-remove"></a>`assets.repair.remove` — Xóa repair record | COMMAND / SELF | Yes, gated | Normal (Normal) | Resolved delegated: action contract; source business rules unchanged | FX37-S04 |
| <a id="assets-component-read"></a>`assets.component.read` — Xem Owned component | QUERY / SELF | Yes, gated | Normal (Normal) | Resolved delegated: action contract; source business rules unchanged | FX37-S05 |
| <a id="assets-component-create"></a>`assets.component.create` — Tạo Owned component | COMMAND / SELF | Yes, gated | Normal (Normal) | Resolved delegated: action contract; source business rules unchanged | FX37-S05 |
| <a id="assets-component-update"></a>`assets.component.update` — Sửa Owned component | COMMAND / SELF | Yes, gated | Normal (Normal) | Resolved delegated: action contract; source business rules unchanged | FX37-S05 |
| <a id="assets-component-remove"></a>`assets.component.remove` — Gỡ component | COMMAND / SELF | Yes, gated | Normal (Normal) | Resolved delegated: action contract; source business rules unchanged | FX37-S05 |
| <a id="assets-accessory-link"></a>`assets.accessory.link` — Liên kết accessory | COMMAND / SELF | Yes, gated | Normal (Normal) | Resolved delegated: action contract; source business rules unchanged | FX37-S05 |
| <a id="assets-accessory-unlink"></a>`assets.accessory.unlink` — Gỡ liên kết accessory | COMMAND / SELF | Yes, gated | Normal (Normal) | Resolved delegated: action contract; source business rules unchanged | FX37-S05 |
| <a id="assets-loan-lend"></a>`assets.loan.lend` — Ghi nhận cho mượn | COMMAND / SELF | Yes, gated | Normal (Normal) | Resolved delegated: action contract; source business rules unchanged | FX37-S03 |
| <a id="assets-loan-return"></a>`assets.loan.return` — Ghi nhận nhận lại | COMMAND / SELF | Yes, gated | Normal (Normal) | Resolved delegated: action contract; source business rules unchanged | FX37-S03 |
| <a id="assets-asset-share"></a>`assets.asset.share` — Quản lý link chỉ-đọc của asset | COMPOSITE / SELF | Yes, gated | Disclosure (Sensitive) | Blocked Q-03 | FX37-S03 |
| <a id="assets-support-read"></a>`assets.support.read` — Xem safe support projection của module | QUERY / SUPPORT | Yes, gated | Sensitive (Sensitive) | Blocked Q-03; domain gates also apply | FX05-S03, FX05-S05 |

## Điều kiện riêng theo operation

| Action | Guard / validation / effects | Explicit dependencies bổ sung |
| --- | --- | --- |
| `assets.asset.read` | Own inventory; lifecycle history; không team assignment/ownership transfer; Own inventory; lifecycle history; không team assignment/ownership transfer | Common + dynamic source/provider guards |
| `assets.asset.create` | Own inventory; lifecycle history; không team assignment/ownership transfer; Own inventory; lifecycle history; không team assignment/ownership transfer | Common + dynamic source/provider guards |
| `assets.asset.update` | Own inventory; lifecycle history; không team assignment/ownership transfer; Own inventory; lifecycle history; không team assignment/ownership transfer | `assets.asset.read` |
| `assets.asset.transition` | FX-37 lifecycle, reason/history where required; không generic Update state bypass; Own inventory; lifecycle history; không team assignment/ownership transfer | Common + dynamic source/provider guards |
| `assets.asset.archive` | Own inventory; lifecycle history; không team assignment/ownership transfer; ngoài Trash, chưa Archived; Own inventory; lifecycle history; không team assignment/ownership transfer | Common + dynamic source/provider guards |
| `assets.asset.unarchive` | Own inventory; lifecycle history; không team assignment/ownership transfer; Archived, khôi phục previous state; Own inventory; lifecycle history; không team assignment/ownership transfer | Common + dynamic source/provider guards |
| `assets.asset.trash` | Own inventory; lifecycle history; không team assignment/ownership transfer; preview aggregate, không purge; Own inventory; lifecycle history; không team assignment/ownership transfer | Common + dynamic source/provider guards |
| `assets.asset.restore` | Own inventory; lifecycle history; không team assignment/ownership transfer; đúng deletion cohort, parent hợp lệ; Own inventory; lifecycle history; không team assignment/ownership transfer | Common + dynamic source/provider guards |
| `assets.asset.purge` | Own inventory; lifecycle history; không team assignment/ownership transfer; chỉ Trash, preview pins/dependencies, xác nhận không thể hoàn tác; Own inventory; lifecycle history; không team assignment/ownership transfer | Common + dynamic source/provider guards |
| `assets.asset.history` | Owner-only history, cùng owner/module; không share/support; Own inventory; lifecycle history; không team assignment/ownership transfer | Common + dynamic source/provider guards |
| `assets.serial.reveal` | Own resource + recent authentication policy; masked default, no search/share payload; Own inventory; lifecycle history; không team assignment/ownership transfer | Common + dynamic source/provider guards |
| `assets.serial.copy` | Own resource + recent authentication policy; masked default, no search/share payload; Own inventory; lifecycle history; không team assignment/ownership transfer | Common + dynamic source/provider guards |
| `assets.purchase.update` | Source reference giữ cùng owner; no Finance post; Own inventory; lifecycle history; không team assignment/ownership transfer | Common + dynamic source/provider guards |
| `assets.warranty.update` | Expiry/source version valid; one approved reminder per source; Own inventory; lifecycle history; không team assignment/ownership transfer | Common + dynamic source/provider guards |
| `assets.warranty.set_reminder` | Expiry/source version valid; one approved reminder per source; Own inventory; lifecycle history; không team assignment/ownership transfer | Common + dynamic source/provider guards |
| `assets.repair.read` | Own inventory; lifecycle history; không team assignment/ownership transfer; Own inventory; lifecycle history; không team assignment/ownership transfer | Common + dynamic source/provider guards |
| `assets.repair.create` | Own inventory; lifecycle history; không team assignment/ownership transfer; Own inventory; lifecycle history; không team assignment/ownership transfer | Common + dynamic source/provider guards |
| `assets.repair.update` | Own inventory; lifecycle history; không team assignment/ownership transfer; Own inventory; lifecycle history; không team assignment/ownership transfer | `assets.repair.read` |
| `assets.repair.remove` | Preview evidence/history according to FX-37, no linked File purge; Own inventory; lifecycle history; không team assignment/ownership transfer | Common + dynamic source/provider guards |
| `assets.component.read` | Own inventory; lifecycle history; không team assignment/ownership transfer; Own inventory; lifecycle history; không team assignment/ownership transfer | Common + dynamic source/provider guards |
| `assets.component.create` | Own inventory; lifecycle history; không team assignment/ownership transfer; Own inventory; lifecycle history; không team assignment/ownership transfer | Common + dynamic source/provider guards |
| `assets.component.update` | Own inventory; lifecycle history; không team assignment/ownership transfer; Own inventory; lifecycle history; không team assignment/ownership transfer | `assets.component.read` |
| `assets.component.remove` | Parent ownership, explicit dependency preview; Own inventory; lifecycle history; không team assignment/ownership transfer | Common + dynamic source/provider guards |
| `assets.accessory.link` | Independent linked Asset remains; no cascade purge; Own inventory; lifecycle history; không team assignment/ownership transfer | Common + dynamic source/provider guards |
| `assets.accessory.unlink` | Independent linked Asset remains; no cascade purge; Own inventory; lifecycle history; không team assignment/ownership transfer | Common + dynamic source/provider guards |
| `assets.loan.lend` | Personal lending log; no data access to borrower, no workspace; Own inventory; lifecycle history; không team assignment/ownership transfer | Common + dynamic source/provider guards |
| `assets.loan.return` | Personal lending log; no data access to borrower, no workspace; Own inventory; lifecycle history; không team assignment/ownership transfer | Common + dynamic source/provider guards |
| `assets.asset.share` | Source eligible, SharingEnabled, projection chính xác; kết hợp sharing.link.*; không history/reason/secret; Own inventory; lifecycle history; không team assignment/ownership transfer | `sharing.link.read` |
| `assets.support.read` | Qualified Admin + support.session.open + current one-module consent OR authorized SuperAdmin Emergency; target enabled; approved redacted projection only; no owner history/export/reveal/linked-module expansion; Own inventory; lifecycle history; không team assignment/ownership transfer | Common + dynamic source/provider guards |

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
