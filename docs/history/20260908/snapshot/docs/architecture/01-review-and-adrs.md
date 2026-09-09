# Architecture fit review and decision records

Review 2026-09-07 · Baseline `b85f0f314da8ca7dcee8dad156e7b538ba52c287` · Documentation only; no schema, migrations or application code executed.

## Outcome

The earlier Modular Monolith direction is suitable; a single set of broad Application/Domain/Infrastructure assemblies with only namespace conventions is too easy to couple as modules grow. Upgrade to **module-oriented packages with public Contracts and internal implementation**, keeping one deployable API/worker topology initially. This is a technical design resolution under delegated architecture authority, not a move to microservices or permission to implement.

| ADR | Baseline / gap | Decision and consequence | Status |
| --- | --- | --- | --- |
| ADR-D01 | Layer-oriented namespaces do not enforce module ownership | Module owns Domain/Application/Infrastructure and DbContext; small modules may one internal implementation assembly plus Contracts, dependency tests enforce boundaries. More projects/test setup, less accidental schema coupling. | Resolved delegated — technical |
| ADR-D02 | SQL datetime/JSON/money conventions not exact | datetime2 UTC, date-only separate; rowversion for mutable concurrency; typed tables; versioned JSON only designated snapshots/config. Decimal28,8 storage proposal, Q-05 still controls money semantics. | Technical; financial behavior blocked Q-05 |
| ADR-D03 | Resource registry conceptual only | Minimal registry identity/lifecycle + typed domain payload and same-owner FKs; no generic EAV. Core references allowed, domain cross-links through contracts. | Resolved delegated — technical |
| ADR-D04 | Atomicity across module DbContexts unspecified | Shared local SQL connection/transaction for required participants; network work via outbox. Source command owns idempotency/history/registry atomicity. | Resolved delegated — technical |
| ADR-D05 | Task Calendar copy versus source projection ambiguous | Task provider emits live readonly Calendar projection; independent ManualEvent separate. Stable opaque ICS UID per source. No mirror task-event table. | Resolved delegated — technical |
| ADR-D06 | Trash/Archive restore cohorts underspecified | Separate immutable membership batches for deletion and Document Archive; original states, file pins and parent gates retained. | Resolved delegated — technical |
| ADR-D07 | Generic Admin data access suggested by UX matrix | AccessContext explicit Self/SharedLink/Support/Emergency. Support one module readonly; emergency reason/audit/intent before read. No ambient impersonation or Vault decrypt. | Confirmed product rules + technical enforcement |
| ADR-D08 | Redis and SQL authority potentially overlap | SQL authoritative for session/grant/job/idempotency; Redis rebuildable cache only; no stale positive authorization. | Resolved delegated — technical |
| ADR-D09 | Pre/post verification provisioning inconsistent | Pending User row/token first; activation transaction creates PersonalSpace and default module snapshot exactly once. Before verification UI only account recovery/verification. | Resolved delegated — technical; user-visible email gate confirmed |
| ADR-D10 | UI routes treated as implementation contract | UX route proposals plus application command/query names are design only; endpoint OpenAPI comes with approved implementation stories. No pretend existing endpoints. | Resolved delegated — technical |
| ADR-D11 | All feature catalog entries assumed Ready | Conditional Finance/Vault/provider/egress/recovery workflows stay blocked on Q decisions; stable core UX/data detail may be refined independently. | Governance, not permission to code |


## Alternatives deliberately not selected

- Microservices/database-per-module now: adds distributed consistency/operations before proven need; extension contracts retain future extraction path.
- One generic universal-content table: weakens foreign-key and financial/secret/lifecycle constraints; only Documents deliberately unifies its three content types.
- Core switch statement for all40 features: makes every future module require kernel edits; use manifest and typed contribution registries.
- SQL cascading deletion across modules: loses history/file references/security evidence; explicit lifecycle orchestration instead.
- Browser localStorage for draft secrets/session bearer tokens: unsafe persistence/exposure; same-origin cookie session, memory-only sensitive drafts, no offline persistence promise.

## Remaining validation before code

No implementation exists to compile or benchmark. Validate boundaries with architecture tests, SQL invariants against actual selected SQL engine, transaction retry behavior, authenticated authorization threat tests, keyboard/mobile prototypes and provider feasibility after explicit implementation approval. Status here is evidence-backed design review, not tested runtime adequacy.
