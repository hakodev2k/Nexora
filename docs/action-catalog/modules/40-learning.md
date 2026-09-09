# FX-40 — Learning / Work Log: actions

Catalog v1 · 2026-09-07 · Docs-only. New key decomposition = Resolved delegated; source business decisions giữ nguyên; Blocked rows không được kích hoạt bằng grant.

## Sources và phạm vi

- [Feature / validation / state graph](../../features/40-learning-and-work-log.md) — FX-40-BR-001, FX-40-BR-002, FX-40-BR-003, FX-40-BR-004, FX-40-BR-005, FX-40-BR-006. Các BR này áp cho feature; không gán sai một BR duy nhất cho mọi row.
- [UX specification](../../ux-ui/modules/40-learning.md); [exact screen bindings](../06-screen-bindings.md).
- [Authorization contract](../00-authorization-contract.md), [semantic field guards](../01-composition-and-field-guards.md), [SDK contract](../04-module-action-contract.md).
- Namespace `learning` là stable logical key, bind installed ModuleId trong manifest. **Personal tracking; no LMS enrollment/payroll/provider write**.

## Catalog

“All prerequisites” ở row bao gồm explicit Requires **và** source/dynamic dependencies trong guard; danh sách Requires trống không có nghĩa bỏ owner/module/lifecycle checks. Every action denies unknown fields and unapproved semantic changes. Source state matrix luôn kiểm tra ở handler, không suy quyền từ verb hoặc tên button.

| Action key / hành vi | Kind / context | Admin checkbox? | Risk (DB mapping) | Status / gate | UI entry |
| --- | --- | --- | --- | --- | --- |
| <a id="learning-skill-read"></a>`learning.skill.read` — Xem Skill | QUERY / SELF | Yes, gated | Normal (Normal) | Resolved delegated: action contract; source business rules unchanged | FX40-S02 |
| <a id="learning-skill-create"></a>`learning.skill.create` — Tạo Skill | COMMAND / SELF | Yes, gated | Normal (Normal) | Resolved delegated: action contract; source business rules unchanged | FX40-S02 |
| <a id="learning-skill-update"></a>`learning.skill.update` — Sửa Skill | COMMAND / SELF | Yes, gated | Normal (Normal) | Resolved delegated: action contract; source business rules unchanged | FX40-S02 |
| <a id="learning-skill-proficiency"></a>`learning.skill.proficiency` — Sửa self-assessed proficiency | COMMAND / SELF | Yes, gated | Normal (Normal) | Resolved delegated: action contract; source business rules unchanged | FX40-S02 |
| <a id="learning-skill-evidence"></a>`learning.skill.evidence` — Gắn/gỡ evidence | COMMAND / SELF | Yes, gated | Normal (Normal) | Resolved delegated: action contract; source business rules unchanged | FX40-S02 |
| <a id="learning-skill-merge"></a>`learning.skill.merge` — Gộp skill | COMMAND / SELF | Yes, gated | Normal (Normal) | Resolved delegated: action contract; source business rules unchanged | FX40-S02 |
| <a id="learning-skill-archive"></a>`learning.skill.archive` — Archive skill | COMMAND / SELF | Yes, gated | Lifecycle (Normal) | Resolved delegated: action contract; source business rules unchanged | FX40-S02 |
| <a id="learning-skill-unarchive"></a>`learning.skill.unarchive` — Unarchive skill | COMMAND / SELF | Yes, gated | Lifecycle (Normal) | Resolved delegated: action contract; source business rules unchanged | FX40-S02 |
| <a id="learning-skill-trash"></a>`learning.skill.trash` — Đưa skill vào Thùng rác | COMMAND / SELF | Yes, gated | Lifecycle (Normal) | Resolved delegated: action contract; source business rules unchanged | FX40-S02 |
| <a id="learning-skill-restore"></a>`learning.skill.restore` — Khôi phục skill từ Thùng rác | COMMAND / SELF | Yes, gated | Lifecycle (Normal) | Resolved delegated: action contract; source business rules unchanged | FX40-S02 |
| <a id="learning-skill-purge"></a>`learning.skill.purge` — Xóa vĩnh viễn skill | COMMAND / SELF | Yes, gated | Destructive (Administrative) | Resolved delegated: action contract; source business rules unchanged | FX40-S02 |
| <a id="learning-course-read"></a>`learning.course.read` — Xem Course | QUERY / SELF | Yes, gated | Normal (Normal) | Resolved delegated: action contract; source business rules unchanged | FX40-S03 |
| <a id="learning-course-create"></a>`learning.course.create` — Tạo Course | COMMAND / SELF | Yes, gated | Normal (Normal) | Resolved delegated: action contract; source business rules unchanged | FX40-S03 |
| <a id="learning-course-update"></a>`learning.course.update` — Sửa Course | COMMAND / SELF | Yes, gated | Normal (Normal) | Resolved delegated: action contract; source business rules unchanged | FX40-S03 |
| <a id="learning-course-progress"></a>`learning.course.progress` — Cập nhật progress | COMMAND / SELF | Yes, gated | Normal (Normal) | Resolved delegated: action contract; source business rules unchanged | FX40-S03 |
| <a id="learning-course-milestone"></a>`learning.course.milestone` — Quản lý milestone | COMMAND / SELF | Yes, gated | Normal (Normal) | Resolved delegated: action contract; source business rules unchanged | FX40-S03 |
| <a id="learning-course-complete"></a>`learning.course.complete` — Hoàn thành | COMMAND / SELF | Yes, gated | Normal (Normal) | Resolved delegated: action contract; source business rules unchanged | FX40-S03 |
| <a id="learning-course-abandon"></a>`learning.course.abandon` — Dừng học | COMMAND / SELF | Yes, gated | Normal (Normal) | Resolved delegated: action contract; source business rules unchanged | FX40-S03 |
| <a id="learning-course-archive"></a>`learning.course.archive` — Archive course | COMMAND / SELF | Yes, gated | Lifecycle (Normal) | Resolved delegated: action contract; source business rules unchanged | FX40-S03 |
| <a id="learning-course-unarchive"></a>`learning.course.unarchive` — Unarchive course | COMMAND / SELF | Yes, gated | Lifecycle (Normal) | Resolved delegated: action contract; source business rules unchanged | FX40-S03 |
| <a id="learning-course-trash"></a>`learning.course.trash` — Đưa course vào Thùng rác | COMMAND / SELF | Yes, gated | Lifecycle (Normal) | Resolved delegated: action contract; source business rules unchanged | FX40-S03 |
| <a id="learning-course-restore"></a>`learning.course.restore` — Khôi phục course từ Thùng rác | COMMAND / SELF | Yes, gated | Lifecycle (Normal) | Resolved delegated: action contract; source business rules unchanged | FX40-S03 |
| <a id="learning-course-purge"></a>`learning.course.purge` — Xóa vĩnh viễn course | COMMAND / SELF | Yes, gated | Destructive (Administrative) | Resolved delegated: action contract; source business rules unchanged | FX40-S03 |
| <a id="learning-certification-read"></a>`learning.certification.read` — Xem Certification | QUERY / SELF | Yes, gated | Normal (Normal) | Resolved delegated: action contract; source business rules unchanged | FX40-S04 |
| <a id="learning-certification-create"></a>`learning.certification.create` — Tạo Certification | COMMAND / SELF | Yes, gated | Normal (Normal) | Resolved delegated: action contract; source business rules unchanged | FX40-S04 |
| <a id="learning-certification-update"></a>`learning.certification.update` — Sửa Certification | COMMAND / SELF | Yes, gated | Normal (Normal) | Resolved delegated: action contract; source business rules unchanged | FX40-S04 |
| <a id="learning-certification-renew"></a>`learning.certification.renew` — Ghi renewal | COMMAND / SELF | Yes, gated | Normal (Normal) | Resolved delegated: action contract; source business rules unchanged | FX40-S04 |
| <a id="learning-certification-evidence"></a>`learning.certification.evidence` — Gắn/gỡ evidence | COMMAND / SELF | Yes, gated | Normal (Normal) | Resolved delegated: action contract; source business rules unchanged | FX40-S04 |
| <a id="learning-certification-set-reminder"></a>`learning.certification.set_reminder` — Đặt expiry reminder | COMMAND / SELF | Yes, gated | Normal (Normal) | Resolved delegated: action contract; source business rules unchanged | FX40-S04 |
| <a id="learning-certification-share"></a>`learning.certification.share` — Quản lý link chỉ-đọc của certification | COMPOSITE / SELF | Yes, gated | Disclosure (Sensitive) | Blocked Q-03 | FX40-S04 |
| <a id="learning-plan-read"></a>`learning.plan.read` — Xem Learning Plan | QUERY / SELF | Yes, gated | Normal (Normal) | Resolved delegated: action contract; source business rules unchanged | FX40-S05 |
| <a id="learning-plan-create"></a>`learning.plan.create` — Tạo Learning Plan | COMMAND / SELF | Yes, gated | Normal (Normal) | Resolved delegated: action contract; source business rules unchanged | FX40-S05 |
| <a id="learning-plan-update"></a>`learning.plan.update` — Sửa Learning Plan | COMMAND / SELF | Yes, gated | Normal (Normal) | Resolved delegated: action contract; source business rules unchanged | FX40-S05 |
| <a id="learning-plan-link"></a>`learning.plan.link` — Liên kết skill/course | COMMAND / SELF | Yes, gated | Normal (Normal) | Resolved delegated: action contract; source business rules unchanged | FX40-S05 |
| <a id="learning-plan-unlink"></a>`learning.plan.unlink` — Gỡ reference | COMMAND / SELF | Yes, gated | Normal (Normal) | Resolved delegated: action contract; source business rules unchanged | FX40-S05 |
| <a id="learning-plan-reorder"></a>`learning.plan.reorder` — Sắp xếp plan | COMMAND / SELF | Yes, gated | Normal (Normal) | Resolved delegated: action contract; source business rules unchanged | FX40-S05 |
| <a id="learning-plan-complete"></a>`learning.plan.complete` — Hoàn thành plan | COMMAND / SELF | Yes, gated | Normal (Normal) | Resolved delegated: action contract; source business rules unchanged | FX40-S05 |
| <a id="learning-plan-archive"></a>`learning.plan.archive` — Archive plan | COMMAND / SELF | Yes, gated | Lifecycle (Normal) | Resolved delegated: action contract; source business rules unchanged | FX40-S05 |
| <a id="learning-plan-unarchive"></a>`learning.plan.unarchive` — Unarchive plan | COMMAND / SELF | Yes, gated | Lifecycle (Normal) | Resolved delegated: action contract; source business rules unchanged | FX40-S05 |
| <a id="learning-plan-trash"></a>`learning.plan.trash` — Đưa plan vào Thùng rác | COMMAND / SELF | Yes, gated | Lifecycle (Normal) | Resolved delegated: action contract; source business rules unchanged | FX40-S05 |
| <a id="learning-plan-restore"></a>`learning.plan.restore` — Khôi phục plan từ Thùng rác | COMMAND / SELF | Yes, gated | Lifecycle (Normal) | Resolved delegated: action contract; source business rules unchanged | FX40-S05 |
| <a id="learning-plan-purge"></a>`learning.plan.purge` — Xóa vĩnh viễn plan | COMMAND / SELF | Yes, gated | Destructive (Administrative) | Resolved delegated: action contract; source business rules unchanged | FX40-S05 |
| <a id="learning-worklog-read"></a>`learning.worklog.read` — Xem Work Log | QUERY / SELF | Yes, gated | Normal (Normal) | Resolved delegated: action contract; source business rules unchanged | FX40-S06 |
| <a id="learning-worklog-create"></a>`learning.worklog.create` — Tạo Work Log | COMMAND / SELF | Yes, gated | Normal (Normal) | Resolved delegated: action contract; source business rules unchanged | FX40-S06 |
| <a id="learning-worklog-update"></a>`learning.worklog.update` — Sửa Work Log | COMMAND / SELF | Yes, gated | Normal (Normal) | Resolved delegated: action contract; source business rules unchanged | FX40-S06 |
| <a id="learning-worklog-link-time"></a>`learning.worklog.link_time` — Liên kết Time Entry | COMMAND / SELF | Yes, gated | Normal (Normal) | Resolved delegated: action contract; source business rules unchanged | FX40-S06 |
| <a id="learning-worklog-archive"></a>`learning.worklog.archive` — Archive worklog | COMMAND / SELF | Yes, gated | Lifecycle (Normal) | Resolved delegated: action contract; source business rules unchanged | FX40-S06 |
| <a id="learning-worklog-unarchive"></a>`learning.worklog.unarchive` — Unarchive worklog | COMMAND / SELF | Yes, gated | Lifecycle (Normal) | Resolved delegated: action contract; source business rules unchanged | FX40-S06 |
| <a id="learning-worklog-trash"></a>`learning.worklog.trash` — Đưa worklog vào Thùng rác | COMMAND / SELF | Yes, gated | Lifecycle (Normal) | Resolved delegated: action contract; source business rules unchanged | FX40-S06 |
| <a id="learning-worklog-restore"></a>`learning.worklog.restore` — Khôi phục worklog từ Thùng rác | COMMAND / SELF | Yes, gated | Lifecycle (Normal) | Resolved delegated: action contract; source business rules unchanged | FX40-S06 |
| <a id="learning-worklog-purge"></a>`learning.worklog.purge` — Xóa vĩnh viễn worklog | COMMAND / SELF | Yes, gated | Destructive (Administrative) | Resolved delegated: action contract; source business rules unchanged | FX40-S06 |
| <a id="learning-report-read"></a>`learning.report.read` — Xem learning/worklog summaries | QUERY / SELF | Yes, gated | Normal (Normal) | Resolved delegated: action contract; source business rules unchanged | FX40-S01 |
| <a id="learning-support-read"></a>`learning.support.read` — Xem safe support projection của module | QUERY / SUPPORT | Yes, gated | Sensitive (Sensitive) | Blocked Q-03; domain gates also apply | FX05-S03, FX05-S05 |
| <a id="learning-certification-archive"></a>`learning.certification.archive` — Archive certification | COMMAND / SELF | Yes, gated | Lifecycle (Normal) | Resolved delegated: action contract; source business rules unchanged | FX40-S04 |
| <a id="learning-certification-unarchive"></a>`learning.certification.unarchive` — Unarchive certification | COMMAND / SELF | Yes, gated | Lifecycle (Normal) | Resolved delegated: action contract; source business rules unchanged | FX40-S04 |
| <a id="learning-certification-trash"></a>`learning.certification.trash` — Đưa certification vào Thùng rác | COMMAND / SELF | Yes, gated | Lifecycle (Normal) | Resolved delegated: action contract; source business rules unchanged | FX40-S04 |
| <a id="learning-certification-restore"></a>`learning.certification.restore` — Khôi phục certification từ Thùng rác | COMMAND / SELF | Yes, gated | Lifecycle (Normal) | Resolved delegated: action contract; source business rules unchanged | FX40-S04 |
| <a id="learning-certification-purge"></a>`learning.certification.purge` — Xóa vĩnh viễn certification | COMMAND / SELF | Yes, gated | Destructive (Administrative) | Resolved delegated: action contract; source business rules unchanged | FX40-S04 |

## Điều kiện riêng theo operation

| Action | Guard / validation / effects | Explicit dependencies bổ sung |
| --- | --- | --- |
| `learning.skill.read` | Personal tracking; no LMS enrollment/payroll/provider write; Personal tracking; no LMS enrollment/payroll/provider write | Common + dynamic source/provider guards |
| `learning.skill.create` | Personal tracking; no LMS enrollment/payroll/provider write; Personal tracking; no LMS enrollment/payroll/provider write | Common + dynamic source/provider guards |
| `learning.skill.update` | Personal tracking; no LMS enrollment/payroll/provider write; Personal tracking; no LMS enrollment/payroll/provider write | `learning.skill.read` |
| `learning.skill.proficiency` | Owner evidence/source access; no auto certification claim; Personal tracking; no LMS enrollment/payroll/provider write | Common + dynamic source/provider guards |
| `learning.skill.evidence` | Owner evidence/source access; no auto certification claim; Personal tracking; no LMS enrollment/payroll/provider write | Common + dynamic source/provider guards |
| `learning.skill.merge` | Owner evidence/source access; no auto certification claim; Personal tracking; no LMS enrollment/payroll/provider write | Common + dynamic source/provider guards |
| `learning.skill.archive` | Personal tracking; no LMS enrollment/payroll/provider write; ngoài Trash, chưa Archived; Personal tracking; no LMS enrollment/payroll/provider write | Common + dynamic source/provider guards |
| `learning.skill.unarchive` | Personal tracking; no LMS enrollment/payroll/provider write; Archived, khôi phục previous state; Personal tracking; no LMS enrollment/payroll/provider write | Common + dynamic source/provider guards |
| `learning.skill.trash` | Personal tracking; no LMS enrollment/payroll/provider write; preview aggregate, không purge; Personal tracking; no LMS enrollment/payroll/provider write | Common + dynamic source/provider guards |
| `learning.skill.restore` | Personal tracking; no LMS enrollment/payroll/provider write; đúng deletion cohort, parent hợp lệ; Personal tracking; no LMS enrollment/payroll/provider write | Common + dynamic source/provider guards |
| `learning.skill.purge` | Personal tracking; no LMS enrollment/payroll/provider write; chỉ Trash, preview pins/dependencies, xác nhận không thể hoàn tác; Personal tracking; no LMS enrollment/payroll/provider write | Common + dynamic source/provider guards |
| `learning.course.read` | Personal tracking; no LMS enrollment/payroll/provider write; Personal tracking; no LMS enrollment/payroll/provider write | Common + dynamic source/provider guards |
| `learning.course.create` | Personal tracking; no LMS enrollment/payroll/provider write; Personal tracking; no LMS enrollment/payroll/provider write | Common + dynamic source/provider guards |
| `learning.course.update` | Personal tracking; no LMS enrollment/payroll/provider write; Personal tracking; no LMS enrollment/payroll/provider write | `learning.course.read` |
| `learning.course.progress` | FX-40 progress/explicit state; no auto award credential; Personal tracking; no LMS enrollment/payroll/provider write | Common + dynamic source/provider guards |
| `learning.course.milestone` | FX-40 progress/explicit state; no auto award credential; Personal tracking; no LMS enrollment/payroll/provider write | Common + dynamic source/provider guards |
| `learning.course.complete` | FX-40 progress/explicit state; no auto award credential; Personal tracking; no LMS enrollment/payroll/provider write | Common + dynamic source/provider guards |
| `learning.course.abandon` | FX-40 progress/explicit state; no auto award credential; Personal tracking; no LMS enrollment/payroll/provider write | Common + dynamic source/provider guards |
| `learning.course.archive` | Personal tracking; no LMS enrollment/payroll/provider write; ngoài Trash, chưa Archived; Personal tracking; no LMS enrollment/payroll/provider write | Common + dynamic source/provider guards |
| `learning.course.unarchive` | Personal tracking; no LMS enrollment/payroll/provider write; Archived, khôi phục previous state; Personal tracking; no LMS enrollment/payroll/provider write | Common + dynamic source/provider guards |
| `learning.course.trash` | Personal tracking; no LMS enrollment/payroll/provider write; preview aggregate, không purge; Personal tracking; no LMS enrollment/payroll/provider write | Common + dynamic source/provider guards |
| `learning.course.restore` | Personal tracking; no LMS enrollment/payroll/provider write; đúng deletion cohort, parent hợp lệ; Personal tracking; no LMS enrollment/payroll/provider write | Common + dynamic source/provider guards |
| `learning.course.purge` | Personal tracking; no LMS enrollment/payroll/provider write; chỉ Trash, preview pins/dependencies, xác nhận không thể hoàn tác; Personal tracking; no LMS enrollment/payroll/provider write | Common + dynamic source/provider guards |
| `learning.certification.read` | Personal tracking; no LMS enrollment/payroll/provider write; Personal tracking; no LMS enrollment/payroll/provider write | Common + dynamic source/provider guards |
| `learning.certification.create` | Personal tracking; no LMS enrollment/payroll/provider write; Personal tracking; no LMS enrollment/payroll/provider write | Common + dynamic source/provider guards |
| `learning.certification.update` | Personal tracking; no LMS enrollment/payroll/provider write; Personal tracking; no LMS enrollment/payroll/provider write | `learning.certification.read` |
| `learning.certification.renew` | Own certification/version; Clean evidence; no provider-issued credential; Personal tracking; no LMS enrollment/payroll/provider write | Common + dynamic source/provider guards |
| `learning.certification.evidence` | Own certification/version; Clean evidence; no provider-issued credential; Personal tracking; no LMS enrollment/payroll/provider write | Common + dynamic source/provider guards |
| `learning.certification.set_reminder` | Own certification/version; Clean evidence; no provider-issued credential; Personal tracking; no LMS enrollment/payroll/provider write | Common + dynamic source/provider guards |
| `learning.certification.share` | Source eligible, SharingEnabled, projection chính xác; kết hợp sharing.link.*; không history/reason/secret; Personal tracking; no LMS enrollment/payroll/provider write | `sharing.link.read` |
| `learning.plan.read` | Personal tracking; no LMS enrollment/payroll/provider write; Personal tracking; no LMS enrollment/payroll/provider write | Common + dynamic source/provider guards |
| `learning.plan.create` | Personal tracking; no LMS enrollment/payroll/provider write; Personal tracking; no LMS enrollment/payroll/provider write | Common + dynamic source/provider guards |
| `learning.plan.update` | Personal tracking; no LMS enrollment/payroll/provider write; Personal tracking; no LMS enrollment/payroll/provider write | `learning.plan.read` |
| `learning.plan.link` | Own source refs/read; no source status mutation; Personal tracking; no LMS enrollment/payroll/provider write | Common + dynamic source/provider guards |
| `learning.plan.unlink` | Own source refs/read; no source status mutation; Personal tracking; no LMS enrollment/payroll/provider write | Common + dynamic source/provider guards |
| `learning.plan.reorder` | Own source refs/read; no source status mutation; Personal tracking; no LMS enrollment/payroll/provider write | Common + dynamic source/provider guards |
| `learning.plan.complete` | Own source refs/read; no source status mutation; Personal tracking; no LMS enrollment/payroll/provider write | Common + dynamic source/provider guards |
| `learning.plan.archive` | Personal tracking; no LMS enrollment/payroll/provider write; ngoài Trash, chưa Archived; Personal tracking; no LMS enrollment/payroll/provider write | Common + dynamic source/provider guards |
| `learning.plan.unarchive` | Personal tracking; no LMS enrollment/payroll/provider write; Archived, khôi phục previous state; Personal tracking; no LMS enrollment/payroll/provider write | Common + dynamic source/provider guards |
| `learning.plan.trash` | Personal tracking; no LMS enrollment/payroll/provider write; preview aggregate, không purge; Personal tracking; no LMS enrollment/payroll/provider write | Common + dynamic source/provider guards |
| `learning.plan.restore` | Personal tracking; no LMS enrollment/payroll/provider write; đúng deletion cohort, parent hợp lệ; Personal tracking; no LMS enrollment/payroll/provider write | Common + dynamic source/provider guards |
| `learning.plan.purge` | Personal tracking; no LMS enrollment/payroll/provider write; chỉ Trash, preview pins/dependencies, xác nhận không thể hoàn tác; Personal tracking; no LMS enrollment/payroll/provider write | Common + dynamic source/provider guards |
| `learning.worklog.read` | Personal tracking; no LMS enrollment/payroll/provider write; Personal tracking; no LMS enrollment/payroll/provider write | Common + dynamic source/provider guards |
| `learning.worklog.create` | Personal tracking; no LMS enrollment/payroll/provider write; Personal tracking; no LMS enrollment/payroll/provider write | Common + dynamic source/provider guards |
| `learning.worklog.update` | Personal tracking; no LMS enrollment/payroll/provider write; Personal tracking; no LMS enrollment/payroll/provider write | `learning.worklog.read` |
| `learning.worklog.link_time` | Source time.entry.read, no double creation/payroll; Personal tracking; no LMS enrollment/payroll/provider write | Common + dynamic source/provider guards |
| `learning.worklog.archive` | Personal tracking; no LMS enrollment/payroll/provider write; ngoài Trash, chưa Archived; Personal tracking; no LMS enrollment/payroll/provider write | Common + dynamic source/provider guards |
| `learning.worklog.unarchive` | Personal tracking; no LMS enrollment/payroll/provider write; Archived, khôi phục previous state; Personal tracking; no LMS enrollment/payroll/provider write | Common + dynamic source/provider guards |
| `learning.worklog.trash` | Personal tracking; no LMS enrollment/payroll/provider write; preview aggregate, không purge; Personal tracking; no LMS enrollment/payroll/provider write | Common + dynamic source/provider guards |
| `learning.worklog.restore` | Personal tracking; no LMS enrollment/payroll/provider write; đúng deletion cohort, parent hợp lệ; Personal tracking; no LMS enrollment/payroll/provider write | Common + dynamic source/provider guards |
| `learning.worklog.purge` | Personal tracking; no LMS enrollment/payroll/provider write; chỉ Trash, preview pins/dependencies, xác nhận không thể hoàn tác; Personal tracking; no LMS enrollment/payroll/provider write | Common + dynamic source/provider guards |
| `learning.report.read` | Personal tracking; no LMS enrollment/payroll/provider write; Personal tracking; no LMS enrollment/payroll/provider write | Common + dynamic source/provider guards |
| `learning.support.read` | Qualified Admin + support.session.open + current one-module consent OR authorized SuperAdmin Emergency; target enabled; approved redacted projection only; no owner history/export/reveal/linked-module expansion; Personal tracking; no LMS enrollment/payroll/provider write | Common + dynamic source/provider guards |
| `learning.certification.archive` | Personal tracking; no LMS enrollment/payroll/provider write; ngoài Trash, chưa Archived; Personal tracking; no LMS enrollment/payroll/provider write | Common + dynamic source/provider guards |
| `learning.certification.unarchive` | Personal tracking; no LMS enrollment/payroll/provider write; Archived, khôi phục previous state; Personal tracking; no LMS enrollment/payroll/provider write | Common + dynamic source/provider guards |
| `learning.certification.trash` | Personal tracking; no LMS enrollment/payroll/provider write; preview aggregate, không purge; Personal tracking; no LMS enrollment/payroll/provider write | Common + dynamic source/provider guards |
| `learning.certification.restore` | Personal tracking; no LMS enrollment/payroll/provider write; đúng deletion cohort, parent hợp lệ; Personal tracking; no LMS enrollment/payroll/provider write | Common + dynamic source/provider guards |
| `learning.certification.purge` | Personal tracking; no LMS enrollment/payroll/provider write; chỉ Trash, preview pins/dependencies, xác nhận không thể hoàn tác; Personal tracking; no LMS enrollment/payroll/provider write | Common + dynamic source/provider guards |

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
