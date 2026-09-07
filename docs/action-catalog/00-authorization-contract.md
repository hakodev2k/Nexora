# Authorization contract — contexts, grants, scope

Status: Resolved delegated / technical design. PO-approved personal-only/module/Admin policy remains source of truth. Action registration does not approve implementation or close [Q-01…12](../features/90-open-decisions.md).

## Distinct contexts

| Catalog audience | Who / source of authority | Scope and mandatory constraints |
| --- | --- | --- |
| SELF | Ordinary User: approved owner action baseline of enabled module. Admin Self: module enabled **and explicit Allow on grantable action**, no Deny. SuperAdmin Self: own-resource baseline, no other-user access | Verified active principal; own PersonalSpace; installed/system/user enabled; dependencies; action-specific lifecycle/current version/recent-auth |
| ADMIN | Admin with explicit operational action Allow; SuperAdmin operating context | Only declared operational metadata or guarded job operation, not private resource payload |
| SUPER | Current SuperAdmin, fixed privileged policy | Role/module/action grants, system policy, emergency initiation; Admin Allow cannot manufacture this context |
| CONTROL | Principal/session/consent proof for fixed own-account safety controls | Logout, revoke own sessions/consent, end actual support/emergency session remain possible when business module off. Profile/security proof flows do not read business data. No generic bypass |
| PUBLIC | Specific registration/login/token handler | No module data; throttle, generic account responses, one-time token, proof checks. Verify before module access |
| LINK | Explicit read-only Sharing resolver | Token, audience mode, authentication/allowlist, expiry/revoke and source live eligibility/projection; not owner read; viewer entitlement not borrowed |
| SUPPORT | Qualified Admin + support.session.open + target namespace.support.read + owner consent/session; or SuperAdmin explicit Emergency session | Target module only, approved safe projection; reject export, mutation, history, secret reveal/copy and linked-module expansion |
| SYSTEM | Registered deployment/worker/adapter identity and trusted invocation | Checks original initiating owner authority and current source/target policy; not a role users can acquire |

For LOCAL operations, capability determines availability in the Nexora UI. Pure tool run keys may be Admin-grantable, but cannot prevent an individual implementing a public algorithm elsewhere. Copy/filter/layout is not permission to fetch extra data. Client rendering is never the authorization boundary for backend data.

## Effective decision order

1. Resolve trusted actor and operating context; never accept Role/OwnerId/access-mode claims as authority from request body.
2. Resolve installed action from current trusted manifest. Unknown/deprecated-unrouted action or unresolved product gate → deny before side effects.
3. Check active account/proof and context capability. Ordinary Admin cannot switch to User context to bypass a denied action; multiple roles do not union into broader Self access. Explicit Support/Emergency context cannot call Owner DTO handlers.
4. Evaluate installed/system enabled/dependencies and target owner's enabled module(s); fixed control-plane safety exception is an explicit allowlist, never arbitrary disabled-module reading.
5. For Admin-grantable operation in Admin contexts: explicit Deny wins; absent = deny; Allow does not imply prerequisites. SuperAdmin-only operation cannot be delegated. SuperAdmin role never supplies other-user owner scope.
6. Load owner-scoped target/projection, then resource lifecycle, parent state, field classification, consent/link scope and recent-auth as relevant. Avoid existence leaks for wrong owner before producing detailed denial.
7. Resolve all static AND dynamic prerequisite actions. Wrapper permission is not target permission. Check protected-field diff including changes induced by normalization/template/import/restore.
8. Recheck current policy and resource revision before commit/side effect; audit according to source. No authorization from stale UI capability.

Rows called QUERY still check scope and projection before counts/results. Resources in Trash/Archived/terminal obey their explicit source action matrix; read does not mean current writable object. Shared read is a separate projection, not Owner read with buttons hidden.

**Mutation responses are not a read bypass.** A principal allowed to create/update/transition but denied source read receives only the minimal command acknowledgement (operation outcome and authorized opaque reference), never a full resource DTO, previous values, history or validation echo containing protected data. Returning a detail/read model requires its current read/projection authorization separately. Error/conflict payloads follow the same rule. Key suffix alone never determines semantics: Kind, context and registered handler are authoritative; no auto-routing or permission inference from a verb string.

## Defaults and administration

- Verified User creation snapshots current registration policy; current R1 starts all declared modules enabled. Changing defaults affects later verification/provisioning, not existing accounts retroactively.
- Admin action defaults **deny**, including newly deployed actions. User does not acquire an action-level configuration UI. SuperAdmin preview can select named action sets, expanded to exact keys; no persisted wildcard, no future auto-grant.
- Non-grantable PUBLIC/CONTROL/SUPER/SYSTEM/LINK operations appear in technical catalog but never as grantable checkbox. LOCAL display controls use owning/source capability; pure tool run exceptions are labelled explicitly.
- Admin allowed to view user metadata is not allowed to edit role/grants or read their Projects. No impersonation switch; support is explicit target session.
- “Blocked” is stronger than Allow; configuration cannot approve Q semantics. Contract key may be reserved while unavailable.

## Evidence and limits

Default-deny, least privilege and authorization on every request follow [OWASP Authorization Cheat Sheet](https://cheatsheetseries.owasp.org/cheatsheets/Authorization_Cheat_Sheet.html). Resource-dependent checks align with [ASP.NET Core resource-based authorization](https://learn.microsoft.com/en-us/aspnet/core/security/authorization/resource-based?view=aspnetcore-10.0). Consulted2026-09-07; official documentation, no runtime test. These sources inform technical patterns, not Nexora business approvals or package/version choices.
