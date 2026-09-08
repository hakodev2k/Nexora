# UX-12 — Security and sensitive-data UX

> **Current decision amendment — 2026-09-07:** Internal-only user journeys; paused module navigation removed; Account/Vault deletion exceptions and SuperAdmin RECOVERY context override generic purge/restore patterns. Language default vi, selectable en. External research URLs in docs are evidence, not product navigation. [Normative PO decisions](../../requirements/10-owner-decisions-20260907.md). Conflicting older proposal paragraphs below are historical; current field/action overrides are in the linked delta. Docs-only.

Review 2026-09-07 · Baseline `b85f0f314da8ca7dcee8dad156e7b538ba52c287` · Documentation only; no schema, migrations or application code executed.

Sensitive data: Vault payload, Finance values/counterparties, Asset serial/invoices, Career salary/contact/interview URLs, certification IDs, connection endpoints/credentials/webhook secrets. Default safe projection never means whole entity with hidden CSS fields. Mask sensitive fields by default where applicable; owner Reveal/Copy explicit and current authorization/recent-auth policy. No secret in URL, toast, analytics, error, search preview or browser persistent storage.

Reveal describes that screen is visible to anyone nearby; no false screenshot prevention. Copy confirmation says Copied, not value. Clipboard clear best effort if browser allows; cannot guarantee removal from OS history. On lock/logout/role revoke/expired support context clear plaintext memory/rendered content and relevant query cache, not merely disable buttons over visible secret.

Provider/import content untrusted: Markdown/HTML sanitized or escaped, no arbitrary scripts/iframes/executable snippets. Upload scan pending/quarantined blocks preview/download/attach. External links validated and isolated opener; QR preview does not auto-navigate. Network tools show execution location/data egress before Run and remain blocked under Q-07 where unapproved.

Share disclosure previews exact resource fields/audience/expiry; no Project Task hiding, no Document-child automatic inclusion. Public invalid/unauthorized link uses generic unavailable without resource title; owner manage links may see precise reason. Sensitive module projections Q-03/Q-04 explicitly Blocked, no inferred Admin/private file download.

Login/register/reset do not enumerate account existence; tokens not logged or retained in browser history beyond security-handled landing. Account delete grace/export/recovery Q-01, MFA/recent-auth Q-02, Vault recovery Q-04 remain major decisions. SecurityCenter and Audit clearly separate actor/target/context; personal-only does not eliminate cross-user threat boundaries.
