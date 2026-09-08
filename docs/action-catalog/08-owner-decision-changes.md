# Action catalog change log — v1 → v1.1

[PO source](../requirements/10-owner-decisions-20260907.md), [current Q status](../features/90-open-decisions.md), [database delta](../design-database/17-owner-decision-delta.md). Prior714/161 counts belong to v1 snapshot; current counts are in README.

| Area | Current action effect | Superseded assumption |
| --- | --- | --- |
| Account | identity.account.soft_delete; retain data/revoke authority | delete_request grace/purge proposal retired |
| Sharing | Permanent token invalidation on sharing off/source delete; epoch + tombstone | No SuspendedByTrash revival or re-enable existing link |
| Vault | Soft delete retained encrypted values; request→SuperAdmin review/authorize→trusted recovery | Owner item.restore/restore_version/purge retired; no automatic grant migration |
| Finance | ManualCategory/ManualRecord/read/create/update + same-currency summary | Existing advanced ledger actions remain blocked, not silently approved or canceled |
| Price / Automation / Integrations | Paused product scope, no user handler/worker/auto-enable | All-modules-on registration cannot override pause |
| Language | vi default, en via Settings | No browser-language override or inferred currency/timezone |
| Formats | DOCX/MD internal preview/manual Save, bounded supported subset | PDF/HTML product export not enabled |
| Career / Calendar | Own Personal Event create/link/unlink; one notification source | Standalone Interview/provider/conference workflow retired |
| Internal navigation | External-open action retired; backend ingestion scope still held | No automatic external URL navigation/fetch/OAuth |

Technical action keys split irreversible/security scope; metadata labels never grant rights. New RECOVERY context requires current SuperAdmin, exact owner/request/kind, reason/proof/recent-auth and durable audit+notification intents. Ordinary Admin/Support/Emergency cannot use recovery mutation. Root key availability is required; permission cannot recreate lost keys.

Account deletion and Vault deletion do not impose global soft-delete on other modules. Generic Trash/purge wrappers must deny Vault purge and delegate restoration only to Recovery; Account restore remains unapproved. Updated status classification prevents treating all residual Q questions as either completely closed or completely open.

## Added acceptance scenarios

ACPO-01: IsDeleted account blocks old session, share token, password-reset login and worker effect while retaining data. ACPO-02: sharing off/on or source delete/restore never revives old token, including restored backup. ACPO-03: Vault purge/old owner restore keys deny; valid SuperAdmin request restores only target with no plaintext response. ACPO-04: optional MFA does not imply password reset disables enabled factor. ACPO-05: paused action denies despite Allow/defaults; reminders/core delivery continue. ACPO-06: vi/en change preserves user content/currency/time. ACPO-07: DOCX external relationship never fetched, unsupported format preview explicit. ACPO-08: internal Calendar link has one event/reminder, no external conference. ACPO-09: basic money Save touches no account/transaction legs/interest.
