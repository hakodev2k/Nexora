# Nexora AI Engineering Workspace

This directory contains the reviewed baseline adopted from `hakodev2k/AI-Engineering` plus Nexora-specific orchestration.

## Baseline composition

- `roles/technical-lead/` — complete primary role package. The Technical Lead is the single implementation owner/coordinator.
- `rules/` — selected standalone constraints for software architecture, .NET, React, database/security/privacy.
- `skills/` — selected procedures for modular architecture, backend, frontend, database, security and QA.
- `controls/agent-multi-tenant-data-boundary-gate/` — complete executable gate for Nexora's highest-risk invariant: personal-data isolation.
- `guards/agent-ground-truth-completion-gate/` — complete verification guard that requires current evidence before a completion claim.
- `profiles/nexora-implementation-agent.md` — task activation matrix and phase-on-demand controls.
- `adoption.yaml` — provenance and selection record.

Repository-root `AGENTS.md` defines source precedence, Nexora invariants, approval boundaries and execution workflow. The copied assets guide implementation; they do not override requirements or grant external permissions.

## Loading policy

Do not load every file for every task. Start with `AGENTS.md`, this README, the Technical Lead role README, and the exact Nexora requirement/feature/UX source. Then load only the specialist Rule/Skill required by the task.

Additional upstream gates named in the profile are **phase-on-demand**: evaluate and copy their complete package only when Nexora reaches a change that needs them.
