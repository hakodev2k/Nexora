# Task brief và evidence dùng goals

Copy phần mẫu dưới vào work item/PR hoặc tài liệu task phù hợp. Không điền `Passed` từ dự kiến. Nguồn: [AGENTS](../../AGENTS.md), [verification](../../.ai/verification.md), [M01 readiness](../delivery/milestone-01/06-readiness-and-evidence.md).

## Task brief

| Trường | Giá trị cần ghi |
| --- | --- |
| Task / accountable owner | ID, người/agent phụ trách; reviewer độc lập nếu bắt buộc |
| User authorization | Lời approve hoặc reference trong session/PO record; phạm vi docs/code/deploy tách rõ |
| Source state | Branch, commit/tree của source đã đọc; cập nhật nếu source đổi |
| Product phase / delivery / milestone | Pxx, RMxx, Mxx/story IDs có thật; không tự đặt milestone thành Approved |
| Goal IDs | SYS, module, cross-module và phase/step goals liên quan |
| Source requirements | Exact requirement/BR/AC/POAC IDs cùng file nguồn hiện hành |
| Action binding | Exact action keys + context + prerequisites/field guards + status/gates; không chỉ ghi tên module |
| Contracts | Story, request/response/errors, data invariants/transactions, UX screens và state/field matrix |
| Rules/skills loaded | Exact paths thực sự đã đọc theo routing; missing control/equivalent plan ghi rõ |
| In scope / outside slice | Capability cụ thể; phần outside không bị hủy khỏi R1 |
| Open gates | Decision ID, affected capability, next owner; paused cần resume riêng |
| Acceptance plan | Allowed/denied/boundary/race/failure assertions; test layer, fixtures và expected outcome có nguồn |
| Rollback/recovery | Impact dữ liệu/API, recovery plan theo rủi ro và deployment scope; không tự thao tác production |

## Trace từ goal tới bằng chứng

Mỗi action/story được chọn có ít nhất một goal; một test có thể chứng minh nhiều goals nếu assertion thật sự bao phủ. Phải kiểm tra tất cả source AC áp dụng, không chỉ ba module goals.

| Goal IDs | Source requirement/AC | Story + exact action | Code/contract reference | Test/assertion + expected result | Executed result | Evidence reference / tested revision | Review / remaining gap |
| --- | --- | --- | --- | --- | --- | --- | --- |
| Điền IDs có thật | File + IDs | Không dùng wildcard | Sau implementation | Trước implementation | Not run / Passed / Failed / Blocked | Actual log/run/artifact; không placeholder URL | Reviewer hoặc Pending |

## Handoff và completion

- Changes: outcome đã thực hiện, files/commit, phần không thay đổi scope.
- Verification: exact commands/environment, exit/result, tested revision và artifact; skip/blocked có lý do. Edits liên quan sau test làm evidence stale và cần kiểm tra lại phần bị ảnh hưởng.
- Review: ai review độc lập, phát hiện gì/đã xử lý gì/chưa re-review. Self-review hoặc CI không được đổi nhãn thành independent security review.
- Completion: báo rõ slice/goal nào đạt, capability nào còn thiếu; không module-complete/phase-complete/R1-complete chỉ vì selected tests green.
- GitHub: PR trên branch; required checks/review process theo repo. Goals không tự bật branch protection hoặc kiểm soát host agent; runtime tests + review + merge rules phải thực sự được cấu hình.

Mỗi lần source PO/action/contract đổi: xác định goals bị ảnh hưởng, update docs/goals/source trace và expected tests trong cùng thay đổi; giữ IDs ổn định hoặc retire có mapping. Không sửa assertion chỉ để giữ test xanh khi không có nguồn cho business change.
