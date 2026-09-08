# FX-08 — Trash / Activity / Audit: actions

Catalog v1 · 2026-09-07 · Docs-only. New key decomposition = Resolved delegated; source business decisions giữ nguyên; Blocked rows không được kích hoạt bằng grant.

## Sources và phạm vi

- [Feature / validation / state graph](../../features/08-trash-activity-and-audit.md) — FX-08-BR-001, FX-08-BR-002, FX-08-BR-003, FX-08-BR-004, FX-08-BR-005, FX-08-BR-006. Các BR này áp cho feature; không gán sai một BR duy nhất cho mọi row.
- [UX specification](../../ux-ui/modules/08-trash-activity-audit.md); [exact screen bindings](../06-screen-bindings.md).
- [Authorization contract](../00-authorization-contract.md), [semantic field guards](../01-composition-and-field-guards.md), [SDK contract](../04-module-action-contract.md).
- Namespace `lifecycle` là stable logical key, bind installed ModuleId trong manifest. **Gateway không cấp quyền domain; dispatch đúng source contract**.

## Catalog

“All prerequisites” ở row bao gồm explicit Requires **và** source/dynamic dependencies trong guard; danh sách Requires trống không có nghĩa bỏ owner/module/lifecycle checks. Every action denies unknown fields and unapproved semantic changes. Source state matrix luôn kiểm tra ở handler, không suy quyền từ verb hoặc tên button.

| Action key / hành vi | Kind / context | Admin checkbox? | Risk (DB mapping) | Status / gate | UI entry |
| --- | --- | --- | --- | --- | --- |
| <a id="lifecycle-trash-read"></a>`lifecycle.trash.read` — Xem Trash inventory | QUERY / SELF | Yes, gated | Normal (Normal) | Resolved delegated: action contract; source business rules unchanged | FX08-S01 |
| <a id="lifecycle-activity-read"></a>`lifecycle.activity.read` — Xem resource Activity | COMPOSITE / SELF | Yes, gated | Normal (Normal) | Resolved delegated: action contract; source business rules unchanged | FX08-S03 |
| <a id="lifecycle-resource-preview"></a>`lifecycle.resource.preview` — Preview lifecycle operation | COMPOSITE / SELF | Yes, gated | Lifecycle (Normal) | Resolved delegated: action contract; source business rules unchanged | FX08-S01, FX08-S02 |
| <a id="lifecycle-resource-trash"></a>`lifecycle.resource.trash` — Dispatch đưa vào Trash | COMPOSITE / SELF | Yes, gated | Lifecycle (Normal) | Resolved delegated: action contract; source business rules unchanged | FX08-S01 |
| <a id="lifecycle-resource-restore"></a>`lifecycle.resource.restore` — Dispatch restore cohort | COMPOSITE / SELF | Yes, gated | Lifecycle (Normal) | Resolved delegated: action contract; source business rules unchanged | FX08-S01, FX08-S02 |
| <a id="lifecycle-resource-purge"></a>`lifecycle.resource.purge` — Dispatch purge | COMPOSITE / SELF | Yes, gated | Destructive (Administrative) | Resolved delegated: action contract; source business rules unchanged | FX08-S01, FX08-S02 |
| <a id="lifecycle-audit-read"></a>`lifecycle.audit.read` — Xem audit đã redacted | QUERY / ADMIN | Yes, gated | Sensitive (Sensitive) | Resolved delegated: action contract; source business rules unchanged | FX08-S04 |
| <a id="lifecycle-audit-append"></a>`lifecycle.audit.append` — Append audit | WORKER / SYSTEM | No | Security (Administrative) | Resolved delegated: action contract; source business rules unchanged | Trusted worker/deployment; no user control |

## Điều kiện riêng theo operation

| Action | Guard / validation / effects | Explicit dependencies bổ sung |
| --- | --- | --- |
| `lifecycle.trash.read` | Kèm read/trash projection của từng source, current module gate; Gateway không cấp quyền domain; dispatch đúng source contract | Common + dynamic source/provider guards |
| `lifecycle.activity.read` | Kèm source.read/history; không Support/link history; Gateway không cấp quyền domain; dispatch đúng source contract | Common + dynamic source/provider guards |
| `lifecycle.resource.preview` | Bắt buộc source action tương ứng; parent lock, cohort và pins; không universal delete permission; Gateway không cấp quyền domain; dispatch đúng source contract | Common + dynamic source/provider guards |
| `lifecycle.resource.trash` | Bắt buộc source action tương ứng; parent lock, cohort và pins; không universal delete permission; Gateway không cấp quyền domain; dispatch đúng source contract | Common + dynamic source/provider guards |
| `lifecycle.resource.restore` | Bắt buộc source action tương ứng; parent lock, cohort và pins; không universal delete permission; Gateway không cấp quyền domain; dispatch đúng source contract | Common + dynamic source/provider guards |
| `lifecycle.resource.purge` | Bắt buộc source action tương ứng; parent lock, cohort và pins; không universal delete permission; Gateway không cấp quyền domain; dispatch đúng source contract | Common + dynamic source/provider guards |
| `lifecycle.audit.read` | Admin operational grant, không User content hoặc raw secret; Gateway không cấp quyền domain; dispatch đúng source contract | Common + dynamic source/provider guards |
| `lifecycle.audit.append` | Trusted audited transaction only; no edit/delete counterpart; Gateway không cấp quyền domain; dispatch đúng source contract | Common + dynamic source/provider guards |

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
