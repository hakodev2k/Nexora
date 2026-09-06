# Settings và Application Shell

FX-09 · Feature specification · 2026-09-06 · Baseline requirements: d0d8418

**Trạng thái:** yêu cầu đã xác nhận được giữ nguyên; chi tiết bổ sung bên dưới là **Resolved (delegated)** theo DEC-GOV-001. Mục Q còn mở là proposal, chưa được duyệt. Tài liệu không cấp phép implement.

## Phạm vi và tham chiếu

Navigation personal-only, preferences, timezone, module settings và responsive shell.

[Notion Sidebar](https://www.notion.com/help/navigate-with-the-sidebar): Sidebar phục vụ điều hướng và truy cập nhanh.

[WordPress](https://wordpress.org/documentation/article/roles-and-capabilities/): Tách roles và capabilities; quyền phụ thuộc hành động.

**Áp dụng cho Nexora:** Notion tham chiếu navigation; không Workspace switcher hoặc hiển thị mọi catalog item cùng lúc.

**Màn hình:** `/settings, application sidebar/header`. Routes là thiết kế đề xuất; không phải endpoint đã implement.

## Luồng sử dụng

1. Login → Dashboard; grouped sidebar Productivity/Knowledge/Finance/Shopping/Developer/Assets/Career tùy module enabled.
2. Settings profile/security/preferences; module settings nằm trong module hoặc Settings route đã đăng ký.
3. Global search/quick capture/notification icon; mobile navigation tương đương keyboard accessible.

## Dữ liệu và validation

- Theme System/Light/Dark, timezone IANA, locale/language Q-09; preference revision.
- Module settings schema/server validation, secret fields chỉ VaultRef; route contributions permission-tagged.

## Hành vi và lifecycle

- **FX-09-BR-001:** Task Kanban default, Projects Grid default, Documents Grid default, Calendar Day default phải giữ đúng.
- **FX-09-BR-002:** Timezone change giữ timed instant, all-day dates giữ ngày; format hiển thị không thay stored number/date semantics.
- **FX-09-BR-003:** Missing/disabled route trả unavailable; menu không là authorization.
- **FX-09-BR-004:** Form explicit Save, dirty guard, field-level validation; toast không che required action; error có retry và correlationID an toàn.
- **FX-09-BR-005:** Responsive proposal360–767/768–1199/≥1200, keyboard/focus/labels/contrast theo NFR; không coi breakpoint là business question.

## Quyền, API và tích hợp

- GetPreferences/UpdatePreferences/NavigationContribution/SettingsSchema; module APIs ownsettings.
- No per-channel notification toggles; all 3 đã confirmed.

Áp dụng [hợp đồng chung](00-shared-behavior.md): owner isolation, module/action gate, concurrency, idempotency, lỗi/loading/empty, phân trang và lifecycle. Support/Emergency chỉ read-only có grant; không thừa hưởng owner mutation, secret reveal hoặc export. API cụ thể phải theo command/query này và được chốt trong solution design.

## Tiêu chí nghiệm thu

- **FX-09-AC-001:** Switch timezone không dời Task instant; Calendar Day mở đúng localday.
- **FX-09-AC-002:** Module disable remove nav/search/widgets nhưng giữ config.
- **FX-09-AC-003:** Keyboard có thể mở/create/save/cancel mà không drag bắt buộc.

Các AC nguồn và common gates vẫn bắt buộc; đây là các scenario bổ sung, không thay thế toàn bộ test specification.

## Traceability và phần còn mở

- [02-cross-cutting-requirements.md](../requirements/02-cross-cutting-requirements.md): `SET-001`, `SET-002`, `SET-003`, `SET-004`
- [04-non-functional-requirements.md](../requirements/04-non-functional-requirements.md): `A11Y-001`, `A11Y-002`, `A11Y-003`, `A11Y-004`, `A11Y-005`, `DAT-001`, `DAT-002`, `DAT-003`, `DAT-004`, `DAT-005`, `DAT-006`, `UX-001`, `UX-002`, `UX-003`, `UX-004`, `UX-005`, `UX-006`
- [phase-01-core-platform.md](../requirements/phases/phase-01-core-platform.md): `P01-PLT-005`, `P01-SHL-001`, `P01-SHL-002`, `P01-SHL-003`, `P01-SHL-004`, `P01-SHL-005`, `P01-SHL-006`, `P01-SHL-007`
- [phase-02-productivity.md](../requirements/phases/phase-02-productivity.md): `P02-TZ-001`, `P02-TZ-002`, `P02-TZ-003`

Quyết định lớn cần PO: [Q-09](90-open-decisions.md#q-09). Các hành vi phụ thuộc chúng chưa đạt Definition of Ready.
