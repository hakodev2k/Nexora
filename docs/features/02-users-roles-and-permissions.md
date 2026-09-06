# Users, Roles và Action Permissions

FX-02 · Feature specification · 2026-09-06 · Baseline requirements: d0d8418

**Trạng thái:** yêu cầu đã xác nhận được giữ nguyên; chi tiết bổ sung bên dưới là **Resolved (delegated)** theo DEC-GOV-001. Mục Q còn mở là proposal, chưa được duyệt. Tài liệu không cấp phép implement.

## Phạm vi và tham chiếu

System roles SuperAdmin/Admin/User; permission matrix và per-user access.

[WordPress](https://wordpress.org/documentation/article/roles-and-capabilities/): Tách roles và capabilities; quyền phụ thuộc hành động.

**Áp dụng cho Nexora:** Không Workspace role, membership hoặc ambient access vào private data.

**Màn hình:** `/admin/users, /admin/roles`. Routes là thiết kế đề xuất; không phải endpoint đã implement.

## Luồng sử dụng

1. SuperAdmin xem User list/search/status → detail account operational metadata.
2. Gán Admin role/module/action grants → xem diff → Save audited.
3. Disable account/revoke role → immediate access reevaluation; re-enable không tự khôi phục session đã revoke.

## Dữ liệu và validation

- Role identity, permission keys module.resource.action; grant allow/deny có version.
- User status/email verified/module entitlements; list không chứa Task/Document/Vault data.

## Hành vi và lifecycle

- **FX-02-BR-001:** Default deny; explicit deny thắng allow; action cần cả installed+enabled module và permission hợp lệ.
- **FX-02-BR-002:** Chặn xóa/disable/demote SuperAdmin cuối cùng còn hoạt động.
- **FX-02-BR-003:** SuperAdmin điều khiển module cho User và module/actions của Admin; Admin không tự nâng quyền.
- **FX-02-BR-004:** Support permission chỉ là điều kiện cần; phải có consent grant module còn hiệu lực. Emergency theo feature05.
- **FX-02-BR-005:** Bulk change có preview affected accounts; partial failures báo từng account, không fake toàn bộ thành công.

## Quyền, API và tích hợp

- EvaluateAccess/GetEffectivePermissions/GrantRole/SetActionGrant/DisableUser; authorization server-side.
- Invalidate permission cache theo version; background jobs recheck current permission trước side effect.

Áp dụng [hợp đồng chung](00-shared-behavior.md): owner isolation, module/action gate, concurrency, idempotency, lỗi/loading/empty, phân trang và lifecycle. Support/Emergency chỉ read-only có grant; không thừa hưởng owner mutation, secret reveal hoặc export. API cụ thể phải theo command/query này và được chốt trong solution design.

## Tiêu chí nghiệm thu

- **FX-02-AC-001:** Admin view permission không execute job hoặc export dữ liệu.
- **FX-02-AC-002:** Đổi role ở tab A khiến request tab B dùng quyền cũ bị chặn.
- **FX-02-AC-003:** Hai lệnh đồng thời không làm mất SuperAdmin cuối cùng.

Các AC nguồn và common gates vẫn bắt buộc; đây là các scenario bổ sung, không thay thế toàn bộ test specification.

## Traceability và phần còn mở

- [02-cross-cutting-requirements.md](../requirements/02-cross-cutting-requirements.md): `OWN-001`, `OWN-002`, `OWN-003`, `OWN-004`, `OWN-005`, `OWN-006`, `OWN-007`
- [03-security-and-privacy.md](../requirements/03-security-and-privacy.md): `AZ-001`, `AZ-002`, `AZ-003`, `AZ-004`, `AZ-005`, `AZ-006`, `AZ-007`, `AZ-008`, `AZ-009`, `AZ-010`
- [phase-01-core-platform.md](../requirements/phases/phase-01-core-platform.md): `P01-OWN-001`, `P01-OWN-002`, `P01-OWN-003`, `P01-RBAC-001`, `P01-RBAC-002`, `P01-RBAC-003`, `P01-RBAC-004`, `P01-RBAC-005`, `P01-RBAC-006`, `P01-RBAC-007`, `P01-RBAC-008`

Không phát sinh câu hỏi nghiệp vụ lớn riêng cho feature này. Các gate chung về security, capacity và solution design vẫn áp dụng.
