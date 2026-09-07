# FX-34 — Automation: actions

Catalog v1 · 2026-09-07 · Docs-only. New key decomposition = Resolved delegated; source business decisions giữ nguyên; Blocked rows không được kích hoạt bằng grant.

## Sources và phạm vi

- [Feature / validation / state graph](../../features/34-automation-and-scheduler.md) — FX-34-BR-001, FX-34-BR-002, FX-34-BR-003, FX-34-BR-004, FX-34-BR-005, FX-34-BR-006. Các BR này áp cho feature; không gán sai một BR duy nhất cho mọi row.
- [UX specification](../../ux-ui/modules/34-automation.md); [exact screen bindings](../06-screen-bindings.md).
- [Authorization contract](../00-authorization-contract.md), [semantic field guards](../01-composition-and-field-guards.md), [SDK contract](../04-module-action-contract.md).
- Namespace `automation` là stable logical key, bind installed ModuleId trong manifest. **Q-07 workflow/egress effects; no implicit graph editor/unbounded script**.

## Catalog

“All prerequisites” ở row bao gồm explicit Requires **và** source/dynamic dependencies trong guard; danh sách Requires trống không có nghĩa bỏ owner/module/lifecycle checks. Every action denies unknown fields and unapproved semantic changes. Source state matrix luôn kiểm tra ở handler, không suy quyền từ verb hoặc tên button.

| Action key / hành vi | Kind / context | Admin checkbox? | Risk (DB mapping) | Status / gate | UI entry |
| --- | --- | --- | --- | --- | --- |
| <a id="automation-definition-read"></a>`automation.definition.read` — Xem definitions | QUERY / SELF | Yes, gated | Normal (Normal) | Blocked Q-07 | FX34-S01, FX34-S03 |
| <a id="automation-definition-create"></a>`automation.definition.create` — Tạo definition | COMMAND / SELF | Yes, gated | Normal (Normal) | Blocked Q-07 | FX34-S01, FX34-S02 |
| <a id="automation-definition-save"></a>`automation.definition.save` — Save immutable definition version | COMMAND / SELF | Yes, gated | Normal (Normal) | Blocked Q-07 | FX34-S02 |
| <a id="automation-definition-validate"></a>`automation.definition.validate` — Validate definition | COMMAND / SELF | Yes, gated | Normal (Normal) | Blocked Q-07 | FX34-S02 |
| <a id="automation-definition-enable"></a>`automation.definition.enable` — Enable definition | COMMAND / SELF | Yes, gated | Normal (Normal) | Blocked Q-07 | FX34-S01 |
| <a id="automation-definition-disable"></a>`automation.definition.disable` — Disable definition | COMMAND / SELF | Yes, gated | Normal (Normal) | Blocked Q-07 | FX34-S01 |
| <a id="automation-definition-trash"></a>`automation.definition.trash` — Đưa definition vào Thùng rác | COMMAND / SELF | Yes, gated | Lifecycle (Normal) | Blocked Q-07 | FX34-S01 |
| <a id="automation-definition-restore"></a>`automation.definition.restore` — Khôi phục definition từ Thùng rác | COMMAND / SELF | Yes, gated | Lifecycle (Normal) | Blocked Q-07 | FX34-S01 |
| <a id="automation-definition-purge"></a>`automation.definition.purge` — Xóa vĩnh viễn definition | COMMAND / SELF | Yes, gated | Destructive (Administrative) | Blocked Q-07 | FX34-S01 |
| <a id="automation-definition-history"></a>`automation.definition.history` — Xem lịch sử definition | QUERY / SELF | Yes, gated | Sensitive (Sensitive) | Blocked Q-07 | FX34-S03 |
| <a id="automation-run-read"></a>`automation.run.read` — Xem runs/step results | QUERY / SELF | Yes, gated | Normal (Normal) | Blocked Q-07 | FX34-S04 |
| <a id="automation-run-start"></a>`automation.run.start` — Chạy approved definition | COMMAND / SELF | Yes, gated | Normal (Normal) | Blocked Q-07 | FX34-S05 |
| <a id="automation-run-cancel"></a>`automation.run.cancel` — Yêu cầu dừng run | COMMAND / SELF | Yes, gated | Normal (Normal) | Blocked Q-07 | FX34-S05 |
| <a id="automation-run-retry-step"></a>`automation.run.retry_step` — Retry eligible failed step | COMMAND / SELF | Yes, gated | Normal (Normal) | Blocked Q-07 | FX34-S05 |
| <a id="automation-run-dry-run"></a>`automation.run.dry_run` — Preview simulated run | COMMAND / SELF | Yes, gated | Normal (Normal) | Blocked Q-07 | FX34-S05 |
| <a id="automation-schedule-update"></a>`automation.schedule.update` — Đổi schedule | COMMAND / SELF | Yes, gated | Normal (Normal) | Blocked Q-07 | FX34-S02 |
| <a id="automation-definition-import"></a>`automation.definition.import` — Import definition | COMPOSITE / SELF | Yes, gated | Disclosure (Sensitive) | Blocked Q-07 | FX34-S02 |
| <a id="automation-definition-export"></a>`automation.definition.export` — Export definition không secrets | COMPOSITE / SELF | Yes, gated | Disclosure (Sensitive) | Blocked Q-07 | FX34-S02 |
| <a id="automation-step-dispatch"></a>`automation.step.dispatch` — Execute one authorized step | WORKER / SYSTEM | No | Normal (Normal) | Blocked Q-07 | Trusted worker/deployment; no user control |
| <a id="automation-support-read"></a>`automation.support.read` — Xem safe support projection của module | QUERY / SUPPORT | Yes, gated | Sensitive (Sensitive) | Blocked Q-03; domain gates also apply | FX05-S03, FX05-S05 |

## Điều kiện riêng theo operation

| Action | Guard / validation / effects | Explicit dependencies bổ sung |
| --- | --- | --- |
| `automation.definition.read` | Q-07 workflow/egress effects; no implicit graph editor/unbounded script; Q-07 workflow/egress effects; no implicit graph editor/unbounded script | Common + dynamic source/provider guards |
| `automation.definition.create` | Owner/module/source/target actions all valid; published exact version; definition permission không grant target effects; Q-07 workflow/egress effects; no implicit graph editor/unbounded script | Common + dynamic source/provider guards |
| `automation.definition.save` | Owner/module/source/target actions all valid; published exact version; definition permission không grant target effects; Q-07 workflow/egress effects; no implicit graph editor/unbounded script | Common + dynamic source/provider guards |
| `automation.definition.validate` | Owner/module/source/target actions all valid; published exact version; definition permission không grant target effects; Q-07 workflow/egress effects; no implicit graph editor/unbounded script | Common + dynamic source/provider guards |
| `automation.definition.enable` | Owner/module/source/target actions all valid; published exact version; definition permission không grant target effects; Q-07 workflow/egress effects; no implicit graph editor/unbounded script | Common + dynamic source/provider guards |
| `automation.definition.disable` | Owner/module/source/target actions all valid; published exact version; definition permission không grant target effects; Q-07 workflow/egress effects; no implicit graph editor/unbounded script | Common + dynamic source/provider guards |
| `automation.definition.trash` | Q-07 workflow/egress effects; no implicit graph editor/unbounded script; preview aggregate, không purge; Q-07 workflow/egress effects; no implicit graph editor/unbounded script | Common + dynamic source/provider guards |
| `automation.definition.restore` | Q-07 workflow/egress effects; no implicit graph editor/unbounded script; đúng deletion cohort, parent hợp lệ; Q-07 workflow/egress effects; no implicit graph editor/unbounded script | Common + dynamic source/provider guards |
| `automation.definition.purge` | Q-07 workflow/egress effects; no implicit graph editor/unbounded script; chỉ Trash, preview pins/dependencies, xác nhận không thể hoàn tác; Q-07 workflow/egress effects; no implicit graph editor/unbounded script | Common + dynamic source/provider guards |
| `automation.definition.history` | Owner-only history, cùng owner/module; không share/support; Q-07 workflow/egress effects; no implicit graph editor/unbounded script | Common + dynamic source/provider guards |
| `automation.run.read` | Redacted results, own scope, no secrets; Q-07 workflow/egress effects; no implicit graph editor/unbounded script | Common + dynamic source/provider guards |
| `automation.run.start` | Pinned version; no unknown external-effect replay; dry-run chỉ mọi adapter hỗ trợ simulation; Q-07 workflow/egress effects; no implicit graph editor/unbounded script | Common + dynamic source/provider guards |
| `automation.run.cancel` | Pinned version; no unknown external-effect replay; dry-run chỉ mọi adapter hỗ trợ simulation; Q-07 workflow/egress effects; no implicit graph editor/unbounded script | Common + dynamic source/provider guards |
| `automation.run.retry_step` | Pinned version; no unknown external-effect replay; dry-run chỉ mọi adapter hỗ trợ simulation; Q-07 workflow/egress effects; no implicit graph editor/unbounded script | Common + dynamic source/provider guards |
| `automation.run.dry_run` | Pinned version; no unknown external-effect replay; dry-run chỉ mọi adapter hỗ trợ simulation; Q-07 workflow/egress effects; no implicit graph editor/unbounded script | Common + dynamic source/provider guards |
| `automation.schedule.update` | Owner timezone + no unapproved recurrence effects; Q-07 workflow/egress effects; no implicit graph editor/unbounded script | Common + dynamic source/provider guards |
| `automation.definition.import` | Schema/allowlist + target gates, no executable upload; Q-07 workflow/egress effects; no implicit graph editor/unbounded script | Common + dynamic source/provider guards |
| `automation.definition.export` | Schema/allowlist + target gates, no executable upload; Q-07 workflow/egress effects; no implicit graph editor/unbounded script | Common + dynamic source/provider guards |
| `automation.step.dispatch` | Trusted worker checks current owner/module/action/provider restrictions before side effect; no privileged automation bypass; Q-07 workflow/egress effects; no implicit graph editor/unbounded script | Common + dynamic source/provider guards |
| `automation.support.read` | Qualified Admin + support.session.open + current one-module consent OR authorized SuperAdmin Emergency; target enabled; approved redacted projection only; no owner history/export/reveal/linked-module expansion; Q-07 workflow/egress effects; no implicit graph editor/unbounded script | Common + dynamic source/provider guards |

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
