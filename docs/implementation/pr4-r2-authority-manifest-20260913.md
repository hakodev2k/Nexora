# PR #4 R2 authority manifest — current review

> This is an implementation/evidence artifact on `impl/m01-s00-scaffold`.
> It does not amend a requirement, approve a future slice, or grant merge,
> deployment, provider or production authority.

## Pinned review state

| Item | Value | Evidence rule |
| --- | --- | --- |
| Repository / PR | `hakodev2k/Nexora` / PR `#4` | Existing PR branch only |
| Implementation branch | `impl/m01-s00-scaffold` | No merge or push to `main` |
| Normative base | `main` = `8782f46be51f4b0f3cb54f0f0f7a06eae3b0d3e3` | Read with `git show main:<path>` |
| Actual reviewed head | `6d5b58dbac330b0d0d9aceb9e0b2f4695b423316` | Matches the requested R2 head |
| Parent of reviewed head | `fcb1f75fd8dccfa0ba1f15b49569e04c694569ce` | Historical comparison only |
| Working tree | Implementation changes are uncommitted on the reviewed head | Uncommitted diff is not a commit/CI result |
| Fetch | `origin/main` and `origin/impl/m01-s00-scaffold` were fetched and matched the pinned refs | Fetch needed elevated repository metadata access; no source was reset or overwritten |

## Authority used

The following were read from the pinned `main` ref before affected work. The
review findings and PR implementation documents were used as claims to verify,
not as product authority.

- `AGENTS.md`, `.ai/profiles/nexora-implementation-agent.md`, the Technical
  Lead role/rules, and routed backend/frontend/security/database/verification
  rules.
- `docs/README.md`, `docs/delivery/README.md`,
  `docs/delivery/01-current-scope.md`.
- `docs/requirements/11-owner-decisions-20260909-implementation-readiness.md`
  and the current decisions it references.
- The complete `docs/delivery/milestone-01/` package: stories, API, data and
  transaction, UX/acceptance, environment/runbook, readiness/evidence,
  handoff and OpenAPI.
- `docs/action-catalog/00-authorization-contract.md`,
  `docs/action-catalog/04-module-action-contract.md`,
  `docs/action-catalog/09-effective-implementation-status-20260909.md`,
  `docs/action-catalog/catalog.csv`, and affected module action contracts.
- `docs/goals/README.md`, `docs/goals/01-system-goals.md`,
  `docs/goals/03-delivery-step-goals.md`,
  `docs/goals/04-cross-module-verification.md`,
  `docs/goals/05-task-and-evidence-template.md`, and relevant module goals.
- Main feature/UX/database contracts for Identity, Access, Modules,
  Notifications, Audit, Settings and App Shell. Post-M01 feature contracts
  were read when mapping a finding, but their presence does not approve code.

## Approval and scope decision

The current main decision is `DEC-20260909-001`: M01 stories `S00`–`S11`, the
backend/frontend scaffold required by M01 and local development scripts are
approved to implement. The effective M01 goal bindings are:

| Goal | Approved outcome |
| --- | --- |
| `NXG-M01-01` | S00–S01 toolchain and safe SuperAdmin bootstrap |
| `NXG-M01-02` | S02–S04 registration, verification, login and password-only reset |
| `NXG-M01-03` | S05–S06 profile, locale/timezone and own-session security |
| `NXG-M01-04` | S07–S09 metadata access, explicit grants and module policy |
| `NXG-M01-05` | S10–S11 durable security delivery foundation and clean-checkout evidence |

The system outcomes used for the changes are `NXG-SYS-01`, `NXG-SYS-03`,
`NXG-SYS-05`, `NXG-SYS-06`, `NXG-SYS-08`, `NXG-SYS-10`, `NXG-SYS-11`,
`NXG-SYS-13`, `NXG-SYS-14` and `NXG-SYS-16`. Relevant module goal bindings are
`NXG-FX01-G01..G03`, `NXG-FX02-G01..G03`, `NXG-FX03-G01..G03`,
`NXG-FX06-G01..G03` and `NXG-FX09-G01..G03`.

The implementation does not use the PR-only `DEC-20260909-014`. It does not
approve full Phase 1/R1, FX04/05/07/11–40, real providers, OAuth, outbound
workers, paid services, production data/secrets or public launch. Existing
post-M01 code is reviewed and dispositioned, but it remains outside the
implementation boundary unless a later Product Owner decision approves the
exact affected vertical slice and its contracts.

## M01 policy points applied in source

- `OwnerId` remains the `PersonalSpaceId`; `UserId` remains the actor identity.
- User and SuperAdmin own-resource baseline is distinct from Admin SELF.
  Admin SELF requires an explicit action-catalog allow, no explicit deny and
  all current module/dependency/account/space prerequisites.
- `RegistrationEnabled` is used only when taking a future verification grant
  snapshot. It is not a current-user capability gate.
- Public receipts use a keyed digest of a separately signed, validated opaque
  anonymous-session binding paired with the CSRF check. The API signing secret
  is configured separately and kept stable across restart, so CSRF rotation
  does not silently create a new receipt namespace. The raw cookie, binding,
  token, password and receipt secret are not stored or logged.
- M01 admin mutations store only allowlisted metadata projections for safe
  replay. A self-demotion returns `204` and clears the privileged UI state;
  it never returns a pre-change admin projection after authority is revoked.
- Registration and reset create a durable encrypted delivery envelope in the
  same SQL transaction as the token intent. A bounded local worker decrypts
  only for the local capture transport, rechecks the token/account and the
  active PersonalSpace for reset delivery; the API has no token inbox endpoint.
- New passwords use the versioned ASP.NET Identity `PasswordHasher` with the
  approved PBKDF2-HMAC-SHA512 cost. A bounded parser exists only to rehash
  hashes from the pre-review local format after successful verification.
- The shell applies the persisted theme and the M01 authentication/profile/
  settings vocabulary has vi/en resources. Untranslated post-M01 screens are
  not represented as completed locale scope.

## Unresolved gates

- SQL Server empty/upgrade/concurrency/receipt/lease/recovery verification,
  browser QA, synthetic-data functional QA and clean-checkout restore were not
  run in this code-only implementation pass.
- CI evidence for the uncommitted working tree does not exist. Baseline CI is
  not evidence for this source state.
- Independent review of authorization, secret delivery, migration and
  background changes is **Pending**; this self-review cannot satisfy it.
- All R2 items tied to post-M01 business modules remain scope-blocked in the
  companion report, even where the current PR contains implementation code.
