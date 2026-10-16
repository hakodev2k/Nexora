# Disposable CI SQL and browser runtime

User scope: rerun the full audit/implementation prompt plus UI smoke and visual verification. This run explicitly permits synthetic fixtures and real local/test runtime checks, overriding the older code-only execution restriction. Local-only approval remains DEC-20260909-014; no production, paid provisioning, provider mutation or real secrets/data.

The previous SQL/API job depended on a secret pointing at a loopback database that cannot be assumed to exist on a fresh runner. The browser job likewise depended on external target/operator variables. These jobs now own reproducible isolated targets instead of reusing an ambient service.

| Boundary | Harness behavior |
| --- | --- |
| SQL | Existing repository SQL2022 Docker image, Developer, fresh random masked SA credential, loopback14339 only, no host data volume. Real SELECT1 readiness; isolated Nexora_Test_GUID databases only. |
| Ownership | Exact run/attempt/job container name and matching run/attempt/job and per-invocation labels required for removal. Existing name refuses reuse. CI identity is mandatory. |
| Browser target | Existing API, Nexora.Local and FunctionalTestOperator compiled; approved seed creates disposable synthetic accounts. Generated Vite config proxies to a fresh loopback API. Both direct and proxied health/ready must return200. |
| Private state | Per-run private runner-temp directory outside web root;700 directories,600 accounts/environment/log/state files,700 operator wrappers. Inherited Nexora/local/config overrides removed in a copied child environment; explicit same private capture/storage paths win config precedence. |
| Operators | Existing CLI only; account token capture never exposed over HTTP. SQL operator guarded to isolated database. Raw tokens/passwords/envelopes/logs stay private; no HTTP delivery/mail/provider activation. |
| Processes | API/Vite started in own process groups. Cleanup checks PID starttime, command hash, group identity, directory ownership and run identity before termination or file removal. |
| Evidence | Browser stdout/call logs captured privately. Uploaded JUnit excludes failure payloads/system streams; preserves test names/outcomes/counts/timing. Upload allows explicitly synthetic home/UI smoke screenshots only; no trace/video/account/capture/environment/rawlog upload. |

Scripts: `start-ci-sql.sh`, `stop-ci-sql.sh`, `start-e2e-target.py` and `stop-e2e-target.py`. Start browser target after SQL and executable builds. `run-browser` executes the current locked Playwright E2E suite against that exact target; failure exit status is preserved. `sanitize-evidence` runs before upload. Cleanup steps run even on failure.

Shell syntax, Python compilation, workflow YAML parsing, non-CI rejection guards and focused JUnit sanitization checks are implementation checks, not proof of Docker/SQL/browser acceptance. Root records actual CI/runtime results at final HEAD. Native SQL PAL initialization failures from earlier attempts remain failed provisioning evidence; this harness does not relabel those tests as passed. Windows capture ACL evidence still requires the real distinct OS principals/current existing Windows job; this change does not weaken that gate or certify it.
