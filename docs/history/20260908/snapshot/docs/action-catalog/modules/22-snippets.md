# FX-22 — Snippets: actions

Catalog v1 · 2026-09-07 · Docs-only. New key decomposition = Resolved delegated; source business decisions giữ nguyên; Blocked rows không được kích hoạt bằng grant.

## Sources và phạm vi

- [Feature / validation / state graph](../../features/22-snippets.md) — FX-22-BR-001, FX-22-BR-002, FX-22-BR-003, FX-22-BR-004, FX-22-BR-005. Các BR này áp cho feature; không gán sai một BR duy nhất cho mọi row.
- [UX specification](../../ux-ui/modules/22-snippets.md); [exact screen bindings](../06-screen-bindings.md).
- [Authorization contract](../00-authorization-contract.md), [semantic field guards](../01-composition-and-field-guards.md), [SDK contract](../04-module-action-contract.md).
- Namespace `snippets` là stable logical key, bind installed ModuleId trong manifest. **Text-only; escape rendered code; không thực thi snippet**.

## Catalog

“All prerequisites” ở row bao gồm explicit Requires **và** source/dynamic dependencies trong guard; danh sách Requires trống không có nghĩa bỏ owner/module/lifecycle checks. Every action denies unknown fields and unapproved semantic changes. Source state matrix luôn kiểm tra ở handler, không suy quyền từ verb hoặc tên button.

| Action key / hành vi | Kind / context | Admin checkbox? | Risk (DB mapping) | Status / gate | UI entry |
| --- | --- | --- | --- | --- | --- |
| <a id="snippets-snippet-read"></a>`snippets.snippet.read` — Xem Snippet | QUERY / SELF | Yes, gated | Normal (Normal) | Resolved delegated: action contract; source business rules unchanged | FX22-S01, FX22-S03 |
| <a id="snippets-snippet-create"></a>`snippets.snippet.create` — Tạo Snippet | COMMAND / SELF | Yes, gated | Normal (Normal) | Resolved delegated: action contract; source business rules unchanged | FX22-S01, FX22-S02 |
| <a id="snippets-snippet-save"></a>`snippets.snippet.save` — Save Snippet thành version | COMMAND / SELF | Yes, gated | Normal (Normal) | Resolved delegated: action contract; source business rules unchanged | FX22-S02 |
| <a id="snippets-snippet-archive"></a>`snippets.snippet.archive` — Archive snippet | COMMAND / SELF | Yes, gated | Lifecycle (Normal) | Resolved delegated: action contract; source business rules unchanged | FX22-S01 |
| <a id="snippets-snippet-unarchive"></a>`snippets.snippet.unarchive` — Unarchive snippet | COMMAND / SELF | Yes, gated | Lifecycle (Normal) | Resolved delegated: action contract; source business rules unchanged | FX22-S01 |
| <a id="snippets-snippet-trash"></a>`snippets.snippet.trash` — Đưa snippet vào Thùng rác | COMMAND / SELF | Yes, gated | Lifecycle (Normal) | Resolved delegated: action contract; source business rules unchanged | FX22-S01 |
| <a id="snippets-snippet-restore"></a>`snippets.snippet.restore` — Khôi phục snippet từ Thùng rác | COMMAND / SELF | Yes, gated | Lifecycle (Normal) | Resolved delegated: action contract; source business rules unchanged | FX22-S01 |
| <a id="snippets-snippet-purge"></a>`snippets.snippet.purge` — Xóa vĩnh viễn snippet | COMMAND / SELF | Yes, gated | Destructive (Administrative) | Resolved delegated: action contract; source business rules unchanged | FX22-S01 |
| <a id="snippets-snippet-history"></a>`snippets.snippet.history` — Xem lịch sử snippet | QUERY / SELF | Yes, gated | Sensitive (Sensitive) | Resolved delegated: action contract; source business rules unchanged | FX22-S03 |
| <a id="snippets-snippet-restore-version"></a>`snippets.snippet.restore_version` — Khôi phục version thành bản mới | COMMAND / SELF | Yes, gated | Lifecycle (Normal) | Resolved delegated: action contract; source business rules unchanged | FX22-S03 |
| <a id="snippets-snippet-share"></a>`snippets.snippet.share` — Quản lý link chỉ-đọc của snippet | COMPOSITE / SELF | Yes, gated | Disclosure (Sensitive) | Resolved delegated: action contract; source business rules unchanged | FX22-S03 |
| <a id="snippets-snippet-copy"></a>`snippets.snippet.copy` — Copy nội dung | LOCAL / SELF | No | Disclosure (Sensitive) | Resolved delegated: action contract; source business rules unchanged | FX22-S03 |
| <a id="snippets-snippet-export"></a>`snippets.snippet.export` — Download text | COMMAND / SELF | Yes, gated | Disclosure (Sensitive) | Resolved delegated: action contract; source business rules unchanged | FX22-S03 |
| <a id="snippets-support-read"></a>`snippets.support.read` — Xem safe support projection của module | QUERY / SUPPORT | Yes, gated | Sensitive (Sensitive) | Resolved delegated: diagnostic projection only; private body excluded unless source scope explicitly permits | FX05-S03, FX05-S05 |

## Điều kiện riêng theo operation

| Action | Guard / validation / effects | Explicit dependencies bổ sung |
| --- | --- | --- |
| `snippets.snippet.read` | Text-only; escape rendered code; không thực thi snippet; Text-only; escape rendered code; không thực thi snippet | Common + dynamic source/provider guards |
| `snippets.snippet.create` | Text-only; escape rendered code; không thực thi snippet; Text-only; escape rendered code; không thực thi snippet | Common + dynamic source/provider guards |
| `snippets.snippet.save` | Own active Snippet; immutable version; concurrency check; Text-only; escape rendered code; không thực thi snippet | Common + dynamic source/provider guards |
| `snippets.snippet.archive` | Text-only; escape rendered code; không thực thi snippet; ngoài Trash, chưa Archived; Text-only; escape rendered code; không thực thi snippet | Common + dynamic source/provider guards |
| `snippets.snippet.unarchive` | Text-only; escape rendered code; không thực thi snippet; Archived, khôi phục previous state; Text-only; escape rendered code; không thực thi snippet | Common + dynamic source/provider guards |
| `snippets.snippet.trash` | Text-only; escape rendered code; không thực thi snippet; preview aggregate, không purge; Text-only; escape rendered code; không thực thi snippet | Common + dynamic source/provider guards |
| `snippets.snippet.restore` | Text-only; escape rendered code; không thực thi snippet; đúng deletion cohort, parent hợp lệ; Text-only; escape rendered code; không thực thi snippet | Common + dynamic source/provider guards |
| `snippets.snippet.purge` | Text-only; escape rendered code; không thực thi snippet; chỉ Trash, preview pins/dependencies, xác nhận không thể hoàn tác; Text-only; escape rendered code; không thực thi snippet | Common + dynamic source/provider guards |
| `snippets.snippet.history` | Owner-only history, cùng owner/module; không share/support; Text-only; escape rendered code; không thực thi snippet | Common + dynamic source/provider guards |
| `snippets.snippet.restore_version` | Text-only; escape rendered code; không thực thi snippet; editable hiện tại, giữ immutable identity/topology, tái kiểm tra quyền trên field diff; Text-only; escape rendered code; không thực thi snippet | Common + dynamic source/provider guards |
| `snippets.snippet.share` | Source eligible, SharingEnabled, projection chính xác; kết hợp sharing.link.*; không history/reason/secret; Text-only; escape rendered code; không thực thi snippet | `sharing.link.read` |
| `snippets.snippet.copy` | Source read required; không secret trong toast; Text-only; escape rendered code; không thực thi snippet | `snippets.snippet.read` |
| `snippets.snippet.export` | Own current read; text artifact only, không executable publish; Text-only; escape rendered code; không thực thi snippet | Common + dynamic source/provider guards |
| `snippets.support.read` | Qualified Admin + support.session.open + current one-module consent OR authorized SuperAdmin Emergency; target enabled; approved redacted projection only; no owner history/export/reveal/linked-module expansion; Text-only; escape rendered code; không thực thi snippet | Common + dynamic source/provider guards |

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
