# M01-S00 implementation evidence

Status: `SLICE_IMPLEMENTATION_STARTED`, not `SLICE_VERIFIED_LOCALLY`.

## Authorization and scope

- Product Owner request: implement Nexora according to current docs and continue by dependency.
- Current repository authority: `DEC-20260909-001` approves M01 stories S00-S11 plus backend/frontend scaffold and local scripts.
- This change implements only the first coherent S00 scaffold package and does not implement S01-S11, business modules, paused modules, production deployment, provider calls, production secrets or production data.

## Goal and requirement trace

| Goal/source | Bound implementation |
| --- | --- |
| M01-S00 / M01-AC00 | Pinned toolchain files, reproducible local scripts, Docker Compose local SQL/Redis profile, application CI scaffold. |
| M01 handoff | Backend scaffold, React scaffold and local scripts required before later M01 stories. |
| Effective action status overlay | Only `getCsrf` M01 control endpoint is exposed; no non-M01 action handlers are added. |

## Verification performed while preparing this branch

Environment available to the agent did not include the .NET SDK or outbound Git clone. Therefore .NET build, restore, migration and SQL integration tests were not run locally.

Observed tool versions in the agent execution environment:

```text
node --version => v22.16.0
npm --version => 10.9.2
python3 --version => Python 3.13.5
dotnet --info => dotnet: command not found
```

Static verification was executed against the generated file tree before commit:

```text
python3 scripts/dev/verify-s00.py
S00 static verification passed: scaffold files, .NET 10 pin, CSRF memory boundary, and paused-scope guards are present.
```

`bash scripts/dev/doctor.sh` was also executed without `--strict` and correctly reported the missing .NET SDK instead of claiming readiness.

## Pending verification

The PR CI workflow `M01 application scaffold` is the first place expected to execute `bash scripts/dev/verify.sh` on the final GitHub revision with .NET 10 and Node 22 available. Until CI passes and later SQL-backed stories are implemented, M01 remains partially implemented only.
