# FX-38 — Digital Assets: actions

Catalog v1 · 2026-09-07 · Docs-only. New key decomposition = Resolved delegated; source business decisions giữ nguyên; Blocked rows không được kích hoạt bằng grant.

## Sources và phạm vi

- [Feature / validation / state graph](../../features/38-digital-assets.md) — FX-38-BR-001, FX-38-BR-002, FX-38-BR-003, FX-38-BR-004, FX-38-BR-005, FX-38-BR-006. Các BR này áp cho feature; không gán sai một BR duy nhất cho mọi row.
- [UX specification](../../ux-ui/modules/38-digital-assets.md); [exact screen bindings](../06-screen-bindings.md).
- [Authorization contract](../00-authorization-contract.md), [semantic field guards](../01-composition-and-field-guards.md), [SDK contract](../04-module-action-contract.md).
- Namespace `digital` là stable logical key, bind installed ModuleId trong manifest. **Metadata tracking only; không infrastructure control plane/payment renewal**.

## Catalog

“All prerequisites” ở row bao gồm explicit Requires **và** source/dynamic dependencies trong guard; danh sách Requires trống không có nghĩa bỏ owner/module/lifecycle checks. Every action denies unknown fields and unapproved semantic changes. Source state matrix luôn kiểm tra ở handler, không suy quyền từ verb hoặc tên button.

| Action key / hành vi | Kind / context | Admin checkbox? | Risk (DB mapping) | Status / gate | UI entry |
| --- | --- | --- | --- | --- | --- |
| <a id="digital-asset-read"></a>`digital.asset.read` — Xem Digital Asset | QUERY / SELF | Yes, gated | Normal (Normal) | Resolved delegated: action contract; source business rules unchanged | FX38-S01, FX38-S03 |
| <a id="digital-asset-create"></a>`digital.asset.create` — Tạo Digital Asset | COMMAND / SELF | Yes, gated | Normal (Normal) | Resolved delegated: action contract; source business rules unchanged | FX38-S02 |
| <a id="digital-asset-update"></a>`digital.asset.update` — Sửa Digital Asset | COMMAND / SELF | Yes, gated | Normal (Normal) | Resolved delegated: action contract; source business rules unchanged | FX38-S02 |
| <a id="digital-asset-cancel"></a>`digital.asset.cancel` — Đánh dấu canceled metadata | COMMAND / SELF | Yes, gated | Normal (Normal) | Resolved delegated: action contract; source business rules unchanged | FX38-S03 |
| <a id="digital-asset-archive"></a>`digital.asset.archive` — Archive asset | COMMAND / SELF | Yes, gated | Lifecycle (Normal) | Resolved delegated: action contract; source business rules unchanged | FX38-S01 |
| <a id="digital-asset-unarchive"></a>`digital.asset.unarchive` — Unarchive asset | COMMAND / SELF | Yes, gated | Lifecycle (Normal) | Resolved delegated: action contract; source business rules unchanged | FX38-S01 |
| <a id="digital-asset-trash"></a>`digital.asset.trash` — Đưa asset vào Thùng rác | COMMAND / SELF | Yes, gated | Lifecycle (Normal) | Resolved delegated: action contract; source business rules unchanged | FX38-S01 |
| <a id="digital-asset-restore"></a>`digital.asset.restore` — Khôi phục asset từ Thùng rác | COMMAND / SELF | Yes, gated | Lifecycle (Normal) | Resolved delegated: action contract; source business rules unchanged | FX38-S01 |
| <a id="digital-asset-purge"></a>`digital.asset.purge` — Xóa vĩnh viễn asset | COMMAND / SELF | Yes, gated | Destructive (Administrative) | Resolved delegated: action contract; source business rules unchanged | FX38-S01 |
| <a id="digital-asset-history"></a>`digital.asset.history` — Xem lịch sử asset | QUERY / SELF | Yes, gated | Sensitive (Sensitive) | Resolved delegated: action contract; source business rules unchanged | FX38-S04 |
| <a id="digital-renewal-record"></a>`digital.renewal.record` — Ghi nhận renewal | COMMAND / SELF | Yes, gated | Normal (Normal) | Resolved delegated: action contract; source business rules unchanged | FX38-S04 |
| <a id="digital-credential-reference"></a>`digital.credential.reference` — Gắn/thay Vault reference | COMMAND / SELF | Yes, gated | Sensitive (Sensitive) | Blocked Q-04 for Vault resolution | FX38-S02 |
| <a id="digital-observation-inspect"></a>`digital.observation.inspect` — Lấy public domain/TLS observation | COMMAND / SELF | Yes, gated | Network (Administrative) | Blocked Q-07 | FX38-S03 |
| <a id="digital-certificate-parse"></a>`digital.certificate.parse` — Parse public certificate local | LOCAL / SELF | No | Normal (Normal) | Resolved delegated: action contract; source business rules unchanged | FX38-S02 |
| <a id="digital-asset-set-reminder"></a>`digital.asset.set_reminder` — Đặt expiry reminder | COMMAND / SELF | Yes, gated | Normal (Normal) | Resolved delegated: action contract; source business rules unchanged | FX38-S03 |
| <a id="digital-asset-share"></a>`digital.asset.share` — Quản lý link chỉ-đọc của asset | COMPOSITE / SELF | Yes, gated | Disclosure (Sensitive) | Blocked Q-03 | FX38-S03 |
| <a id="digital-support-read"></a>`digital.support.read` — Xem safe support projection của module | QUERY / SUPPORT | Yes, gated | Sensitive (Sensitive) | Blocked Q-03; domain gates also apply | FX05-S03, FX05-S05 |

## Điều kiện riêng theo operation

| Action | Guard / validation / effects | Explicit dependencies bổ sung |
| --- | --- | --- |
| `digital.asset.read` | Metadata tracking only; không infrastructure control plane/payment renewal; Metadata tracking only; không infrastructure control plane/payment renewal | Common + dynamic source/provider guards |
| `digital.asset.create` | Metadata tracking only; không infrastructure control plane/payment renewal; Metadata tracking only; không infrastructure control plane/payment renewal | Common + dynamic source/provider guards |
| `digital.asset.update` | Metadata tracking only; không infrastructure control plane/payment renewal; Metadata tracking only; không infrastructure control plane/payment renewal | `digital.asset.read` |
| `digital.asset.cancel` | Không hủy service bên ngoài; Metadata tracking only; không infrastructure control plane/payment renewal | Common + dynamic source/provider guards |
| `digital.asset.archive` | Metadata tracking only; không infrastructure control plane/payment renewal; ngoài Trash, chưa Archived; Metadata tracking only; không infrastructure control plane/payment renewal | Common + dynamic source/provider guards |
| `digital.asset.unarchive` | Metadata tracking only; không infrastructure control plane/payment renewal; Archived, khôi phục previous state; Metadata tracking only; không infrastructure control plane/payment renewal | Common + dynamic source/provider guards |
| `digital.asset.trash` | Metadata tracking only; không infrastructure control plane/payment renewal; preview aggregate, không purge; Metadata tracking only; không infrastructure control plane/payment renewal | Common + dynamic source/provider guards |
| `digital.asset.restore` | Metadata tracking only; không infrastructure control plane/payment renewal; đúng deletion cohort, parent hợp lệ; Metadata tracking only; không infrastructure control plane/payment renewal | Common + dynamic source/provider guards |
| `digital.asset.purge` | Metadata tracking only; không infrastructure control plane/payment renewal; chỉ Trash, preview pins/dependencies, xác nhận không thể hoàn tác; Metadata tracking only; không infrastructure control plane/payment renewal | Common + dynamic source/provider guards |
| `digital.asset.history` | Owner-only history, cùng owner/module; không share/support; Metadata tracking only; không infrastructure control plane/payment renewal | Common + dynamic source/provider guards |
| `digital.renewal.record` | Explicit new expiry/cost metadata; no charge or provider write; Metadata tracking only; không infrastructure control plane/payment renewal | Common + dynamic source/provider guards |
| `digital.credential.reference` | Same owner + purpose validation; không reveal từ Digital Assets; Metadata tracking only; không infrastructure control plane/payment renewal | Common + dynamic source/provider guards |
| `digital.observation.inspect` | Approved public-only adapter + Q-07 outbound policy; Metadata tracking only; không infrastructure control plane/payment renewal | Common + dynamic source/provider guards |
| `digital.certificate.parse` | No private key input; local-only bounded parser; Metadata tracking only; không infrastructure control plane/payment renewal | Common + dynamic source/provider guards |
| `digital.asset.set_reminder` | Current source expiry/version; no auto-renew; Metadata tracking only; không infrastructure control plane/payment renewal | Common + dynamic source/provider guards |
| `digital.asset.share` | Source eligible, SharingEnabled, projection chính xác; kết hợp sharing.link.*; không history/reason/secret; Metadata tracking only; không infrastructure control plane/payment renewal | `sharing.link.read` |
| `digital.support.read` | Qualified Admin + support.session.open + current one-module consent OR authorized SuperAdmin Emergency; target enabled; approved redacted projection only; no owner history/export/reveal/linked-module expansion; Metadata tracking only; không infrastructure control plane/payment renewal | Common + dynamic source/provider guards |

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
