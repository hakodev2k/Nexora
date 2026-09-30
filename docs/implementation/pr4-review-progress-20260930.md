# PR #4 review continuation — 2026-09-30

This is an interim implementation checkpoint, not full E2E acceptance.
The branch remains `impl/m01-s00-scaffold`; PR #4 must remain unmerged.
Requirements were read from `origin/main`, particularly M01-S07–S09 and
`docs/delivery/milestone-01/02-api-contracts.md`. The user's current request
explicitly authorizes functional tests and synthetic fixtures, superseding the
older repository code-only execution amendment for this review.

## Corrected behavior

- Access changes use a signed, session-bound preview followed by confirmation,
  exact change bodies, If-Match and idempotency headers. Role changes retain the
  base User membership. Admin metadata grants do not authorize SUPER access
  detail or mutation endpoints.
- A forward migration registers the missing `access.change.read` metadata.
  Recent authentication timestamps read from SQL are interpreted as UTC;
  missing proof no longer becomes fresh authentication automatically.
- Failed actions keep confirmation dialogs open, display errors and preserve
  drafts. Revision conflicts refresh account snapshots without dismissing the
  review. Confirmation uses the version bound to its preview.
- Modal dialogs restore focus, isolate background controls and prefer the safe
  action for destructive flows. Trash, sharing, files and other destructive
  actions have clearer confirmation and failure behavior.
- Module policy editing supports changing both booleans before preview. The
  catalog request retrieves up to 100 modules instead of silently showing 25.

## Executed verification

- Frontend production build: passed; Vite reports a bundle-size advisory.
- Frontend Vitest: 63 passed, 1 skipped. Includes 26 screen-surface checks,
  accessible control names, modal keyboard/focus behavior, loading/error states,
  recent-auth continuation and stale-preview preservation (409 and 412).
- API/integration project build: passed, zero warnings/errors.
- Backend unit tests: 12 passed, zero failed/skipped.
- Real SQL Server 2022 Developer integration: 14 passed, zero failed/skipped.
  Synthetic fixture databases apply the migration manifest and test owner
  isolation, lifecycle, authorization and access preview/commit behavior.
- These suites are bounded regression evidence, not coverage of every feature,
  role, interactive element, visual breakpoint or UI edge case.

## Runtime continuity

Node dependencies were reused. .NET SDK 10.0.100 and NuGet packages were restored
under `/tmp/nexora-dotnet` and `/tmp/nexora-nuget-packages`. Docker installation
succeeded, but container layer registration fails with `unshare: operation not
permitted`. Native SQL Server was restored from Microsoft's package and started
on port 14333 with a generated local-only password. No credential is committed.
`scripts/dev/run-sql-native.sh` preserves the optional native startup configuration;
the normal `docker-compose.local.yml` remains reusable on a Docker-capable host.
`scripts/dev/test-sql.sh` requires a loopback synthetic SQL connection and preserves
the migration/fixture setup. Preserve working SQL state during continuation.

Frontend dev server starts on loopback port 5173. The provided cloud browser
rejects that URL with `ERR_BLOCKED_BY_CLIENT`. Actual browser UI role flows,
screenshots, responsive visual review and full E2E acceptance remain pending.
The temporary public frontend must remain off.

## Remaining review gates

Continue the main-branch requirement/feature matrix and all-role UI workflows.
Known areas needing further review include exact API response projections,
cursor pagination, current transactional authority checks, module dependency
revision binding, idempotency replay after preview expiry, and module-policy
control-plane/catalog permission semantics. Independent security/migration
review is pending. Do not claim all logic, all UI elements or all edge cases
verified from this checkpoint.
