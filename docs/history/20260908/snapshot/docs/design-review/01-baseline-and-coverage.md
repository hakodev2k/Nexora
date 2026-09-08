# Baseline, scope and coverage

Review 2026-09-07 · Baseline `b85f0f314da8ca7dcee8dad156e7b538ba52c287` · Documentation only; no schema, migrations or application code executed.

## Evidence inspected

Frozen GitHub main baseline contains140blobs,138Markdown files under docs:18requirements,47features,11roadmap,61UX/UI and docs README. File contents were retrieved at exact commit and byte hashes checked before authoring; no application source/AGENTS/schema/migrations existed in that baseline. All40FX source specifications and shared rules govern this design, not historic Workspace/collaboration decisions superseded by personal-only PO direction.

Priority: latest PO → approved requirements → resolved delegated decisions → current feature specs → solution/data/UX design consistent with them → historical roadmap → external references. No historical OPEN marker reopens an already delegated minor decision; unresolved financial/security/provider decisions retain Q gates.

## Coverage delivered

| Layer | Coverage | Limit |
| --- | --- | --- |
| Database | 181 proposed tables with expanded fields/types/nullability/keys/references, scoped ERDs and payload/classification contracts | Conditional Q branches explicitly marked, SQL constraints/migrations not executed |
| Architecture | 6design documents + ADR fit review + roadmap pointers | No application code exists to compile/test |
| UX/UI | 40module specs,24sections each,197 Screen IDs, common profiles/global design language | Text specifications, not rendered wireframes/usability certification |
| Research | 41 official reference entries plus technical/accessibility sources | Public text/excerpts, not authenticated product behavior test |
| Consistency | Source→data→commands→screens→state/dialog/acceptance routing | Coverage counts do not prove all business decisions closed |


## New module extensibility proof

[VehicleMaintenance hypothetical exercise](../design-database/12-evolution-and-recovery.md) proves the documented extension path without changing existing User/Task/Page/Finance tables. It is not a new committed module. New module owns schema/migrations/contracts and registers resource/contribution metadata; no EAV, executable plugin marketplace or no-code schema creation.

## Ready versus blocked

Core personal ownership/lifecycle/Task/Document scheduling and routine screens can be refined into implementation stories after architecture/security review and explicit code approval. Q-dependent financial semantics, Vault recovery, sensitive sharing, provider feasibility, data egress, capacity/recovery targets, account purge and Interview Calendar authority remain blocked for affected stories.
