# Career, Companies, Calendar links và Resumes

> Current specification · reconciled 2026-09-08 · Docs-only. [Previous version](../history/20260908/snapshot/docs/features/39-career-and-resumes.md) is historical evidence, not implementation input.

FX-39 · Feature specification · 2026-09-06 · Baseline requirements: d0d8418

**Trạng thái:** yêu cầu đã xác nhận được giữ nguyên; chi tiết bổ sung bên dưới là **Resolved (delegated)** theo DEC-GOV-001. Mục Q còn mở là proposal, chưa được duyệt. Tài liệu không cấp phép implement.

## Phạm vi và tham chiếu

Job opportunities, pipeline, companies/contact notes, internal Calendar Event links và exact resume versions.

[Teal Job Tracker](https://www.tealhq.com/tools/job-tracker): Theo dõi job applications qua pipeline và thông tin từng cơ hội.

**Áp dụng cho Nexora:** Teal tham chiếu job tracker; không AI resume generation, job scraping, outreach hoặc employer/team account.

**Màn hình:** `/career/jobs, /companies, /career/jobs/:jobId/events, /resumes`. Routes là thiết kế đề xuất; không phải endpoint đã implement.

## Luồng sử dụng

1. Save job nhập tay/URL → Company → pipeline board/list.
2. Move status, ghi notes/activity/reminders; link exact resumeversion đã dùng khi apply.
3. Tạo hoặc liên kết Personal Calendar Event trong Nexora; đổi giờ/trạng thái tại Calendar theo event lifecycle, chỉ một reminder source.
4. Upload resume version hoặc link Document savedversion; share resume riêng bằng Sharing Engine.

## Dữ liệu và validation

- Job title/company/sourceURL/location/workmode/type/salarytext/description/dates.
- Saved/Preparing/Applied/Screening/Interviewing/Offer/Accepted/Rejected/Withdrawn/Closed.
- Company name/URL/industry/location/notes; contact user-entered private.
- CalendarLink: JobApplicationId + CalendarEventId cùng owner; không duplicate thời gian/reminder hoặc interview/conference execution fields.
- Resume name/language/version/sourceFile hoặc immutable Documentversion/status Active/Archived.

## Hành vi và lifecycle

- **FX-39-BR-001:** Manual pipeline transitions giữa states đều explicit+history; backward từ terminal cảnh báo reason, không áp Project immutableterminal sang Job.
- **FX-39-BR-002:** Company merge preview preserve links và historical company label, không merge vì chỉ cùng tên.
- **FX-39-BR-003:** Application giữ exactresumeversion; update resume không rewrite bản đã nộp.
- **FX-39-BR-004:** Interview feedback/contact/salary/private notes không vào share. Resume share không lộ Job tracker.
- **FX-39-BR-005:** Calendar là authority đã chốt: link Personal Event, create-and-link atomic/idempotent; unlink không xóa Event; không tự mở lại Completed/Canceled Event.
- **FX-39-BR-006:** Resume input/output cơ bản .docx/.md; fidelity preview theo format contract, không PDF/HTML export, không AI.

## Quyền, API và tích hợp

- JobAggregate/TransitionJob/MergeCompany/LinkCalendarEvent/AttachResumeVersion.
- Calendar sở hữu Event, Career chỉ giữ reference; Files/Document version refs immutable, Share resume provider.

Áp dụng [hợp đồng chung](00-shared-behavior.md): owner isolation, module/action gate, concurrency, idempotency, lỗi/loading/empty, phân trang và lifecycle. Support/Emergency chỉ read-only có grant; không thừa hưởng owner mutation, secret reveal hoặc export. API cụ thể phải theo command/query này và được chốt trong solution design.

## Tiêu chí nghiệm thu

- **FX-39-AC-001:** Resume update không sửa historical application reference.
- **FX-39-AC-002:** Company merge không orphan interview/job.
- **FX-39-AC-003:** Shared resume không lộ interview notes.
- **FX-39-AC-004:** Create/link Event retry không duplicate link/Event/reminder; Calendar own terminal rules vẫn áp dụng.

Các AC nguồn và common gates vẫn bắt buộc; đây là các scenario bổ sung, không thay thế toàn bộ test specification.

## Traceability và phần còn mở

- [phase-07-assets-and-career.md](../requirements/phases/phase-07-assets-and-career.md): `P07-COM-001`, `P07-INT-001`, `P07-INT-002`, `P07-JOB-001`, `P07-JOB-002`, `P07-JOB-003`, `P07-JOB-004`, `P07-JOB-005`, `P07-RES-001`, `P07-RES-002`, `P07-RES-003`, `P07-RES-004`

Q11 DOCX/MD và Q12 internal Calendar workflow đã đóng. Sensitive Career sharing fields vẫn theo Q03/P-H03; không chặn owner job/event linking.
