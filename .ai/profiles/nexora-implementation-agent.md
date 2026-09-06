# Nexora Implementation Agent Profile

Primary role: `../roles/technical-lead/README.md`.

The Technical Lead remains final owner. Specialist capability is supplied through focused Rules/Skills rather than competing autonomous role packages.

| Change type | Load from the baseline | Gate / verification |
| --- | --- | --- |
| Architecture/module boundary | `rules/software-architect/*`, `skills/software-architect/*` | multi-tenant gate when ownership/access is affected |
| ASP.NET Core/API/auth | `rules/dotnet-backend-developer/*`, `skills/dotnet-backend-developer/*` | multi-tenant gate for protected data |
| SQL/schema/migration | EF/migration rules + database concurrency/migration skills | phase-on-demand: `agent-database-migration-preflight-rollback-gate` |
| Redis/cache | caching rules + distributed-caching skill | phase-on-demand: `agent-cache-invalidation-safety-gate` |
| Background jobs/notifications/automation | background-processing rule + idempotency skill | phase-on-demand: `agent-background-job-idempotency-gate` |
| Calendar/reminders/scheduler | backend rules + deterministic tests | phase-on-demand: `agent-clock-timezone-boundary-gate` |
| Vault/secrets/sensitive data | security/privacy rules + secrets/threat-model skills | phase-on-demand: `agent-secret-exposure-prevention-gate` |
| Webhook/provider/network fetch | security rules + secure-input review | phase-on-demand: webhook replay and SSRF gates |
| React UI/forms | React accessibility/browser-security/design-system/forms/testing rules + skills | multi-tenant gate if projection can expose another user's data |
| Test/release evidence | QA test strategy + contract testing | ground-truth completion guard |
| Production readiness | repository roadmap + security/test evidence | add SRE/backup/rollback controls only after RM18/RM19 decisions |

## Mandatory independent verification

Always use an independent verification pass for changes affecting owner isolation, `module.action` authorization, Support/Emergency access, Vault, migrations, finance integrity, background effects, file authorization, webhook/SSRF boundaries, backup/restore, or cross-module contracts.

## Explicit non-selection

Do not activate AI/LLM/ML/RAG, Kubernetes, cloud-provider, billing/payment, native-mobile, team-collaboration, or executable-plugin assets unless Nexora requirements change through the documented approval process.

## MCP/API policy

No MCP connector is vendored in the baseline. GitHub is required as an external implementation capability, but use the host/runtime connection with least privilege. Vendor a connector only after Nexora explicitly defines credential storage, capability allowlists, human approvals for side effects, logging/redaction, revocation and runtime ownership.
