# Trusted module SDK — action catalog and small features

> **Current decision amendment — 2026-09-07:** User.IsDeleted is a current authority gate; Account/Vault never purge; permanent sharing invalidation; explicit SuperAdmin RECOVERY mode added; Paused product scope outranks grant. Read ../requirements/10-owner-decisions-20260907.md and current catalog before old Q references. [Normative PO decisions](../requirements/10-owner-decisions-20260907.md). Conflicting older proposal paragraphs below are historical; current field/action overrides are in the linked delta. Docs-only.

## Required manifest contract (documentation, not code)

| Field | Type / validation | Meaning |
| --- | --- | --- |
| ActionKey | lowercase ASCII string≤150, dot-separated namespace.resource.verb; unique globally | Stable semantic identifier, never translated/reused for broader access |
| ModuleKey / binding | existing registered installed module key/ID | Owning entitlement and system enablement boundary |
| ContractVersion | positive integer | Version of action semantics/guard/projection |
| Label / description | localized labels; persisted description≤500 | UI name independent of immutable key |
| Kind | QUERY / COMMAND / COMPOSITE / LOCAL / WORKER | Processing and enforcement boundary |
| Contexts | nonempty declared allowlist matching catalog audience rules | No default any-context |
| AdminGrantable | boolean, default false | Only safe declared eligible operation; cannot grant SUPER/SYSTEM/CONTROL/LINK authority |
| Risk / DbRisk | detailed classification + Normal/Sensitive/Administrative mapping | Disclosure/secret/network/destructive warnings without SQL enum drift |
| GuardContract | registered versioned handler policy reference | Owner query, lifecycle, protected fields, recent-auth and conditional projection |
| Prerequisites | explicit stable keys + declared dynamic resolver | Required in addition, never inherited grant |
| ProductGate | resolved/delegated or Q-linked Blocked | Metadata cannot approve a major decision |
| Effects | read/write fields, state transition, events/jobs/files/egress, sensitivity | Diff authorization and retries inspect true effects, not just button label |
| Retry / concurrency | idempotency scope, conflict policy, safe cancellation checkpoint | No global blind retries |
| UI bindings | existing Screen IDs and optional contribution entry | No orphan command or hidden unapproved route |

## Minimal module versus optional capabilities

Every module, including a small JSON formatter, needs stable manifest identity, action registry, enablement, current capability checks, trusted packaging/version and contract tests. A pure local tool does **not** need arbitrary database tables, migrations, ResourceType, Trash, background jobs or sharing.

| Optional contribution | Required only when declared |
| --- | --- |
| Persistent owner resource | ResourceType + typed own schema/provider + owner/lifecycle/read/write actions + migrations/concurrency |
| Search/Favorites/Dashboard | Safe source-read provider/projection, revoke invalidation, disabled fallback |
| Files/Tags/Templates | Typed reference/namespace/cardinality/validation and actual source action composition |
| Sharing | Explicit approved projection, source share action, eligible states and current-link checks; no automatic inherited sharing |
| Support/Emergency | Separate safe projection/action; disabled by default if absent/blocked; not full owner DTO |
| History/Trash | Declared retention, cohort/parent/pin dependency, source actions; generic UI cannot invent lifecycle |
| Reminder/Jobs | Source revision, idempotency, stop/disable checkpoints, redacted operation visibility |
| Automation/API/Webhooks | Explicit exposed subset, current owner effects/egress gate; no expose-all reflection |

A domain can have many small actions without becoming many modules. Example Toolbox local JSON and network HTTP have different action contracts and risk. Their shared installed module still supplies common entitlement; separate action denies for Admin work within it. New future domain adds registered action keys/provider contracts, not new User columns or a central switch statement enumerating every resource type.

## Evolution and deployment

1. Developer adds trusted definitions and tests; no user/admin executable upload.
2. Registration validates global key uniqueness, actual ModuleId binding, guard/context, prerequisites and source-provider availability. Cyclic static authorization prerequisites are invalid; composition invokes source without recursive authorization loop.
3. Deploy compatibility review detects scope expansion, rename/removal and pending Q gates. Unknown keys deny; required dependency failure keeps module unavailable.
4. Metadata reconciliation never creates Admin Allow. Minor label changes retain key/id; new semantic scope requires new key or explicitly approved migration. Deprecated old grants do not transfer silently to replacement.
5. Manifest/catalog hash and version match deployment; missing registry entries fail readiness. Old app nodes must not execute newly registered semantic contracts until compatible.

40 FX namespaces are not40 fixed code assemblies or microservices. Concrete packaging can group related features while keeping stable authorization boundaries; changing an existing module entitlement through regrouping requires impact review, not action-catalog inference.
