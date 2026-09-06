# Module Platform và Module Manager

FX-03 · Feature specification · 2026-09-06 · Baseline requirements: d0d8418

**Trạng thái:** yêu cầu đã xác nhận được giữ nguyên; chi tiết bổ sung bên dưới là **Resolved (delegated)** theo DEC-GOV-001. Mục Q còn mở là proposal, chưa được duyệt. Tài liệu không cấp phép implement.

## Phạm vi và tham chiếu

Developer-defined module registry, lifecycle, contributions và enablement.

[WordPress](https://wordpress.org/documentation/article/manage-plugins/): Quản lý activation/deactivation bằng danh sách chức năng.

**Áp dụng cho Nexora:** Tham chiếu activation UI, không upload/cài executable plugin bởi User/Admin; disable không mất dữ liệu.

**Màn hình:** `/admin/modules, /admin/users/:id/modules`. Routes là thiết kế đề xuất; không phải endpoint đã implement.

## Luồng sử dụng

1. Developer deploy version → registry validate manifest/dependencies/migrations → installed.
2. SuperAdmin enable system/perUser/Admin action; registration defaults bật tất cả hiện tại, có màn hình cấu hình policy cho account mới.
3. Disable preview dependent features/jobs → apply; re-enable phục hồi dữ liệu/config hợp lệ.

## Dữ liệu và validation

- Manifest stable moduleId/semver/minCoreVersion/dependencies, routes/permissions/resource schemas/migrations/contributions.
- Contributions: Search/Dashboard/QuickCapture/Notifications/Events/Actions/Jobs/Settings/Files/Sharing với schemaVersion.
- States Installed/Enabled/Disabled/UpgradePending/Failed; account enablement độc lập system.

## Hành vi và lifecycle

- **FX-03-BR-001:** Module không đọc table module khác; application contracts/events là boundary.
- **FX-03-BR-002:** Disable chặn UI/API/search/widget/job side effects; history/data retained. Running work dừng tại checkpoint an toàn.
- **FX-03-BR-003:** Dependent module thiếu required dependency không enable; optional contribution degraded có thông báo.
- **FX-03-BR-004:** Migration versioned, preflight backup/rollback compatibility; uninstall/data purge là thao tác riêng không ngầm đi kèm disable.
- **FX-03-BR-005:** All catalog committed R1; P1 label cũ là thứ tự refinement, không tự loại khỏi release.

## Quyền, API và tích hợp

- ModuleManifest/ResourceRegistry/ContributionRegistry/ModulePolicy/UpgradePreflight; architecture ADR xác định interfaces cụ thể.
- Permissions check installed→system→user→action→resource; no Workspace gate.

Áp dụng [hợp đồng chung](00-shared-behavior.md): owner isolation, module/action gate, concurrency, idempotency, lỗi/loading/empty, phân trang và lifecycle. Support/Emergency chỉ read-only có grant; không thừa hưởng owner mutation, secret reveal hoặc export. API cụ thể phải theo command/query này và được chốt trong solution design.

## Tiêu chí nghiệm thu

- **FX-03-AC-001:** Disabled module direct API không trả dữ liệu dù menu hidden.
- **FX-03-AC-002:** Enable lại không tạo duplicate records/reminders.
- **FX-03-AC-003:** Dependency/migration failure không đánh dấu upgrade thành công.

Các AC nguồn và common gates vẫn bắt buộc; đây là các scenario bổ sung, không thay thế toàn bộ test specification.

## Traceability và phần còn mở

- [07-module-platform.md](../requirements/07-module-platform.md): `MOD-001`, `MOD-002`, `MOD-003`, `MOD-004`, `MOD-ENA-001`, `MOD-ENA-002`, `MOD-ENA-003`, `MOD-ENA-004`, `MOD-ENA-005`, `MOD-ENA-006`, `MOD-ISO-001`, `MOD-ISO-002`, `MOD-ISO-003`, `MOD-ISO-004`, `MOD-ISO-005`, `MOD-LCY-001`, `MOD-LCY-002`, `MOD-LCY-003`, `MOD-LCY-004`, `MOD-LCY-005`, `MOD-LCY-006`, `MOD-LCY-007`, `MOD-MAN-001`, `MOD-MAN-002`, `MOD-MAN-003`, `MOD-MAN-004`, `MOD-MAN-005`, `MOD-MAN-006`, `MOD-MIG-001`, `MOD-MIG-002`, `MOD-MIG-003`, `MOD-MIG-004`, `MOD-MIG-005`, `MOD-SEC-001`, `MOD-SEC-002`, `MOD-SEC-003`, `MOD-SEC-004`, `MOD-SEC-005`
- [phase-01-core-platform.md](../requirements/phases/phase-01-core-platform.md): `P01-MOD-001`, `P01-MOD-002`, `P01-MOD-003`, `P01-MOD-004`, `P01-MOD-005`

Không phát sinh câu hỏi nghiệp vụ lớn riêng cho feature này. Các gate chung về security, capacity và solution design vẫn áp dụng.
