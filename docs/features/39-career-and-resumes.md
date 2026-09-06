# Career, Companies, Interviews và Resumes

FX-39 · Feature specification · 2026-09-06 · Baseline requirements: d0d8418

**Trạng thái:** yêu cầu đã xác nhận được giữ nguyên; chi tiết bổ sung bên dưới là **Resolved (delegated)** theo DEC-GOV-001. Mục Q còn mở là proposal, chưa được duyệt. Tài liệu không cấp phép implement.

## Phạm vi và tham chiếu

Job opportunities, pipeline, companies/contact notes, interviews và exact resume versions.

[Teal Job Tracker](https://www.tealhq.com/tools/job-tracker): Theo dõi job applications qua pipeline và thông tin từng cơ hội.

**Áp dụng cho Nexora:** Teal tham chiếu job tracker; không AI resume generation, job scraping, outreach hoặc employer/team account.

**Màn hình:** `/career/jobs, /companies, /interviews, /resumes`. Routes là thiết kế đề xuất; không phải endpoint đã implement.

## Luồng sử dụng

1. Save job nhập tay/URL → Company → pipeline board/list.
2. Move status, ghi notes/activity/reminders; link exact resumeversion đã dùng khi apply.
3. Schedule/reschedule/cancel interview; Calendar integration còn Q-12.
4. Upload resume version hoặc link Document savedversion; share resume riêng bằng Sharing Engine.

## Dữ liệu và validation

- Job title/company/sourceURL/location/workmode/type/salarytext/description/dates.
- Saved/Preparing/Applied/Screening/Interviewing/Offer/Accepted/Rejected/Withdrawn/Closed.
- Company name/URL/industry/location/notes; contact user-entered private.
- Interview job/round/type/Start/End/timezone/location/link/participants text/notes/status Scheduled/Completed/Cancelled.
- Resume name/language/version/sourceFile hoặc immutable Documentversion/status Active/Archived.

## Hành vi và lifecycle

- **FX-39-BR-001:** Manual pipeline transitions giữa states đều explicit+history; backward từ terminal cảnh báo reason, không áp Project immutableterminal sang Job.
- **FX-39-BR-002:** Company merge preview preserve links và historical company label, không merge vì chỉ cùng tên.
- **FX-39-BR-003:** Application giữ exactresumeversion; update resume không rewrite bản đã nộp.
- **FX-39-BR-004:** Interview feedback/contact/salary/private notes không vào share. Resume share không lộ Job tracker.
- **FX-39-BR-005:** Interview Calendar source conflict Q-12: proposal explicit linked Calendar personal Event, phải chốt authority trước code; reminder không nhân hai nguồn.
- **FX-39-BR-006:** Resume conversion/template formats Q-11 cần fidelity/privacy; file upload vẫn core, không AI.

## Quyền, API và tích hợp

- JobAggregate/TransitionJob/MergeCompany/RecordInterview/AttachResumeVersion.
- Calendar linking authority Q-12; Files/Document version refs immutable, Share resume provider.

Áp dụng [hợp đồng chung](00-shared-behavior.md): owner isolation, module/action gate, concurrency, idempotency, lỗi/loading/empty, phân trang và lifecycle. Support/Emergency chỉ read-only có grant; không thừa hưởng owner mutation, secret reveal hoặc export. API cụ thể phải theo command/query này và được chốt trong solution design.

## Tiêu chí nghiệm thu

- **FX-39-AC-001:** Resume update không sửa historical application reference.
- **FX-39-AC-002:** Company merge không orphan interview/job.
- **FX-39-AC-003:** Shared resume không lộ interview notes.
- **FX-39-AC-004:** Reschedule retry không duplicate reminder; Calendar integration test blocked Q-12.

Các AC nguồn và common gates vẫn bắt buộc; đây là các scenario bổ sung, không thay thế toàn bộ test specification.

## Traceability và phần còn mở

- [phase-07-assets-and-career.md](../requirements/phases/phase-07-assets-and-career.md): `P07-COM-001`, `P07-INT-001`, `P07-INT-002`, `P07-JOB-001`, `P07-JOB-002`, `P07-JOB-003`, `P07-JOB-004`, `P07-JOB-005`, `P07-RES-001`, `P07-RES-002`, `P07-RES-003`, `P07-RES-004`

Quyết định lớn cần PO: [Q-11](90-open-decisions.md#q-11), [Q-12](90-open-decisions.md#q-12). Các hành vi phụ thuộc chúng chưa đạt Definition of Ready.
