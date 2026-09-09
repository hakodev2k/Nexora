---
name: nexora-engineering
description: Apply Nexora repository engineering rules when reviewing, planning, implementing, testing, or changing this repository and its agent configuration. Load before task work and route to the relevant vendored skills.
---

# Nexora engineering

Paths below are relative to the repository root, not this skill directory.

1. Read root `AGENTS.md`, `.ai/profiles/nexora-implementation-agent.md`,
   `.ai/roles/technical-lead/README.md`, and its `rules/core-rules.md`.
2. Record the user's existing authorization, scope, source revision, applicable
   delivery contracts and acceptance IDs. Review authorization is not application
   implementation authorization. Follow current PO decisions and unresolved gates.
3. Load the matching paths from `.ai/routing.json`; combine routes for mixed work.
   Read the files, not only their names. Missing mandatory paths block affected work.
4. Plan and perform only authorized work. Preserve owner boundaries and current
   milestone exclusions. Upstream examples do not approve tools or delegation.
5. Follow `.ai/verification.md`. Attach actual commands, results, source revision,
   evidence references and pending checks. Manifest gates do not execute app tests.
6. Before handoff run the baseline verifier when changing agent configuration;
   distinguish completed changes, executed checks, and pending independent review.

If the host does not discover `.agents/skills`, manually load this file through
the root AGENTS instructions. Do not claim native discovery on an untested host.
