# FX-07 — Files / Attachments: actions

Catalog v1 · 2026-09-07 · Docs-only. New key decomposition = Resolved delegated; source business decisions giữ nguyên; Blocked rows không được kích hoạt bằng grant.

## Sources và phạm vi

- [Feature / validation / state graph](../../features/07-files-and-attachments.md) — FX-07-BR-001, FX-07-BR-002, FX-07-BR-003, FX-07-BR-004, FX-07-BR-005, FX-07-BR-006. Các BR này áp cho feature; không gán sai một BR duy nhất cho mọi row.
- [UX specification](../../ux-ui/modules/07-files.md); [exact screen bindings](../06-screen-bindings.md).
- [Authorization contract](../00-authorization-contract.md), [semantic field guards](../01-composition-and-field-guards.md), [SDK contract](../04-module-action-contract.md).
- Namespace `files` là stable logical key, bind installed ModuleId trong manifest. **Own FileObject; Clean trước attach/preview/download; pins/current history được bảo vệ**.

## Catalog

“All prerequisites” ở row bao gồm explicit Requires **và** source/dynamic dependencies trong guard; danh sách Requires trống không có nghĩa bỏ owner/module/lifecycle checks. Every action denies unknown fields and unapproved semantic changes. Source state matrix luôn kiểm tra ở handler, không suy quyền từ verb hoặc tên button.

| Action key / hành vi | Kind / context | Admin checkbox? | Risk (DB mapping) | Status / gate | UI entry |
| --- | --- | --- | --- | --- | --- |
| <a id="files-file-read"></a>`files.file.read` — Xem file metadata | QUERY / SELF | Yes, gated | Normal (Normal) | Resolved delegated: action contract; source business rules unchanged | FX07-S01 |
| <a id="files-file-upload"></a>`files.file.upload` — Khởi tạo/finalize upload | COMMAND / SELF | Yes, gated | Normal (Normal) | Resolved delegated: action contract; source business rules unchanged | FX07-S02 |
| <a id="files-file-cancel-upload"></a>`files.file.cancel_upload` — Hủy staging upload | COMMAND / SELF | Yes, gated | Normal (Normal) | Resolved delegated: action contract; source business rules unchanged | FX07-S02 |
| <a id="files-file-rename"></a>`files.file.rename` — Đổi tên file | COMMAND / SELF | Yes, gated | Normal (Normal) | Resolved delegated: action contract; source business rules unchanged | FX07-S01 |
| <a id="files-file-replace"></a>`files.file.replace` — Tạo binary thay thế | COMMAND / SELF | Yes, gated | Normal (Normal) | Resolved delegated: action contract; source business rules unchanged | FX07-S05 |
| <a id="files-file-preview"></a>`files.file.preview` — Preview nội dung file | QUERY / SELF | Yes, gated | Disclosure (Sensitive) | Resolved delegated: action contract; source business rules unchanged | FX07-S03 |
| <a id="files-file-download"></a>`files.file.download` — Download file | QUERY / SELF | Yes, gated | Disclosure (Sensitive) | Resolved delegated: action contract; source business rules unchanged | FX07-S03 |
| <a id="files-reference-attach"></a>`files.reference.attach` — Gắn file vào source | COMPOSITE / SELF | Yes, gated | Normal (Normal) | Resolved delegated: action contract; source business rules unchanged | FX07-S04 |
| <a id="files-reference-detach"></a>`files.reference.detach` — Gỡ file khỏi source | COMPOSITE / SELF | Yes, gated | Normal (Normal) | Resolved delegated: action contract; source business rules unchanged | FX07-S05 |
| <a id="files-file-trash"></a>`files.file.trash` — Đưa file vào Thùng rác | COMMAND / SELF | Yes, gated | Lifecycle (Normal) | Resolved delegated: action contract; source business rules unchanged | FX07-S01 |
| <a id="files-file-restore"></a>`files.file.restore` — Khôi phục file từ Thùng rác | COMMAND / SELF | Yes, gated | Lifecycle (Normal) | Resolved delegated: action contract; source business rules unchanged | FX07-S01 |
| <a id="files-file-purge"></a>`files.file.purge` — Xóa vĩnh viễn file | COMMAND / SELF | Yes, gated | Destructive (Administrative) | Resolved delegated: action contract; source business rules unchanged | FX07-S01 |
| <a id="files-scan-complete"></a>`files.scan.complete` — Nhận kết quả scan | WORKER / SYSTEM | No | Security (Administrative) | Resolved delegated: action contract; source business rules unchanged | Trusted worker/deployment; no user control |
| <a id="files-support-read"></a>`files.support.read` — Xem safe support projection của module | QUERY / SUPPORT | Yes, gated | Sensitive (Sensitive) | Resolved delegated: diagnostic projection only; private body excluded unless source scope explicitly permits | FX05-S03, FX05-S05 |

## Điều kiện riêng theo operation

| Action | Guard / validation / effects | Explicit dependencies bổ sung |
| --- | --- | --- |
| `files.file.read` | Own FileObject; Clean trước attach/preview/download; pins/current history được bảo vệ; Own FileObject; Clean trước attach/preview/download; pins/current history được bảo vệ | Common + dynamic source/provider guards |
| `files.file.upload` | Own FileObject; Clean trước attach/preview/download; pins/current history được bảo vệ; replacement không overwrite binary cũ; Own FileObject; Clean trước attach/preview/download; pins/current history được bảo vệ | Common + dynamic source/provider guards |
| `files.file.cancel_upload` | Own FileObject; Clean trước attach/preview/download; pins/current history được bảo vệ; replacement không overwrite binary cũ; Own FileObject; Clean trước attach/preview/download; pins/current history được bảo vệ | Common + dynamic source/provider guards |
| `files.file.rename` | Own FileObject; Clean trước attach/preview/download; pins/current history được bảo vệ; replacement không overwrite binary cũ; Own FileObject; Clean trước attach/preview/download; pins/current history được bảo vệ | Common + dynamic source/provider guards |
| `files.file.replace` | Own FileObject; Clean trước attach/preview/download; pins/current history được bảo vệ; replacement không overwrite binary cũ; Own FileObject; Clean trước attach/preview/download; pins/current history được bảo vệ | Common + dynamic source/provider guards |
| `files.file.preview` | Own FileObject; Clean trước attach/preview/download; pins/current history được bảo vệ; owning source projection/lifecycle và credential riêng; không ambient Support; Own FileObject; Clean trước attach/preview/download; pins/current history được bảo vệ | Common + dynamic source/provider guards |
| `files.file.download` | Own FileObject; Clean trước attach/preview/download; pins/current history được bảo vệ; owning source projection/lifecycle và credential riêng; không ambient Support; Own FileObject; Clean trước attach/preview/download; pins/current history được bảo vệ | Common + dynamic source/provider guards |
| `files.reference.attach` | Source write action + FileReference/version atomic; historical pins còn nguyên; Own FileObject; Clean trước attach/preview/download; pins/current history được bảo vệ | Common + dynamic source/provider guards |
| `files.reference.detach` | Source write action + FileReference/version atomic; historical pins còn nguyên; Own FileObject; Clean trước attach/preview/download; pins/current history được bảo vệ | Common + dynamic source/provider guards |
| `files.file.trash` | Own FileObject; Clean trước attach/preview/download; pins/current history được bảo vệ; preview aggregate, không purge; Own FileObject; Clean trước attach/preview/download; pins/current history được bảo vệ | Common + dynamic source/provider guards |
| `files.file.restore` | Own FileObject; Clean trước attach/preview/download; pins/current history được bảo vệ; đúng deletion cohort, parent hợp lệ; Own FileObject; Clean trước attach/preview/download; pins/current history được bảo vệ | Common + dynamic source/provider guards |
| `files.file.purge` | Own FileObject; Clean trước attach/preview/download; pins/current history được bảo vệ; chỉ Trash, preview pins/dependencies, xác nhận không thể hoàn tác; Own FileObject; Clean trước attach/preview/download; pins/current history được bảo vệ | Common + dynamic source/provider guards |
| `files.scan.complete` | Authenticated scanner job, checksum/object match; không endpoint User set Clean; Own FileObject; Clean trước attach/preview/download; pins/current history được bảo vệ | Common + dynamic source/provider guards |
| `files.support.read` | Qualified Admin + support.session.open + current one-module consent OR authorized SuperAdmin Emergency; target enabled; approved redacted projection only; no owner history/export/reveal/linked-module expansion; Own FileObject; Clean trước attach/preview/download; pins/current history được bảo vệ | Common + dynamic source/provider guards |

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
