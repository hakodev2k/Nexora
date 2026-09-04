# Non-functional Requirements

**Document ID:** `NX-NFR-001`  
**Status:** Working draft  
**Note:** Numeric targets marked `PROPOSED` require measurement profile approval in Phase 0/1.

## 1. Responsive UX và compatibility

| ID | Pri | Requirement | Verification |
|---|---:|---|---|
| `UX-001` | P0 | Mọi P0 user journey MUST sử dụng được trên desktop và mobile web. | Test matrix với viewport representative; không có action chỉ tồn tại qua hover. |
| `UX-002` | P0 | Tablet không phải target riêng nhưng không được overflow, che action hoặc unusable. | Smoke test portrait/landscape representative. |
| `UX-003` | P0 | Loading, empty, validation, permission-denied, provider-failure và unexpected-error states phải có UI rõ ràng. | Mỗi screen P0 có state inventory và test/screenshot evidence. |
| `UX-004` | P0 | Destructive/sensitive action phải phân biệt rõ, yêu cầu confirmation tương xứng và báo outcome. | Delete, permanent delete, revoke, reveal/export, role change được usability review. |
| `UX-005` | P1 | Browser support `PROPOSED`: hai major version mới nhất của Chrome, Edge, Firefox và Safari tại thời điểm release. | Cross-browser suite P0 pass; exception được ghi. |
| `UX-006` | P1 | UI giữ terminology/state/action nhất quán giữa module. | Design/content review theo glossary; không dùng hai tên cho cùng capability. |

## 2. Accessibility

Target `PROPOSED`: WCAG 2.2 Level AA cho P0 flows.

| ID | Pri | Requirement | Verification |
|---|---:|---|---|
| `A11Y-001` | P0 | Core flow sử dụng được bằng keyboard, focus order hợp lý và focus visible. | Manual keyboard test + automated checks. |
| `A11Y-002` | P0 | Form control có accessible name, instruction/error được liên kết và không chỉ dựa vào màu. | Screen-reader spot check và automated rules. |
| `A11Y-003` | P0 | Modal/menu/toast/dynamic update quản lý focus và announcement phù hợp. | Manual assistive-technology test cho component dùng trong P0. |
| `A11Y-004` | P0 | Text/UI contrast và target size đáp ứng target đã duyệt. | Automated contrast scan + design QA. |
| `A11Y-005` | P1 | Chart có text/table equivalent cho thông tin thiết yếu. | Finance/Dashboard accessibility review. |

## 3. Performance budgets

Measurement phải dùng dataset, hardware, browser và network profile được version hóa; không tuyên bố pass nếu thiếu profile.

| ID | Pri | Target `PROPOSED` | Điều kiện |
|---|---:|---|---|
| `PERF-001` | P0 | P95 API read/write tương tác thông thường ≤ 500 ms server time; P99 ≤ 1,500 ms. | Không tính external provider; representative local profile. |
| `PERF-002` | P0 | Paginated list mặc định ≤ 50 items và không có unbounded endpoint. | API contract/static test. |
| `PERF-003` | P1 | LCP ≤ 2.5 s, INP ≤ 200 ms, CLS ≤ 0.1 cho P0 routes ở profile đã duyệt. | Lab + browser telemetry nếu có. |
| `PERF-004` | P0 | Search P95 ≤ 1 s với baseline dataset; kết quả đầu tiên không đợi index toàn hệ thống rebuild. | Load test với access filtering. |
| `PERF-005` | P0 | Background job lớn không block interactive request thread và có timeout/cancellation. | Concurrency/fault test. |
| `PERF-006` | P1 | Dashboard fan-out có bounded concurrency/cache/read model; một widget lỗi không chặn toàn dashboard. | Failure injection và timing test. |

## 4. Reliability và data integrity

| ID | Pri | Requirement | Verification |
|---|---:|---|---|
| `REL-001` | P0 | Multi-record state transition phải atomic hoặc có compensation rõ. | Fault injection giữa các bước không tạo state không thể phục hồi. |
| `REL-002` | P0 | Retryable write/background operation phải idempotent. | Duplicate request/job không nhân đôi transaction, alert hoặc share. |
| `REL-003` | P0 | Mọi external call có timeout; retry có backoff/jitter và giới hạn. | Provider timeout/429 test không gây thread/job exhaustion. |
| `REL-004` | P0 | Migration phải forward-safe, có pre-check và documented rollback/restore path. | Migration rehearsal trên copy của baseline database. |
| `REL-005` | P0 | Concurrent update conflict không được âm thầm overwrite dữ liệu quan trọng. | Optimistic concurrency/version strategy test cho aggregate đã chỉ định. |
| `REL-006` | P0 | Cache là optimization, không là source of truth cho dữ liệu không thể tái tạo. | Flush Redis không làm mất persistent business data; app phục hồi có kiểm soát. |
| `REL-007` | P1 | Scheduled job có missed-run policy rõ (`skip`, `catch-up`, `coalesce`). | Restart qua schedule boundary cho outcome đúng từng job type. |

Local-first baseline không có uptime SLA. Availability/SLO production phải được quyết định ở Phase 8.

## 5. Capacity và scalability

| ID | Pri | Requirement | Verification |
|---|---:|---|---|
| `CAP-001` | P0 | Không load toàn bộ user data vào memory cho list/search/export. | Code/performance test với dataset lớn hơn baseline. |
| `CAP-002` | P0 | File size, request size, page size, export size và job concurrency có configurable bounds. | Boundary tests và documented defaults. |
| `CAP-003` | P1 | Baseline capacity profile phải xác định số user, record/module, file volume và job rate trước performance sign-off. | Versioned load profile + report. |
| `CAP-004` | P1 | Thiết kế không phụ thuộc sticky in-memory session nếu mục tiêu Phase 8 yêu cầu nhiều app instance. | Architecture decision/test theo deployment target. |

## 6. Observability và diagnosability

| ID | Pri | Requirement | Verification |
|---|---:|---|---|
| `OBS-001` | P0 | Request/job có correlation hoặc trace identifier xuyên boundary nội bộ. | Error ID nối được UI response với server log và job run. |
| `OBS-002` | P0 | Structured log có timestamp, level, service/module, event name, outcome; áp dụng redaction. | Schema/redaction tests. |
| `OBS-003` | P0 | Health checks phân biệt liveness/readiness và dependency state cần thiết. | Dependency outage cho status đúng, không làm restart loop vô ích. |
| `OBS-004` | P1 | Metrics tối thiểu: request/error/latency, job queue/run/failure, provider errors/rate limits, DB/cache health. | Dashboard hoặc query/runbook chứng minh đọc được. |
| `OBS-005` | P1 | Alert phải actionable, có owner/runbook và tránh lộ user data. | Alert review/tabletop exercise. |

## 7. Time, locale và numeric correctness

| ID | Pri | Requirement | Verification |
|---|---:|---|---|
| `DAT-001` | P0 | Timestamp sự kiện lưu dưới dạng instant chuẩn (thường UTC); user input/display dùng timezone preference. | DST/timezone boundary tests; API contract nêu offset/zone semantics. |
| `DAT-002` | P0 | Date-only (birthday/due date nếu không có time) không được tự chuyển ngày vì timezone. | Cross-timezone round-trip giữ nguyên calendar date. |
| `DAT-003` | P0 | Money dùng decimal/fixed-precision và luôn gắn currency; không dùng binary floating point. | Calculation/rounding tests theo currency rule. |
| `DAT-004` | P0 | Recurrence có timezone, start, end/count và DST behavior rõ. | Test spring-forward/fall-back hoặc equivalent zone transition. |
| `DAT-005` | P1 | Ngôn ngữ UI, default locale, currency và first-day-of-week là `TBD`. | `DEC-PRD-006` đóng trước content freeze. |

## 8. Backup, recovery và durability

| ID | Pri | Requirement | Verification |
|---|---:|---|---|
| `BKP-001` | P0 trước dữ liệu giá trị | Backup scope gồm SQL data, file objects, configuration cần thiết và encryption key dependency được mô tả rõ. | Inventory đối chiếu với restore result. |
| `BKP-002` | P0 trước production | Restore là operation được kiểm soát, audit và không ghi đè environment sai. | Restore rehearsal vào isolated target. |
| `BKP-003` | P1 | RPO/RTO, schedule, retention và off-device/off-site strategy là decision Phase 8. | Approved decision + automated evidence. |
| `BKP-004` | P0 | Backup failure visible; “job ran” không đồng nghĩa “backup restorable”. | Alert + periodic integrity/restore test. |

## 9. Maintainability và testability

| ID | Pri | Requirement | Verification |
|---|---:|---|---|
| `MNT-001` | P0 | Module boundary và dependency direction được document bằng ADR/architecture spec trước implementation tương ứng. | Architecture review; không có circular domain coupling chưa justify. |
| `MNT-002` | P0 | API/schema change có version/migration/compatibility strategy. | Contract tests và upgrade test. |
| `MNT-003` | P0 | Automated test pyramid bao phủ domain rules, authorization, persistence integration và P0 E2E flows. | CI report; required suites pass trước merge/release. |
| `MNT-004` | P0 | Lint/build/test/security scan có deterministic command và documented local setup. | Clean checkout chạy được theo README. |
| `MNT-005` | P1 | Feature flags/module enablement không được bypass server authorization hoặc để schema half-configured. | Flag off/on migration and access tests. |
| `MNT-006` | P0 | Không commit credential, production data hoặc personal sensitive sample. | Secret scan + sanitized fixture review. |

## 10. Local deployment requirements

| ID | Pri | Requirement | Verification |
|---|---:|---|---|
| `LOC-001` | P0 | Developer/operator có documented path để khởi tạo app, SQL, Redis và storage dependency ở local. | Fresh-machine/fresh-environment setup rehearsal. |
| `LOC-002` | P0 | First-run tạo SuperAdmin an toàn, không dùng default password hard-coded. | Setup không mở app trước khi bootstrap hoàn tất; secret không vào git/log. |
| `LOC-003` | P0 | Environment-specific config tách khỏi source và có safe development defaults. | Missing required secret fail-fast với hướng dẫn không nhạy cảm. |
| `LOC-004` | P1 | Seed/demo data là tùy chọn, idempotent và không trộn vào user data thật. | Chạy lại seed không duplicate; production mode không tự seed. |

## 11. Quality gate chung

Mỗi phase phải có: acceptance mapping, automated test evidence, responsive/accessibility smoke test, security negative tests, migration/rollback evidence nếu đổi schema, log redaction check và danh sách known limitations được duyệt.
