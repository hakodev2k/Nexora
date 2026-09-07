# FX-24 — Tags / Collections / Templates: actions

Catalog v1 · 2026-09-07 · Docs-only. New key decomposition = Resolved delegated; source business decisions giữ nguyên; Blocked rows không được kích hoạt bằng grant.

## Sources và phạm vi

- [Feature / validation / state graph](../../features/24-organization-and-templates.md) — FX-24-BR-001, FX-24-BR-002, FX-24-BR-003, FX-24-BR-004, FX-24-BR-005. Các BR này áp cho feature; không gán sai một BR duy nhất cho mọi row.
- [UX specification](../../ux-ui/modules/24-organization-tags-templates.md); [exact screen bindings](../06-screen-bindings.md).
- [Authorization contract](../00-authorization-contract.md), [semantic field guards](../01-composition-and-field-guards.md), [SDK contract](../04-module-action-contract.md).
- Namespace `organization` là stable logical key, bind installed ModuleId trong manifest. **Namespace và source provider quyết định cardinality/lifecycle; không universal grant**.

## Catalog

“All prerequisites” ở row bao gồm explicit Requires **và** source/dynamic dependencies trong guard; danh sách Requires trống không có nghĩa bỏ owner/module/lifecycle checks. Every action denies unknown fields and unapproved semantic changes. Source state matrix luôn kiểm tra ở handler, không suy quyền từ verb hoặc tên button.

| Action key / hành vi | Kind / context | Admin checkbox? | Risk (DB mapping) | Status / gate | UI entry |
| --- | --- | --- | --- | --- | --- |
| <a id="organization-tag-read"></a>`organization.tag.read` — Xem tags theo namespace | QUERY / SELF | Yes, gated | Normal (Normal) | Resolved delegated: action contract; source business rules unchanged | FX24-S01 |
| <a id="organization-tag-create"></a>`organization.tag.create` — Tạo Tag | COMPOSITE / SELF | Yes, gated | Normal (Normal) | Resolved delegated: action contract; source business rules unchanged | FX24-S01 |
| <a id="organization-tag-rename"></a>`organization.tag.rename` — Đổi tên Tag | COMPOSITE / SELF | Yes, gated | Normal (Normal) | Resolved delegated: action contract; source business rules unchanged | FX24-S01 |
| <a id="organization-tag-remove"></a>`organization.tag.remove` — Xóa Tag | COMPOSITE / SELF | Yes, gated | Normal (Normal) | Resolved delegated: action contract; source business rules unchanged | FX24-S01 |
| <a id="organization-tag-assign"></a>`organization.tag.assign` — Gắn/gỡ Tag trên source | COMPOSITE / SELF | Yes, gated | Normal (Normal) | Resolved delegated: action contract; source business rules unchanged | FX24-S01 |
| <a id="organization-collection-read"></a>`organization.collection.read` — Xem Collection | QUERY / SELF | Yes, gated | Normal (Normal) | Resolved delegated: action contract; source business rules unchanged | FX24-S02 |
| <a id="organization-collection-create"></a>`organization.collection.create` — Tạo Collection | COMMAND / SELF | Yes, gated | Normal (Normal) | Resolved delegated: action contract; source business rules unchanged | FX24-S02 |
| <a id="organization-collection-update"></a>`organization.collection.update` — Sửa Collection | COMMAND / SELF | Yes, gated | Normal (Normal) | Resolved delegated: action contract; source business rules unchanged | FX24-S02 |
| <a id="organization-collection-add"></a>`organization.collection.add` — Thêm source reference | COMMAND / SELF | Yes, gated | Normal (Normal) | Resolved delegated: action contract; source business rules unchanged | FX24-S03 |
| <a id="organization-collection-remove"></a>`organization.collection.remove` — Gỡ source reference | COMMAND / SELF | Yes, gated | Normal (Normal) | Resolved delegated: action contract; source business rules unchanged | FX24-S03 |
| <a id="organization-collection-reorder"></a>`organization.collection.reorder` — Sắp xếp references | COMMAND / SELF | Yes, gated | Normal (Normal) | Resolved delegated: action contract; source business rules unchanged | FX24-S03 |
| <a id="organization-collection-delete"></a>`organization.collection.delete` — Xóa collection container | COMMAND / SELF | Yes, gated | Normal (Normal) | Resolved delegated: action contract; source business rules unchanged | FX24-S02 |
| <a id="organization-template-read"></a>`organization.template.read` — Xem Template | QUERY / SELF | Yes, gated | Normal (Normal) | Resolved delegated: action contract; source business rules unchanged | FX24-S04 |
| <a id="organization-template-create"></a>`organization.template.create` — Tạo Template | COMMAND / SELF | Yes, gated | Normal (Normal) | Resolved delegated: action contract; source business rules unchanged | FX24-S04 |
| <a id="organization-template-update"></a>`organization.template.update` — Sửa Template | COMMAND / SELF | Yes, gated | Normal (Normal) | Resolved delegated: action contract; source business rules unchanged | FX24-S04 |
| <a id="organization-template-archive"></a>`organization.template.archive` — Archive template | COMMAND / SELF | Yes, gated | Lifecycle (Normal) | Resolved delegated: action contract; source business rules unchanged | FX24-S04 |
| <a id="organization-template-unarchive"></a>`organization.template.unarchive` — Unarchive template | COMMAND / SELF | Yes, gated | Lifecycle (Normal) | Resolved delegated: action contract; source business rules unchanged | FX24-S04 |
| <a id="organization-template-trash"></a>`organization.template.trash` — Đưa template vào Thùng rác | COMMAND / SELF | Yes, gated | Lifecycle (Normal) | Resolved delegated: action contract; source business rules unchanged | FX24-S04 |
| <a id="organization-template-restore"></a>`organization.template.restore` — Khôi phục template từ Thùng rác | COMMAND / SELF | Yes, gated | Lifecycle (Normal) | Resolved delegated: action contract; source business rules unchanged | FX24-S04 |
| <a id="organization-template-purge"></a>`organization.template.purge` — Xóa vĩnh viễn template | COMMAND / SELF | Yes, gated | Destructive (Administrative) | Resolved delegated: action contract; source business rules unchanged | FX24-S04 |
| <a id="organization-template-instantiate"></a>`organization.template.instantiate` — Tạo resource từ Template | COMPOSITE / SELF | Yes, gated | Normal (Normal) | Resolved delegated: action contract; source business rules unchanged | FX24-S05 |
| <a id="organization-support-read"></a>`organization.support.read` — Xem safe support projection của module | QUERY / SUPPORT | Yes, gated | Sensitive (Sensitive) | Resolved delegated: diagnostic projection only; private body excluded unless source scope explicitly permits | FX05-S03, FX05-S05 |
| <a id="organization-collection-share"></a>`organization.collection.share` — Quản lý link chỉ-đọc của collection | COMPOSITE / SELF | Yes, gated | Disclosure (Sensitive) | Blocked Q-03 until provider projection approved | FX24-S03 |

## Điều kiện riêng theo operation

| Action | Guard / validation / effects | Explicit dependencies bổ sung |
| --- | --- | --- |
| `organization.tag.read` | Namespace và source provider quyết định cardinality/lifecycle; không universal grant; Namespace và source provider quyết định cardinality/lifecycle; không universal grant | Common + dynamic source/provider guards |
| `organization.tag.create` | Namespace source checks; Documents delegate documents.tag.* và page.save; Projects/Tasks chung namespace, module khác riêng; không bypass reference blockers; Namespace và source provider quyết định cardinality/lifecycle; không universal grant | Common + dynamic source/provider guards |
| `organization.tag.rename` | Namespace source checks; Documents delegate documents.tag.* và page.save; Projects/Tasks chung namespace, module khác riêng; không bypass reference blockers; Namespace và source provider quyết định cardinality/lifecycle; không universal grant | Common + dynamic source/provider guards |
| `organization.tag.remove` | Namespace source checks; Documents delegate documents.tag.* và page.save; Projects/Tasks chung namespace, module khác riêng; không bypass reference blockers; Namespace và source provider quyết định cardinality/lifecycle; không universal grant | Common + dynamic source/provider guards |
| `organization.tag.assign` | Namespace source checks; Documents delegate documents.tag.* và page.save; Projects/Tasks chung namespace, module khác riêng; không bypass reference blockers; Namespace và source provider quyết định cardinality/lifecycle; không universal grant | Common + dynamic source/provider guards |
| `organization.collection.read` | Own collection metadata; refs không thay owner/source lifecycle; Namespace và source provider quyết định cardinality/lifecycle; không universal grant | Common + dynamic source/provider guards |
| `organization.collection.create` | Own collection metadata; refs không thay owner/source lifecycle; Namespace và source provider quyết định cardinality/lifecycle; không universal grant | Common + dynamic source/provider guards |
| `organization.collection.update` | Own collection metadata; refs không thay owner/source lifecycle; Namespace và source provider quyết định cardinality/lifecycle; không universal grant | `organization.collection.read` |
| `organization.collection.add` | Source read khi thêm/xem; delete không xóa source; không tự mở public sharing; Namespace và source provider quyết định cardinality/lifecycle; không universal grant | Common + dynamic source/provider guards |
| `organization.collection.remove` | Source read khi thêm/xem; delete không xóa source; không tự mở public sharing; Namespace và source provider quyết định cardinality/lifecycle; không universal grant | Common + dynamic source/provider guards |
| `organization.collection.reorder` | Source read khi thêm/xem; delete không xóa source; không tự mở public sharing; Namespace và source provider quyết định cardinality/lifecycle; không universal grant | Common + dynamic source/provider guards |
| `organization.collection.delete` | Source read khi thêm/xem; delete không xóa source; không tự mở public sharing; Namespace và source provider quyết định cardinality/lifecycle; không universal grant | Common + dynamic source/provider guards |
| `organization.template.read` | Source declared template provider; không include secrets; immutable source kinds theo provider; Namespace và source provider quyết định cardinality/lifecycle; không universal grant | Common + dynamic source/provider guards |
| `organization.template.create` | Source declared template provider; không include secrets; immutable source kinds theo provider; Namespace và source provider quyết định cardinality/lifecycle; không universal grant | Common + dynamic source/provider guards |
| `organization.template.update` | Source declared template provider; không include secrets; immutable source kinds theo provider; Namespace và source provider quyết định cardinality/lifecycle; không universal grant | `organization.template.read` |
| `organization.template.archive` | Namespace và source provider quyết định cardinality/lifecycle; không universal grant; ngoài Trash, chưa Archived; Namespace và source provider quyết định cardinality/lifecycle; không universal grant | Common + dynamic source/provider guards |
| `organization.template.unarchive` | Namespace và source provider quyết định cardinality/lifecycle; không universal grant; Archived, khôi phục previous state; Namespace và source provider quyết định cardinality/lifecycle; không universal grant | Common + dynamic source/provider guards |
| `organization.template.trash` | Namespace và source provider quyết định cardinality/lifecycle; không universal grant; preview aggregate, không purge; Namespace và source provider quyết định cardinality/lifecycle; không universal grant | Common + dynamic source/provider guards |
| `organization.template.restore` | Namespace và source provider quyết định cardinality/lifecycle; không universal grant; đúng deletion cohort, parent hợp lệ; Namespace và source provider quyết định cardinality/lifecycle; không universal grant | Common + dynamic source/provider guards |
| `organization.template.purge` | Namespace và source provider quyết định cardinality/lifecycle; không universal grant; chỉ Trash, preview pins/dependencies, xác nhận không thể hoàn tác; Namespace và source provider quyết định cardinality/lifecycle; không universal grant | Common + dynamic source/provider guards |
| `organization.template.instantiate` | Target create + protected-field actions; chọn immutable Type/Editor của Documents khi tạo; không copy original owner/history; Namespace và source provider quyết định cardinality/lifecycle; không universal grant | Common + dynamic source/provider guards |
| `organization.support.read` | Qualified Admin + support.session.open + current one-module consent OR authorized SuperAdmin Emergency; target enabled; approved redacted projection only; no owner history/export/reveal/linked-module expansion; Namespace và source provider quyết định cardinality/lifecycle; không universal grant | Common + dynamic source/provider guards |
| `organization.collection.share` | Source eligible, SharingEnabled, projection chính xác; kết hợp sharing.link.*; không history/reason/secret; Namespace và source provider quyết định cardinality/lifecycle; không universal grant | `sharing.link.read` |

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
