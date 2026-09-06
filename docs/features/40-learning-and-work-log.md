# Skills, Courses, Certifications, Learning Plan và Work Log

FX-40 · Feature specification · 2026-09-06 · Baseline requirements: d0d8418

**Trạng thái:** yêu cầu đã xác nhận được giữ nguyên; chi tiết bổ sung bên dưới là **Resolved (delegated)** theo DEC-GOV-001. Mục Q còn mở là proposal, chưa được duyệt. Tài liệu không cấp phép implement.

## Phạm vi và tham chiếu

Học tập cá nhân, evidence/progress/certification expiry và nhật ký công việc.

[Moodle](https://docs.moodle.org/502/en/Activity_completion): Tiến độ học dựa trên quy tắc completion được định nghĩa.

[Toggl Track](https://support.toggl.com/en-us/article/creating-a-time-entry-wg8nug/): Time entry tạo bằng timer hoặc nhập thủ công.

**Áp dụng cho Nexora:** Moodle tham chiếu completion; Toggl tham chiếu duration. Không LMS lớp học, chấm thi/payroll hoặc claim năng lực tự động.

**Màn hình:** `/career/skills, /learning, /certifications, /work-log`. Routes là thiết kế đề xuất; không phải endpoint đã implement.

## Luồng sử dụng

1. Skill name/category/proficiency → evidence/lastUsed.
2. Course provider/URL/dates → plan targets/milestones → progress → Complete.
3. Certification issued/expiry/files → reminder/renewal.
4. Learning Plan link Goals/Tasks/Courses; WorkLog date/duration/category/project/employerlabel/notes.

## Dữ liệu và validation

- Skill scale Beginner/Intermediate/Advanced/Expert tự đánh giá, evidence refs; unique normalizedname User.
- Course Planned/InProgress/Completed/Abandoned/Archived; progress mode ManualPercent hoặc Milestones immutable sau progress đầu.
- Certification issuer/credentialID Sensitive/URL/issued/expiry optional/files.
- WorkLog date, duration>0≤24h/entry, category/ProjectRef/employertext/notes; no monetary payroll.

## Hành vi và lifecycle

- **FX-40-BR-001:** Manual progress0–100; Milestones done/total equalweight; status Complete explicit không chỉ nhìn100%.
- **FX-40-BR-002:** Plan progress đọc sourceproviders, deleted/unavailable shown, không fake done; remove link không delete source.
- **FX-40-BR-003:** Skill merge explicit preserve evidence/history; proficiency không inferred từ hours.
- **FX-40-BR-004:** Certification renewal lưu prior evidence/expiryhistory và invalidate stalealerts; public projection credentialID masked Q-03.
- **FX-40-BR-005:** WorkLog link Time Entry optional unique để tránh double import; report grossduration cảnh báo overlap, timezone local date rõ.
- **FX-40-BR-006:** Archive read-only có unarchive; Trash manual purge; no Calendar events cho Course/WorkLog trừ scope được duyệt.

## Quyền, API và tích hợp

- Skill/ CourseProgress/CertificationExpiry/LearningPlan providers; WorkLog Report.
- Files evidence và Finance cost refs read-only; Reminder/Notification all 3.

Áp dụng [hợp đồng chung](00-shared-behavior.md): owner isolation, module/action gate, concurrency, idempotency, lỗi/loading/empty, phân trang và lifecycle. Support/Emergency chỉ read-only có grant; không thừa hưởng owner mutation, secret reveal hoặc export. API cụ thể phải theo command/query này và được chốt trong solution design.

## Tiêu chí nghiệm thu

- **FX-40-AC-001:** Milestone denominator0 hiển thị No Milestones không100%.
- **FX-40-AC-002:** Course delete không xóa Task/Goal liên kết.
- **FX-40-AC-003:** Certification renew chỉ một pending reminder mới.
- **FX-40-AC-004:** Time Entry linked hai lần không cộng WorkLog duplicate.

Các AC nguồn và common gates vẫn bắt buộc; đây là các scenario bổ sung, không thay thế toàn bộ test specification.

## Traceability và phần còn mở

- [phase-07-assets-and-career.md](../requirements/phases/phase-07-assets-and-career.md): `P07-CERF-001`, `P07-CRS-001`, `P07-LRN-001`, `P07-SKL-001`, `P07-WRK-001`

Quyết định lớn cần PO: [Q-03](90-open-decisions.md#q-03). Các hành vi phụ thuộc chúng chưa đạt Definition of Ready.
