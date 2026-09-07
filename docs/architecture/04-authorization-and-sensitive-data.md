# Authorization contexts and sensitive-data boundaries

Review 2026-09-07 · Baseline `b85f0f314da8ca7dcee8dad156e7b538ba52c287` · Documentation only; no schema, migrations or application code executed.

## Evaluation order

Authenticate current session/account → resolve explicit operating context → verify installed/system/dependency/module-user availability → action permission → resource type capability → same-owner or specific Share/Support/Emergency grant → current lifecycle → field projection → log required audit. All defaults deny. UI hiding a button is never enforcement.

| Context | Who / target | Allowed data | Denied |
| --- | --- | --- | --- |
| Self | Current verified User → own PersonalSpace, including Admin using own modules | Owner DTO and commands subject to module/lifecycle/recent-auth | Other User ownership; overriding Project terminal/Document immutable fields |
| SharedLink | Public or authenticated viewer selected by current link policy | Exact approved readonly projection; Project includes live child Task detail; Document current page only; Resume pinned version | Edit, history, audit/reasons, reminder config, automatic child Document navigation, unsupported module share |
| Support | Any currently qualified Admin + owner one-module consent + explicit session | Approved module-specific support-safe readonly projection; persistent banner | Export/download of private files by implication, mutations, impersonation, secret reveal/copy, other modules |
| Emergency | SuperAdmin + specific target/module + reason + durable audit + immediate intent | Only approved emergency-safe readonly projection; explicit stronger banner | Ambient Vault decrypt; silent access; use of failed/uncommitted audit; cross-module browsing |
| SystemJob | Trusted registered system handler, public-cache/operations scope | Only public or operational inventory declared by handler | Null-owner wildcard over personal tables or bypass through scheduler |


Support/Emergency authorization revoked/expired during view: server denies next read, UI clears protected data/context caches and replaces with ended-access state. Browser-tab cached content is not proof of ongoing access. Already viewed information or user screenshots cannot be technically recalled; do not claim screenshot prevention. Emergency User alert intent commits before access; external channel delivery latency is exposed truthfully.

## Session and CSRF proposal

Same-origin secure HttpOnly SameSite cookie backed by SQL revocation registry, antiforgery token on state-changing requests, origin checks and rate limits. OAuth/social/passkeys are not automatically added. MFA/recovery/recent-auth product policy Q-02; implementation must pin supported security libraries/hash parameters and upgrade policy in reviewed security ADR. Pending verification cannot access modules even if a cookie exists.

## Sensitive content

Vault outer data carries encrypted payload/key version and minimal type/identity, not plaintext searchable title. Recovery/operator metadata policy Q-04 must close before physical crypto/key implementation; no zero-knowledge/recoverable marketing claim. Finance amount/counterparty, asset serial, career salary/contact/interview links, integration endpoints/secrets are absent from generic previews and logs. Email/Push contain safe summary and authorized deep link, never secret/body/financial value.

Owner Reveal/Copy requires current allowed action and recent-auth policy, masked initial render, short-lived memory-only value and no secret in toast, URL, analytics, error or browser persistent storage. Clearing clipboard is best-effort browser behavior, not guaranteed erasure. File download/preview receives scoped short-lived capability; shared projection cannot generate arbitrary attachment URL.

## Network guard

All server URL fetchers (Feeds, Bookmark metadata, approved Toolbox network utilities, Webhooks, Monitoring, Digital observations) use one reviewed egress policy service: allowed schemes, public targets, DNS/IP classification and rebinding protection, redirect revalidation, port allowlist, request/response/time caps, no user-supplied credential forwarding, safe parser settings, per-owner/provider rate limits. Q-07 defines allowed effects/providers; guard alone does not approve new integrations. Never bypass CAPTCHA/login/provider controls.

## Threat-model checks

Cross-owner IDs in every read/write/list/export/file/version path; stale caches after revoke; guessed share tokens; restricted user removed mid-session; parent Trash with active child cache; support attempting secret reveal; emergency audit failure; forged webhook replay; import XML external entity and decompression bomb; crafted Markdown/Block content XSS; module-disabled queued automation; account delete/key backup residuals. Security review and runtime tests remain prerequisites, not work claimed complete in a docs-only phase.

## Normative action contract update — 2026-09-07

[Action Catalog](../action-catalog/README.md) supplies exact stable keys. [Context evaluation](../action-catalog/00-authorization-contract.md), [semantic diff/composition](../action-catalog/01-composition-and-field-guards.md), [revoke](../action-catalog/02-revocation-and-runtime.md) and [module contract](../action-catalog/04-module-action-contract.md) are required. Admin Self does not bypass an Admin Deny by retaining User role. SuperAdmin-only policy commands cannot be delegated via an Allow row. Every entry point, worker and field-diff wrapper revalidates the same source action. No reflection-based arbitrary handler dispatch, wildcard grant, automatic prerequisite grant or implementation is authorized by these documents.
