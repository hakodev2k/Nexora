# FX-39 — Career / Jobs / Resumes: actions

Catalog v1 · 2026-09-07 · Docs-only. New key decomposition = Resolved delegated; source business decisions giữ nguyên; Blocked rows không được kích hoạt bằng grant.

## Sources và phạm vi

- [Feature / validation / state graph](../../features/39-career-and-resumes.md) — FX-39-BR-001, FX-39-BR-002, FX-39-BR-003, FX-39-BR-004, FX-39-BR-005, FX-39-BR-006. Các BR này áp cho feature; không gán sai một BR duy nhất cho mọi row.
- [UX specification](../../ux-ui/modules/39-career-jobs-resume.md); [exact screen bindings](../06-screen-bindings.md).
- [Authorization contract](../00-authorization-contract.md), [semantic field guards](../01-composition-and-field-guards.md), [SDK contract](../04-module-action-contract.md).
- Namespace `career` là stable logical key, bind installed ModuleId trong manifest. **Own job applications; sensitive salary/contact; no recruiter collaboration**.

## Catalog

“All prerequisites” ở row bao gồm explicit Requires **và** source/dynamic dependencies trong guard; danh sách Requires trống không có nghĩa bỏ owner/module/lifecycle checks. Every action denies unknown fields and unapproved semantic changes. Source state matrix luôn kiểm tra ở handler, không suy quyền từ verb hoặc tên button.

| Action key / hành vi | Kind / context | Admin checkbox? | Risk (DB mapping) | Status / gate | UI entry |
| --- | --- | --- | --- | --- | --- |
| <a id="career-job-read"></a>`career.job.read` — Xem Job application | QUERY / SELF | Yes, gated | Normal (Normal) | Resolved delegated: action contract; source business rules unchanged | FX39-S01 |
| <a id="career-job-create"></a>`career.job.create` — Tạo Job application | COMMAND / SELF | Yes, gated | Normal (Normal) | Resolved delegated: action contract; source business rules unchanged | FX39-S02 |
| <a id="career-job-update"></a>`career.job.update` — Sửa Job application | COMMAND / SELF | Yes, gated | Normal (Normal) | Resolved delegated: action contract; source business rules unchanged | FX39-S02 |
| <a id="career-job-transition"></a>`career.job.transition` — Đổi pipeline stage | COMMAND / SELF | Yes, gated | Normal (Normal) | Resolved delegated: action contract; source business rules unchanged | FX39-S01 |
| <a id="career-job-trash"></a>`career.job.trash` — Đưa job vào Thùng rác | COMMAND / SELF | Yes, gated | Lifecycle (Normal) | Resolved delegated: action contract; source business rules unchanged | FX39-S01 |
| <a id="career-job-restore"></a>`career.job.restore` — Khôi phục job từ Thùng rác | COMMAND / SELF | Yes, gated | Lifecycle (Normal) | Resolved delegated: action contract; source business rules unchanged | FX39-S01 |
| <a id="career-job-purge"></a>`career.job.purge` — Xóa vĩnh viễn job | COMMAND / SELF | Yes, gated | Destructive (Administrative) | Resolved delegated: action contract; source business rules unchanged | FX39-S01 |
| <a id="career-job-history"></a>`career.job.history` — Xem lịch sử job | QUERY / SELF | Yes, gated | Sensitive (Sensitive) | Resolved delegated: action contract; source business rules unchanged | FX39-S07 |
| <a id="career-company-read"></a>`career.company.read` — Xem Company | QUERY / SELF | Yes, gated | Normal (Normal) | Resolved delegated: action contract; source business rules unchanged | FX39-S03 |
| <a id="career-company-create"></a>`career.company.create` — Tạo Company | COMMAND / SELF | Yes, gated | Normal (Normal) | Resolved delegated: action contract; source business rules unchanged | FX39-S03 |
| <a id="career-company-update"></a>`career.company.update` — Sửa Company | COMMAND / SELF | Yes, gated | Normal (Normal) | Resolved delegated: action contract; source business rules unchanged | FX39-S03 |
| <a id="career-company-merge"></a>`career.company.merge` — Gộp Company | COMMAND / SELF | Yes, gated | Normal (Normal) | Resolved delegated: action contract; source business rules unchanged | FX39-S03 |
| <a id="career-interview-read"></a>`career.interview.read` — Xem Interview | QUERY / SELF | Yes, gated | Normal (Normal) | Resolved delegated: action contract; source business rules unchanged | FX39-S04 |
| <a id="career-interview-create"></a>`career.interview.create` — Tạo Interview | COMMAND / SELF | Yes, gated | Normal (Normal) | Resolved delegated: action contract; source business rules unchanged | FX39-S04 |
| <a id="career-interview-update"></a>`career.interview.update` — Sửa Interview | COMMAND / SELF | Yes, gated | Normal (Normal) | Resolved delegated: action contract; source business rules unchanged | FX39-S04 |
| <a id="career-interview-complete"></a>`career.interview.complete` — Hoàn thành Interview | COMMAND / SELF | Yes, gated | Normal (Normal) | Resolved delegated: action contract; source business rules unchanged | FX39-S04 |
| <a id="career-interview-cancel"></a>`career.interview.cancel` — Hủy Interview | COMMAND / SELF | Yes, gated | Normal (Normal) | Resolved delegated: action contract; source business rules unchanged | FX39-S04 |
| <a id="career-interview-link-calendar"></a>`career.interview.link_calendar` — Liên kết Calendar Event | COMPOSITE / SELF | Yes, gated | Normal (Normal) | Blocked Q-12 | FX39-S04 |
| <a id="career-resume-read"></a>`career.resume.read` — Xem Resume metadata | QUERY / SELF | Yes, gated | Normal (Normal) | Resolved delegated: action contract; source business rules unchanged | FX39-S05 |
| <a id="career-resume-create"></a>`career.resume.create` — Tạo Resume metadata | COMMAND / SELF | Yes, gated | Normal (Normal) | Resolved delegated: action contract; source business rules unchanged | FX39-S05 |
| <a id="career-resume-update"></a>`career.resume.update` — Sửa Resume metadata | COMMAND / SELF | Yes, gated | Normal (Normal) | Resolved delegated: action contract; source business rules unchanged | FX39-S05 |
| <a id="career-resume-upload-version"></a>`career.resume.upload_version` — Thêm immutable file version | COMMAND / SELF | Yes, gated | Normal (Normal) | Resolved delegated: action contract; source business rules unchanged | FX39-S05 |
| <a id="career-resume-select-document"></a>`career.resume.select_document` — Chọn exact Document version | COMMAND / SELF | Yes, gated | Normal (Normal) | Resolved delegated: action contract; source business rules unchanged | FX39-S05 |
| <a id="career-resume-download"></a>`career.resume.download` — Download exact Resume version | COMMAND / SELF | Yes, gated | Disclosure (Sensitive) | Resolved delegated: action contract; source business rules unchanged | FX39-S06 |
| <a id="career-resume-archive"></a>`career.resume.archive` — Archive resume | COMMAND / SELF | Yes, gated | Lifecycle (Normal) | Resolved delegated: action contract; source business rules unchanged | FX39-S05 |
| <a id="career-resume-unarchive"></a>`career.resume.unarchive` — Unarchive resume | COMMAND / SELF | Yes, gated | Lifecycle (Normal) | Resolved delegated: action contract; source business rules unchanged | FX39-S05 |
| <a id="career-resume-trash"></a>`career.resume.trash` — Đưa resume vào Thùng rác | COMMAND / SELF | Yes, gated | Lifecycle (Normal) | Resolved delegated: action contract; source business rules unchanged | FX39-S05 |
| <a id="career-resume-restore"></a>`career.resume.restore` — Khôi phục resume từ Thùng rác | COMMAND / SELF | Yes, gated | Lifecycle (Normal) | Resolved delegated: action contract; source business rules unchanged | FX39-S05 |
| <a id="career-resume-purge"></a>`career.resume.purge` — Xóa vĩnh viễn resume | COMMAND / SELF | Yes, gated | Destructive (Administrative) | Resolved delegated: action contract; source business rules unchanged | FX39-S05 |
| <a id="career-resume-history"></a>`career.resume.history` — Xem lịch sử resume | QUERY / SELF | Yes, gated | Sensitive (Sensitive) | Resolved delegated: action contract; source business rules unchanged | FX39-S07 |
| <a id="career-resume-share"></a>`career.resume.share` — Quản lý link chỉ-đọc của resume | COMPOSITE / SELF | Yes, gated | Disclosure (Sensitive) | Resolved delegated: action contract; source business rules unchanged | FX39-S06 |
| <a id="career-application-attach-resume"></a>`career.application.attach_resume` — Gắn exact Resume version vào application | COMMAND / SELF | Yes, gated | Normal (Normal) | Resolved delegated: action contract; source business rules unchanged | FX39-S02 |
| <a id="career-resume-convert"></a>`career.resume.convert` — Convert/render Resume output | COMMAND / SELF | Yes, gated | Disclosure (Sensitive) | Blocked Q-11 | FX39-S06 |
| <a id="career-support-read"></a>`career.support.read` — Xem safe support projection của module | QUERY / SUPPORT | Yes, gated | Sensitive (Sensitive) | Blocked Q-03; domain gates also apply | FX05-S03, FX05-S05 |

## Điều kiện riêng theo operation

| Action | Guard / validation / effects | Explicit dependencies bổ sung |
| --- | --- | --- |
| `career.job.read` | Own job applications; sensitive salary/contact; no recruiter collaboration; Own job applications; sensitive salary/contact; no recruiter collaboration | Common + dynamic source/provider guards |
| `career.job.create` | Own job applications; sensitive salary/contact; no recruiter collaboration; Own job applications; sensitive salary/contact; no recruiter collaboration | Common + dynamic source/provider guards |
| `career.job.update` | Own job applications; sensitive salary/contact; no recruiter collaboration; Own job applications; sensitive salary/contact; no recruiter collaboration | `career.job.read` |
| `career.job.transition` | FX-39 graph/history; không dùng Project terminal lock; Own job applications; sensitive salary/contact; no recruiter collaboration | Common + dynamic source/provider guards |
| `career.job.trash` | Own job applications; sensitive salary/contact; no recruiter collaboration; preview aggregate, không purge; Own job applications; sensitive salary/contact; no recruiter collaboration | Common + dynamic source/provider guards |
| `career.job.restore` | Own job applications; sensitive salary/contact; no recruiter collaboration; đúng deletion cohort, parent hợp lệ; Own job applications; sensitive salary/contact; no recruiter collaboration | Common + dynamic source/provider guards |
| `career.job.purge` | Own job applications; sensitive salary/contact; no recruiter collaboration; chỉ Trash, preview pins/dependencies, xác nhận không thể hoàn tác; Own job applications; sensitive salary/contact; no recruiter collaboration | Common + dynamic source/provider guards |
| `career.job.history` | Owner-only history, cùng owner/module; không share/support; Own job applications; sensitive salary/contact; no recruiter collaboration | Common + dynamic source/provider guards |
| `career.company.read` | Own job applications; sensitive salary/contact; no recruiter collaboration; Own job applications; sensitive salary/contact; no recruiter collaboration | Common + dynamic source/provider guards |
| `career.company.create` | Own job applications; sensitive salary/contact; no recruiter collaboration; Own job applications; sensitive salary/contact; no recruiter collaboration | Common + dynamic source/provider guards |
| `career.company.update` | Own job applications; sensitive salary/contact; no recruiter collaboration; Own job applications; sensitive salary/contact; no recruiter collaboration | `career.company.read` |
| `career.company.merge` | Explicit references preview; no external CRM sync; Own job applications; sensitive salary/contact; no recruiter collaboration | Common + dynamic source/provider guards |
| `career.interview.read` | Own job applications; sensitive salary/contact; no recruiter collaboration; Own job applications; sensitive salary/contact; no recruiter collaboration | Common + dynamic source/provider guards |
| `career.interview.create` | Own job applications; sensitive salary/contact; no recruiter collaboration; Own job applications; sensitive salary/contact; no recruiter collaboration | Common + dynamic source/provider guards |
| `career.interview.update` | Own job applications; sensitive salary/contact; no recruiter collaboration; Own job applications; sensitive salary/contact; no recruiter collaboration | `career.interview.read` |
| `career.interview.complete` | Explicit lifecycle; no Calendar mutation; Own job applications; sensitive salary/contact; no recruiter collaboration | Common + dynamic source/provider guards |
| `career.interview.cancel` | Explicit lifecycle; no Calendar mutation; Own job applications; sensitive salary/contact; no recruiter collaboration | Common + dynamic source/provider guards |
| `career.interview.link_calendar` | Must settle source ownership/sync policy first; Own job applications; sensitive salary/contact; no recruiter collaboration | Common + dynamic source/provider guards |
| `career.resume.read` | Own job applications; sensitive salary/contact; no recruiter collaboration; Own job applications; sensitive salary/contact; no recruiter collaboration | Common + dynamic source/provider guards |
| `career.resume.create` | Own job applications; sensitive salary/contact; no recruiter collaboration; Own job applications; sensitive salary/contact; no recruiter collaboration | Common + dynamic source/provider guards |
| `career.resume.update` | Own job applications; sensitive salary/contact; no recruiter collaboration; Own job applications; sensitive salary/contact; no recruiter collaboration | `career.resume.read` |
| `career.resume.upload_version` | Clean file or immutable DocumentVersion; pinned version, source enabled/read; no latest pointer drift; Own job applications; sensitive salary/contact; no recruiter collaboration | Common + dynamic source/provider guards |
| `career.resume.select_document` | Clean file or immutable DocumentVersion; pinned version, source enabled/read; no latest pointer drift; Own job applications; sensitive salary/contact; no recruiter collaboration | Common + dynamic source/provider guards |
| `career.resume.download` | Read current owner/version/file-scan authorization, no silent conversion; Own job applications; sensitive salary/contact; no recruiter collaboration | Common + dynamic source/provider guards |
| `career.resume.archive` | Own job applications; sensitive salary/contact; no recruiter collaboration; ngoài Trash, chưa Archived; Own job applications; sensitive salary/contact; no recruiter collaboration | Common + dynamic source/provider guards |
| `career.resume.unarchive` | Own job applications; sensitive salary/contact; no recruiter collaboration; Archived, khôi phục previous state; Own job applications; sensitive salary/contact; no recruiter collaboration | Common + dynamic source/provider guards |
| `career.resume.trash` | Own job applications; sensitive salary/contact; no recruiter collaboration; preview aggregate, không purge; Own job applications; sensitive salary/contact; no recruiter collaboration | Common + dynamic source/provider guards |
| `career.resume.restore` | Own job applications; sensitive salary/contact; no recruiter collaboration; đúng deletion cohort, parent hợp lệ; Own job applications; sensitive salary/contact; no recruiter collaboration | Common + dynamic source/provider guards |
| `career.resume.purge` | Own job applications; sensitive salary/contact; no recruiter collaboration; chỉ Trash, preview pins/dependencies, xác nhận không thể hoàn tác; Own job applications; sensitive salary/contact; no recruiter collaboration | Common + dynamic source/provider guards |
| `career.resume.history` | Owner-only history, cùng owner/module; không share/support; Own job applications; sensitive salary/contact; no recruiter collaboration | Common + dynamic source/provider guards |
| `career.resume.share` | Source eligible, SharingEnabled, projection chính xác; kết hợp sharing.link.*; không history/reason/secret; Own job applications; sensitive salary/contact; no recruiter collaboration | `sharing.link.read` |
| `career.application.attach_resume` | Same owner, immutable pin preserved across later edits/deletion; files/download needs source permissions; Own job applications; sensitive salary/contact; no recruiter collaboration | Common + dynamic source/provider guards |
| `career.resume.convert` | Output fidelity/formats approval needed; not same as original file download; Own job applications; sensitive salary/contact; no recruiter collaboration | Common + dynamic source/provider guards |
| `career.support.read` | Qualified Admin + support.session.open + current one-module consent OR authorized SuperAdmin Emergency; target enabled; approved redacted projection only; no owner history/export/reveal/linked-module expansion; Own job applications; sensitive salary/contact; no recruiter collaboration | Common + dynamic source/provider guards |

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
