# FX-04 — Read-only Sharing: actions

Catalog v1 · 2026-09-07 · Docs-only. New key decomposition = Resolved delegated; source business decisions giữ nguyên; Blocked rows không được kích hoạt bằng grant.

## Sources và phạm vi

- [Feature / validation / state graph](../../features/04-read-only-sharing.md) — FX-04-BR-001, FX-04-BR-002, FX-04-BR-003, FX-04-BR-004, FX-04-BR-005. Các BR này áp cho feature; không gán sai một BR duy nhất cho mọi row.
- [UX specification](../../ux-ui/modules/04-sharing.md); [exact screen bindings](../06-screen-bindings.md).
- [Authorization contract](../00-authorization-contract.md), [semantic field guards](../01-composition-and-field-guards.md), [SDK contract](../04-module-action-contract.md).
- Namespace `sharing` là stable logical key, bind installed ModuleId trong manifest. **Cùng owner resource; link mode/expiry/revoke và source projection hiện tại**.

## Catalog

“All prerequisites” ở row bao gồm explicit Requires **và** source/dynamic dependencies trong guard; danh sách Requires trống không có nghĩa bỏ owner/module/lifecycle checks. Every action denies unknown fields and unapproved semantic changes. Source state matrix luôn kiểm tra ở handler, không suy quyền từ verb hoặc tên button.

| Action key / hành vi | Kind / context | Admin checkbox? | Risk (DB mapping) | Status / gate | UI entry |
| --- | --- | --- | --- | --- | --- |
| <a id="sharing-link-read"></a>`sharing.link.read` — Xem own links | QUERY / SELF | Yes, gated | Normal (Normal) | Resolved delegated: action contract; source business rules unchanged | FX04-S01, FX04-S02 |
| <a id="sharing-link-create"></a>`sharing.link.create` — Tạo link | COMMAND / SELF | Yes, gated | Disclosure (Sensitive) | Resolved delegated: action contract; source business rules unchanged | FX04-S01 |
| <a id="sharing-link-update"></a>`sharing.link.update` — Đổi audience/expiry/allowlist | COMMAND / SELF | Yes, gated | Disclosure (Sensitive) | Resolved delegated: action contract; source business rules unchanged | FX04-S01 |
| <a id="sharing-link-revoke"></a>`sharing.link.revoke` — Thu hồi link | COMMAND / SELF | Yes, gated | Disclosure (Sensitive) | Resolved delegated: action contract; source business rules unchanged | FX04-S02 |
| <a id="sharing-link-resolve"></a>`sharing.link.resolve` — Đọc approved shared projection | QUERY / LINK | No | Disclosure (Sensitive) | Resolved delegated: action contract; source business rules unchanged | FX04-S03, FX04-S04 |
| <a id="sharing-link-copy-created"></a>`sharing.link.copy_created` — Copy URL vừa tạo | LOCAL / SELF | No | Disclosure (Sensitive) | Resolved delegated: action contract; source business rules unchanged | FX04-S01 |

## Điều kiện riêng theo operation

| Action | Guard / validation / effects | Explicit dependencies bổ sung |
| --- | --- | --- |
| `sharing.link.read` | Cùng owner resource; link mode/expiry/revoke và source projection hiện tại; Cùng owner resource; link mode/expiry/revoke và source projection hiện tại | Common + dynamic source/provider guards |
| `sharing.link.create` | Kèm source *.share; create Published Documents only; không đổi source/pinned Resume version bằng Update; Cùng owner resource; link mode/expiry/revoke và source projection hiện tại | Common + dynamic source/provider guards |
| `sharing.link.update` | Kèm source *.share; create Published Documents only; không đổi source/pinned Resume version bằng Update; Cùng owner resource; link mode/expiry/revoke và source projection hiện tại | Common + dynamic source/provider guards |
| `sharing.link.revoke` | Kèm source *.share; create Published Documents only; không đổi source/pinned Resume version bằng Update; Cùng owner resource; link mode/expiry/revoke và source projection hiện tại | Common + dynamic source/provider guards |
| `sharing.link.resolve` | Token + mode/auth/allowlist + owner module + source lifecycle + field projection; không owner.read hoặc viewer module grant bypass; Cùng owner resource; link mode/expiry/revoke và source projection hiện tại | Common + dynamic source/provider guards |
| `sharing.link.copy_created` | Chỉ plaintext còn trong authorized creation-result memory; hash-only không reconstruct URL cũ; Cùng owner resource; link mode/expiry/revoke và source projection hiện tại | `sharing.link.create` |

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
