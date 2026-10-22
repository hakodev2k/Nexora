# Windows capture ACL operator helper and worker handoff

Task owner: Windows ACL worker for PR4. Base branch `impl/m01-s00-scaffold`,
base revision `33e68006efdc75ed86551679aaf7fa6ccd8b3a5a`.
Task start: 2026-10-21 13:49:44 UTC. Final build/custom-runner checks completed before
handoff packaging at 2026-10-21 14:01:52 UTC.

Authorization: explicit current task permits synthetic local QA and ephemeral
principals, superseding the older code-only amendment for this task. D01-D09=A
apply. No push, merge, provider execution or production changes. AGENTS unchanged.
All edits are in the worker's separate temporary clone; no product/backend edits.

## Run on disposable elevated Windows

From an elevated 64-bit Windows terminal with .NET 10 and local account management
available (including Secondary Logon / credentialed process creation):

```powershell
./scripts/dev/test-filesystem.ps1 -RequireOs windows
```

The existing `filesystem-windows-acl` CI job already runs this entry point, so no
workflow changes are necessary. This is a required executable gate, not an opt-in
skip. Missing real administrator capability fails it. Do not change ACL policy,
UAC, local security/logon policy or reuse existing people/service accounts to pass.
An account in Administrators with a filtered medium-integrity token is insufficient.
The helper does not request UAC elevation or install/change any system service.

After building, the principal helper can also be invoked directly:

```powershell
./scripts/dev/test-filesystem-windows-principals.ps1 -ProbeDirectory ./tests/Nexora.UnitTests/bin/Release/net10.0
```

The helper creates three uniquely named temporary standard users, resolves their
actual SIDs and launches the built test executable under their credentials. It
stages binaries in a fresh `ProgramData/NexoraAcl-<GUID>` directory because arbitrary
new users must not be granted access to a developer's private source/profile.
Only A can write the synthetic workspace. The actual sink, without modification,
creates its protected capture ACL with exactly A+B. The controller is not added.
Passwords are randomly generated and held only in process memory/PSCredential;
no passwords, captured tokens or capture bodies are printed. No profile loading
is requested and the helper does not modify machine execution policy.

Each child checks `WindowsIdentity.GetCurrent().User`, standard-user status and
its expected role, and emits only SID, PID, operation and pass status. The parent
requires the attested PID to match the process it launched. CLI SID arguments are
expectations, never evidence on their own. Sequence:

1. A prepares/promotes one synthetic capture; file/directory ACL is exactly A+B.
2. B reconstructs the same sink policy and reads the expected capture.
3. C is denied direct OS read, enumeration and write, and denied sink/reader use.
4. Fresh A and B processes reconstruct/read the original capture without ACL drift.
5. A removes its own capture directory using its actual credential. The controller
   never broadens the protected capture ACL to perform cleanup.

Children have 45-second deadlines. Finally stops only recorded children, attempts
capture cleanup before removing users, validates the absolute unique root and
rejects reparse points before recursive deletion. User deletion uses the exact
created SID and checks the name still matches. Cleanup errors fail the gate and
identify retained synthetic paths/accounts. No existing user is removed. Account
expiration is a two-hour fallback, not a substitute for successful removal.
If cleanup fails, investigate the explicitly reported unique root/SID in an
administrator terminal; do not run broad wildcard account/filesystem cleanup.
The gate must remain failed until the residue is resolved and a fresh run passes.

## Source and acceptance binding

| Goal | Current source/story/action | Assertion and status |
| --- | --- | --- |
| NXG-SYS-08, NXG-SYS-03 | M01-S10 / M01-AC10 transport protection; LOCAL sink publish/promote/read (no new application action); current sink ACL and R4 stable A+B operator contract | B reads; C denied by OS and caller validation; exact protected A+B ACL. Real A/B/C execution BLOCKED. |
| NXG-FX06-G02, NXG-SYS-06 | M01-S10 local capture restart subset; not complete durable-job acceptance | New A/B processes preserve original message and policy. Real A/B/C execution BLOCKED. |
| NXG-SYS-14 | D09 release contract P0 Windows A/B/C, NQA-019; M01-S11 / AC11 evidence protocol | Missing admin fails, never skip/pass; no Windows/PR/R1 acceptance claim. |

Sources read include root AGENTS; engineering SKILL; implementation profile;
Technical Lead README/core; routing/verification; baseline docs/current scope;
current DEC014 and D01-D09 register/contract index; D09 release contract; system,
phase, identity/notification goals and evidence template; M01 environment,
readiness/story contracts; local-operator runbook; R4 stable-runtime/operator ACL
contract; PR4 completion plan. Routing selections: `.ai/rules/security-engineer/*`
(data-protection, identity-access, secrets-management, threat-modeling), privacy
access-control rules, secrets-key-management/security-code-review/threat-modeling
skills; QA contract-testing/test-strategy; ground-truth completion README/rules/
evidence-grounded-verification skill. No agent configuration was changed.

## Executed facts

Windows SDK: 10.0.302. Initial build failed reading inaccessible user NuGet.Config.
A second restore named a nonexistent repo config and failed. An explicit config
alone still hit user-config access. Isolated APPDATA removed that access failure;
network restore then failed NU1801 and was interrupted. Offline restore succeeded
from the existing `C:/Users/hakodev/.nuget/packages` cache with empty package sources
and `-p:NuGetAudit=false`. This was compilation preparation, not a vulnerability
assessment; no repository dependency/lock/config changes were made.

Final source commands/results:

- `dotnet build tests/Nexora.UnitTests/Nexora.UnitTests.csproj --configuration Release --no-restore --nologo -v minimal`: exit0, 0 warnings/errors.
- `./tests/Nexora.UnitTests/bin/Release/net10.0/Nexora.UnitTests.exe --require-os windows`: exit1; discovered34, passed30, failed1, skipped3 Linux-only. The one failure is the real A/B/C gate: actual controller not elevated; no ephemeral principals created. Log: sibling temp file `nexora-windows-acl-final-run.log`.
- PowerShell AST parsing of both filesystem scripts: 0 parser errors; `git diff --check`: exit0.
- Direct helper preflight under sandbox: actual SID ending1009, administrator false, exit1 before mutation.
- Read-only `require_escalated` host-token inspection: succeeded, SID ending1001, medium integrity, Administrators deny-only, administrator false. No approval rejection.
- Direct helper under `require_escalated`: exit1, administrator false, no principal/root creation. Started2026-10-21T13:59:09.9453861Z; ended2026-10-21T13:59:09.9860903Z.
- Earlier direct Windows PowerShell `-File` invocation also failed signature policy. No execution-policy change was attempted; the current host shell successfully executed helper preflight. Elevated hosted Windows script launch remains to be verified.

Remaining gates: real three-user process execution, cleanup success/fault paths on
an elevated disposable Windows runner, current-source hosted Windows CI and
independent security review. This worker has not triggered CI and has not pushed.
The host exposed no callable independent subagent tool to this worker; integration
owner must arrange the required review. This is code present/build checked,
NOT a passing Windows ACL gate or product acceptance. Reverting these scoped
harness/test changes rolls back code without schema/product-data impact.
