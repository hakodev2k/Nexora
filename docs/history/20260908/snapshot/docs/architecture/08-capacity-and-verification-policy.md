# Capacity policy — technical ownership, measurable evidence

PO target “càng nhiều người càng tốt” is a direction, not an SLA. No fixed maximum accounts, unlimited capacity, free infinite retention or zero bugs is promised. [Q-08](../features/90-open-decisions.md#q-08) remains open for spend/workload/RPO/RTO.

Technical decisions: keep modular monolith + stateless API scale-out, bounded workers/queues, authoritative SQL and rebuildable caches; owner-first indexes and query scopes; pagination with bounded response; stream files, avoid full history loads; async exports/delivery; database transactions/outbox/idempotency/revocation checkpoints. No microservices/sharding without measured bottleneck. Fair queue scheduling and backpressure prevent one account monopolizing workers.

Engineering benchmark scenarios (not user quotas or promises):100/500/1000 concurrent active sessions at stated request mix, plus one owner with100k tasks and deep retained history;50 concurrent uploads and notification bursts. Dataset sizes/concurrency are test inputs to adjust, not a claim the current design passes. Record hardware, warm/cold cache, QPS, p50/p95/p99, error rate, SQL waits/IO, queue age, per-channel failure, memory/CPU and retained bytes per owner.

No execution in docs phase. After explicit code approval: unit/domain/real-SQL integration/contract/security/accessibility/E2E tests, fault injection and recovery drill; then baseline load test, find bottleneck, tune indexes/batching/parallelism, repeat only changed risk. New module must meet bounded API/job contracts and isolation before enabled.

Soft-deleted Accounts and Vault values retained indefinitely by current product rule add ongoing storage/key-backup costs; no silent auto-purge to meet capacity. Storage/email/provider budget and recovery objectives require PO decision before production commitments. Operational crash recovery restores account-deleted and link-revoked barriers; SQL/files/wrapped keys must match before unlocking service.

Stability evidence must report observed failure rates and limitations; “không lỗi” is an engineering goal, not a warranty. Feature completion requires acceptance tests; production release requires load/security/restore evidence and approval separately.
