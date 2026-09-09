# FX-05 — Support / Emergency: actions

Catalog v1 · 2026-09-07 · Docs-only. New key decomposition = Resolved delegated; source business decisions giữ nguyên; Blocked rows không được kích hoạt bằng grant.

## Sources và phạm vi

- [Feature / validation / state graph](../../features/05-support-emergency-and-security-center.md) — FX-05-BR-001, FX-05-BR-002, FX-05-BR-003, FX-05-BR-004, FX-05-BR-005. Các BR này áp cho feature; không gán sai một BR duy nhất cho mọi row.
- [UX specification](../../ux-ui/modules/05-support-emergency.md); [exact screen bindings](../06-screen-bindings.md).
- [Authorization contract](../00-authorization-contract.md), [semantic field guards](../01-composition-and-field-guards.md), [SDK contract](../04-module-action-contract.md).
- Namespace `support` là stable logical key, bind installed ModuleId trong manifest. **Explicit access session; một module, readonly, actual actor khác target**.

## Catalog

“All prerequisites” ở row bao gồm explicit Requires **và** source/dynamic dependencies trong guard; danh sách Requires trống không có nghĩa bỏ owner/module/lifecycle checks. Every action denies unknown fields and unapproved semantic changes. Source state matrix luôn kiểm tra ở handler, không suy quyền từ verb hoặc tên button.

| Action key / hành vi | Kind / context | Admin checkbox? | Risk (DB mapping) | Status / gate | UI entry |
| --- | --- | --- | --- | --- | --- |
| <a id="support-consent-read"></a>`support.consent.read` — Xem own support grants/access records | QUERY / CONTROL | No | Normal (Normal) | Resolved delegated: action contract; source business rules unchanged | FX05-S01, FX05-S02 |
| <a id="support-consent-grant"></a>`support.consent.grant` — Cho phép hỗ trợ một module | COMMAND / CONTROL | No | Security (Administrative) | Resolved delegated: action contract; source business rules unchanged | FX05-S01 |
| <a id="support-consent-revoke"></a>`support.consent.revoke` — Thu hồi đồng ý hỗ trợ | COMMAND / CONTROL | No | Security (Administrative) | Resolved delegated: action contract; source business rules unchanged | FX05-S02 |
| <a id="support-session-open"></a>`support.session.open` — Bắt đầu Support session | COMMAND / ADMIN | Yes, gated | Security (Administrative) | Resolved delegated: action contract; source business rules unchanged | FX05-S03 |
| <a id="support-session-end"></a>`support.session.end` — Kết thúc session hỗ trợ của actor | COMMAND / CONTROL | No | Security (Administrative) | Resolved delegated: action contract; source business rules unchanged | FX05-S03 |
| <a id="support-emergency-open"></a>`support.emergency.open` — Bắt đầu Emergency | COMMAND / SUPER | No | Security (Administrative) | Confirmed emergency rule; session policy Blocked Q-02 | FX05-S04 |
| <a id="support-emergency-end"></a>`support.emergency.end` — Kết thúc Emergency của actor | COMMAND / CONTROL | No | Security (Administrative) | Resolved delegated: action contract; source business rules unchanged | FX05-S05 |

## Điều kiện riêng theo operation

| Action | Guard / validation / effects | Explicit dependencies bổ sung |
| --- | --- | --- |
| `support.consent.read` | Chỉ chủ tài khoản; safe security history; Explicit access session; một module, readonly, actual actor khác target | Common + dynamic source/provider guards |
| `support.consent.grant` | Owner verified; default24h/custom/until revoke; disclosure any qualified Admin; Explicit access session; một module, readonly, actual actor khác target | Common + dynamic source/provider guards |
| `support.consent.revoke` | Owner; chặn mọi derived session/request tiếp theo dù target module đang tắt; Explicit access session; một module, readonly, actual actor khác target | Common + dynamic source/provider guards |
| `support.session.open` | Admin qualified + target-module support-read grant + owner consent hiệu lực; không impersonate; Explicit access session; một module, readonly, actual actor khác target | Common + dynamic source/provider guards |
| `support.session.end` | Actual actor hoặc owner revoke; không cần target business write permission; Explicit access session; một module, readonly, actual actor khác target | Common + dynamic source/provider guards |
| `support.emergency.open` | SuperAdmin; target/module; reason20..1000; audit+3-channel intent commit trước data; duration/recent-auth Q-02; Explicit access session; một module, readonly, actual actor khác target | Common + dynamic source/provider guards |
| `support.emergency.end` | Actual actor; không xóa audit/notification; Explicit access session; một module, readonly, actual actor khác target | Common + dynamic source/provider guards |

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
