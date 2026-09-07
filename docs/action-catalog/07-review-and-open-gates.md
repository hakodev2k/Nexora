# Action catalog consistency review

2026-09-07 · Reviewed against current source commit78d8107 and direct PO constraints. Catalog has 714 operations; counts are structural coverage, not security certification.

## Findings and normalization

| Severity | Finding | Resolution / residual |
| --- | --- | --- |
| Critical | Admin Self could be interpreted as unrestricted User-role union | Explicit Admin Allow/Deny applies in Self too; updated architecture and UX pointers; runtime ACT-002 pending |
| Critical | Generic Update/Restore/Import could bypass status/reveal/purge grants | Semantic field-diff composition contract, protected operations and tests |
| High | Owner read, support read and operational read were insufficiently separated by descriptive verbs | Separate contexts/target support projection keys; no implicit export/history/secrets; sensitive support projections Blocked |
| High | New action registration could accidentally auto-grant future capabilities | New Admin action default deny; no wildcard; current manifest/version validation |
| High | Permission metadata SQL Risk accepts3 values, while UX needs richer risk labels | Explicit DetailedRisk→DbRisk mapping; no undeclared enum values/schema change |
| Medium | Read Later UX introduced Archive/Unarchive beyond source FX-23 | Removed invented queue archive surface; FX23-S03 now unavailable-source filter, same screen ID; no new lifecycle/table/action |
| Medium | UX role editor wording could imply Admin delegation, and users.manage was a descriptive placeholder | SuperAdmin-only grant mutations; canonical access.user.read replaces placeholder; key binding overrides descriptive verbs |
| Medium | Small tools risk being forced into full resource lifecycle/DB | Mandatory minimal action manifest; optional resource/search/share/jobs only when declared |
| Medium | Background/system/local operations could appear as ordinary Admin permissions | Kind/context and AdminGrantable explicitly separate; local pure tool run capability limitation disclosed |
| Low | Permission label translated or UI verb mistaken for stable key | Stable namespace.resource.verb + human label + source screen bindings |

## Remaining major gates

[Source Q log](../features/90-open-decisions.md) remains unchanged; **not closed by this catalog**. Q-01 account deletion/portability; Q-02 MFA/recovery/recent-auth; Q-03 sharing/sensitive projections; Q-04 Vault keys/recovery; Q-05 Finance semantics; Q-06 provider/price contract; Q-07 automation/egress; Q-08 capacity/backup; Q-09 locale; Q-10 productivity extensions; Q-11 formats; Q-12 Interview Calendar.

Blocked entries reserve vocabulary and guard boundaries, not activated handlers. Q gates may affect only a specific extension of otherwise usable module; do not label whole unrelated workflow Blocked. Finance/Vault/Price/Automation execution remains gated where fundamental semantics are unresolved. No new question about button placement or small interaction is needed.

## Review boundaries

Sources include40 feature documents, current database/architecture and197 screen inventory. Structural checks validate keys/links/mappings, not runtime authorization, performance, security testing or production capacity. Source BR/AC remain authoritative; action rows refine contracts rather than replace full field/state specification. The concrete module packaging-to-ModuleId manifest must be validated during later approved implementation design;40 FX namespaces do not force40 deployables.

Completion in this phase means a reviewable action vocabulary, context/grant model, semantic guards, screen bindings and verification backlog. It does not mean all major product decisions resolved or code approved.
