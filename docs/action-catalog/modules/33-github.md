# FX-33 — GitHub Discovery: actions

Catalog v1 · 2026-09-07 · Docs-only. New key decomposition = Resolved delegated; source business decisions giữ nguyên; Blocked rows không được kích hoạt bằng grant.

## Sources và phạm vi

- [Feature / validation / state graph](../../features/33-github-discovery.md) — FX-33-BR-001, FX-33-BR-002, FX-33-BR-003, FX-33-BR-004, FX-33-BR-005, FX-33-BR-006. Các BR này áp cho feature; không gán sai một BR duy nhất cho mọi row.
- [UX specification](../../ux-ui/modules/33-github-discovery.md); [exact screen bindings](../06-screen-bindings.md).
- [Authorization contract](../00-authorization-contract.md), [semantic field guards](../01-composition-and-field-guards.md), [SDK contract](../04-module-action-contract.md).
- Namespace `github` là stable logical key, bind installed ModuleId trong manifest. **Public repository discovery only; không OAuth, star/fork/issues/write**.

## Catalog

“All prerequisites” ở row bao gồm explicit Requires **và** source/dynamic dependencies trong guard; danh sách Requires trống không có nghĩa bỏ owner/module/lifecycle checks. Every action denies unknown fields and unapproved semantic changes. Source state matrix luôn kiểm tra ở handler, không suy quyền từ verb hoặc tên button.

| Action key / hành vi | Kind / context | Admin checkbox? | Risk (DB mapping) | Status / gate | UI entry |
| --- | --- | --- | --- | --- | --- |
| <a id="github-repository-search"></a>`github.repository.search` — Tìm public repositories | QUERY / SELF | Yes, gated | Normal (Normal) | Resolved delegated: action contract; source business rules unchanged | FX33-S01 |
| <a id="github-repository-read"></a>`github.repository.read` — Xem repository metadata | QUERY / SELF | Yes, gated | Normal (Normal) | Resolved delegated: action contract; source business rules unchanged | FX33-S02 |
| <a id="github-repository-refresh"></a>`github.repository.refresh` — Refresh public metadata | COMMAND / SELF | Yes, gated | Normal (Normal) | Resolved delegated: action contract; source business rules unchanged | FX33-S02 |
| <a id="github-saved-read"></a>`github.saved.read` — Xem saved repositories | QUERY / SELF | Yes, gated | Normal (Normal) | Resolved delegated: action contract; source business rules unchanged | FX33-S03 |
| <a id="github-saved-save"></a>`github.saved.save` — Lưu repository | COMMAND / SELF | Yes, gated | Normal (Normal) | Resolved delegated: action contract; source business rules unchanged | FX33-S03 |
| <a id="github-saved-notes"></a>`github.saved.notes` — Sửa own notes | COMMAND / SELF | Yes, gated | Normal (Normal) | Resolved delegated: action contract; source business rules unchanged | FX33-S03 |
| <a id="github-saved-remove"></a>`github.saved.remove` — Bỏ lưu | COMMAND / SELF | Yes, gated | Normal (Normal) | Resolved delegated: action contract; source business rules unchanged | FX33-S03 |
| <a id="github-query-read"></a>`github.query.read` — Xem Saved discovery query | QUERY / SELF | Yes, gated | Normal (Normal) | Resolved delegated: action contract; source business rules unchanged | FX33-S03 |
| <a id="github-query-create"></a>`github.query.create` — Tạo Saved discovery query | COMMAND / SELF | Yes, gated | Normal (Normal) | Resolved delegated: action contract; source business rules unchanged | FX33-S03 |
| <a id="github-query-update"></a>`github.query.update` — Sửa Saved discovery query | COMMAND / SELF | Yes, gated | Normal (Normal) | Resolved delegated: action contract; source business rules unchanged | FX33-S03 |
| <a id="github-query-delete"></a>`github.query.delete` — Xóa query | COMMAND / SELF | Yes, gated | Normal (Normal) | Resolved delegated: action contract; source business rules unchanged | FX33-S03 |
| <a id="github-query-run"></a>`github.query.run` — Chạy saved query | COMMAND / SELF | Yes, gated | Normal (Normal) | Resolved delegated: action contract; source business rules unchanged | FX33-S03 |
| <a id="github-snapshot-read"></a>`github.snapshot.read` — Xem snapshots | QUERY / SELF | Yes, gated | Normal (Normal) | Resolved delegated: action contract; source business rules unchanged | FX33-S04 |
| <a id="github-snapshot-capture"></a>`github.snapshot.capture` — Chụp discovery snapshot | COMMAND / SELF | Yes, gated | Normal (Normal) | Resolved delegated: action contract; source business rules unchanged | FX33-S04 |
| <a id="github-snapshot-compare"></a>`github.snapshot.compare` — So sánh snapshots | COMMAND / SELF | Yes, gated | Normal (Normal) | Resolved delegated: action contract; source business rules unchanged | FX33-S04 |
| <a id="github-provider-fetch"></a>`github.provider.fetch` — Fetch public metadata | WORKER / SYSTEM | No | Normal (Normal) | Resolved delegated: action contract; source business rules unchanged | Trusted worker/deployment; no user control |
| <a id="github-support-read"></a>`github.support.read` — Xem safe support projection của module | QUERY / SUPPORT | Yes, gated | Sensitive (Sensitive) | Resolved delegated: diagnostic projection only; private body excluded unless source scope explicitly permits | FX05-S03, FX05-S05 |

## Điều kiện riêng theo operation

| Action | Guard / validation / effects | Explicit dependencies bổ sung |
| --- | --- | --- |
| `github.repository.search` | Approved public provider; freshness/rate-limit surfaced; Public repository discovery only; không OAuth, star/fork/issues/write | Common + dynamic source/provider guards |
| `github.repository.read` | Approved public provider; freshness/rate-limit surfaced; Public repository discovery only; không OAuth, star/fork/issues/write | Common + dynamic source/provider guards |
| `github.repository.refresh` | Approved adapter + rate-limit, no owner credentials forwarded; Public repository discovery only; không OAuth, star/fork/issues/write | Common + dynamic source/provider guards |
| `github.saved.read` | Public repository discovery only; không OAuth, star/fork/issues/write; Public repository discovery only; không OAuth, star/fork/issues/write | Common + dynamic source/provider guards |
| `github.saved.save` | Own reference metadata; không GitHub write; Public repository discovery only; không OAuth, star/fork/issues/write | Common + dynamic source/provider guards |
| `github.saved.notes` | Own reference metadata; không GitHub write; Public repository discovery only; không OAuth, star/fork/issues/write | Common + dynamic source/provider guards |
| `github.saved.remove` | Own reference metadata; không GitHub write; Public repository discovery only; không OAuth, star/fork/issues/write | Common + dynamic source/provider guards |
| `github.query.read` | Public repository discovery only; không OAuth, star/fork/issues/write; Public repository discovery only; không OAuth, star/fork/issues/write | Common + dynamic source/provider guards |
| `github.query.create` | Public repository discovery only; không OAuth, star/fork/issues/write; Public repository discovery only; không OAuth, star/fork/issues/write | Common + dynamic source/provider guards |
| `github.query.update` | Public repository discovery only; không OAuth, star/fork/issues/write; Public repository discovery only; không OAuth, star/fork/issues/write | `github.query.read` |
| `github.query.delete` | Public repository discovery only; không OAuth, star/fork/issues/write; Public repository discovery only; không OAuth, star/fork/issues/write | Common + dynamic source/provider guards |
| `github.query.run` | Same provider/search constraints; Public repository discovery only; không OAuth, star/fork/issues/write | Common + dynamic source/provider guards |
| `github.snapshot.read` | Public repository discovery only; không OAuth, star/fork/issues/write; Public repository discovery only; không OAuth, star/fork/issues/write | Common + dynamic source/provider guards |
| `github.snapshot.capture` | Same query/week/rule compatibility; unknown/stale shown explicitly; Public repository discovery only; không OAuth, star/fork/issues/write | Common + dynamic source/provider guards |
| `github.snapshot.compare` | Same query/week/rule compatibility; unknown/stale shown explicitly; Public repository discovery only; không OAuth, star/fork/issues/write | Common + dynamic source/provider guards |
| `github.provider.fetch` | Trusted public-only adapter, current policy and cache TTL; Public repository discovery only; không OAuth, star/fork/issues/write | Common + dynamic source/provider guards |
| `github.support.read` | Qualified Admin + support.session.open + current one-module consent OR authorized SuperAdmin Emergency; target enabled; approved redacted projection only; no owner history/export/reveal/linked-module expansion; Public repository discovery only; không OAuth, star/fork/issues/write | Common + dynamic source/provider guards |

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
