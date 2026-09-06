# Nexora UX/UI Blueprint

**Status:** UX/UI design documentation — not implementation approval  
**Repository baseline:** `89198351a3d6cf937179d234a0f16e8cf8c259d7`  
**Research snapshot:** 2026-09-07

## Purpose
This directory is the UX/UI behavior specification layer for Nexora. It converts existing requirements and feature behavior into a coherent application shell, interaction model, screen inventory, responsive behavior, accessibility requirements, and module-level UI specifications.

Source inputs remain read-only:
- `docs/requirements/**`
- `docs/features/**`
- `docs/roadmap/**`

## Authority order
1. Latest explicit Product Owner decision.
2. Approved Nexora requirements.
3. Resolved delegated decisions.
4. `docs/features/**`.
5. `docs/roadmap/**`.
6. External reference products.

External products never override Nexora behavior.

## Map
- `global/` — shared UX architecture and patterns.
- `modules/` — FX-01 through FX-40.
- `references/` — product references and behavior patterns.
- `decisions/` — delegated decisions and major open UX questions.
- `audits/` — consistency findings.

## Status vocabulary
| Status | Meaning |
|---|---|
| Approved | Supported by approved Nexora requirement/decision. |
| Resolved delegated | Small UX choice resolved within delegated authority. |
| Proposed | Recommended design that depends on a major decision. |
| Blocked | Cannot be finalized safely until a named decision closes. |
| Technical ADR | Implementation detail UX depends on. |
| Future / Out of scope | Useful pattern intentionally excluded from R1. |

## Core UX thesis
Nexora should feel like one personal operating system, not forty independent SaaS products. Navigation, page headers, data lists, lifecycle actions, destructive language, search, dialogs, feedback, responsive adaptation and accessibility are shared.

Frontend should not invent a new interaction pattern when a documented global pattern applies.