# Revision-bound baseline audit — initial remote inspection

This initial remote-inspection snapshot is preserved. Later local runtime recovery, actual tests, verifier fixes and migration0046 reconciliation are recorded in [baseline-restoration-evidence.md](baseline-restoration-evidence.md); its current-state evidence supersedes the initial host-blocker/unreconciled-migration statements below.

## Source / coverage evidence

Main branch API xác nhận `8782f46be51f4b0f3cb54f0f0f7a06eae3b0d3e3`; PR #4 OPEN draft, unmerged, HEAD `74771b70a262fa0b9d47bfc038dd3dbea23aa7dd`. Đây là remote verification; local git fetch/status chưa chạy được.

Đã parse CSV bằng JavaScript trong tool orchestration, không chỉ sao chép counts từ checkpoint:

- Main `docs/action-catalog/catalog.csv`: 733 rows, 733 unique ActionKey.
- PR `full-plan-actions.csv`: 733 rows; action key set trùng main (0 thiếu, 0 thừa).
- Main `docs/ux-ui/screen-inventory.md`: 202 unique screen-ID rows; PR `full-plan-screens.csv`: 202 rows, không missing source ID.
- Main tree: 40 current numbered feature specs (01–40); PR `full-plan-modules.csv`: 40 rows.
- PR `target-action-matrix.json`: 386 rows; 102 InstalledSubsetWithFocusedEvidence, 15 SourceCandidateReviewPending, 201 MissingImplementation, 59 PreservedPaused, 9 ExcludedRetired.
- FX30/34/35: 18/20/21 catalog rows. 347 catalog actions nằm ngoài continuation inventory trước đây.

Key/ID reconciliation không chứng minh current field/AC/permission equivalence, functional workflow hoặc acceptance. Full bidirectional source-to-code audit của 733 actions và 202 screens chưa hoàn thành, kể cả 347 actions ngoài inventory. Historical fullActionAccepted=false không được thay bằng claimed acceptance. `full-phase-traceability-matrix.md` và exact source contracts cần đọc trong lượt tiếp theo.

## CI tại baseline HEAD

Sources: [implementation run 37342878027](https://github.com/hakodev2k/Nexora/actions/runs/37342878027), [agent baseline run 37342878009](https://github.com/hakodev2k/Nexora/actions/runs/37342878009). Implementation run thực tế checkout synthetic merge `c7179acc1b3590df43c7657ae60da89e2ccf269c` giữa pinned PR HEAD và main; không coi là local HEAD test.

| Check | Observed CI result | Interpretation |
| --- | --- | --- |
| Agent baseline | PASS | Agent assets check, không app acceptance |
| Frontend unit / typecheck / bundle | PASS | Historical CI trên merge revision nói trên |
| Linux filesystem | PASS | Không chứng minh Windows ACL/process boundary |
| Backend static | FAIL | verify-s00.py báo stale marker setAdminUserRole |
| Backend build / custom unit / xUnit | SKIPPED | Static step failure; không backend pass |
| SQL/API | CI FAIL / functional BLOCKED | NEXORA_TEST_SQL_CONNECTION rỗng; yêu cầu isolated loopback Nexora_Test_<GUID> |
| Browser E2E | CI FAIL / functional BLOCKED | Require configured isolated local E2E target failure; browser install/run skipped |
| Windows filesystem ACL | FAIL | Native 34 discovered, 30 pass, 4 skipped; required Windows identity test skipped |
| Final local 265 browser cases | Incomplete checkpoint | Interrupted; không completed passing report; session này không chạy lại |

Windows skips: một required Windows A/B/C test cần actual runtime/operator/foreign process identities; ba Linux-only tests không áp Windows. Không xóa required test hoặc giảm gate để xanh. Phải configure actual process identities và chứng minh boundary.

## Confirmed static verification defect

`scripts/dev/verify-s00.py` yêu cầu ba marker cũ:

| Stale marker | Registered current endpoint name |
| --- | --- |
| setAdminUserRole | commitAdminUserRole |
| setAdminActionGrant | commitAdminUserPermissions |
| setAdminModuleGrant | commitAdminUserModules |

Source `src/Nexora.Api/Features/Access/AdminAccessEndpoints.cs` có PUT role/permissions/modules dưới /api/v1/admin/users/{userId:guid}/access, preview endpoint, CSRF group, current identity, If-Match, PreviewToken và Idempotency-Key. Đây là source inspection, không runtime authorization proof.

First safe engineering slice sau host recovery: sửa verifier theo exact registered routes và giữ required authorization/preview/ETag/idempotency/CSRF contract; không đổi endpoint để satisfy tên cũ. Acceptance: verifier reject missing current route/guard, pass final source khi contract hiện hữu; run actual static verification và affected backend builds/tests. Không claim fix đã làm. Full baseline phải restore trước new module logic.

## Migration/runtime preservation

Checkpoint báo Monitoring 0045 applied/immutable; 46 normal manifest migrations tại lúc pause; News staged 0046 unapplied/unregistered. Đây là historical checkpoint, chưa kiểm tra actual SQL journal. Không sửa 0045 checksum, không activate staged News hoặc assign new number theo suy đoán. Reconcile manifest + actual journal + draft trước new migration.

Retained synthetic data, dirty/detached checkout, private runtime configuration và generated artifacts không bị sửa/xóa. Không đọc/in private secret files trong remote audit.

## Executed this session / host blocker

- Read-only GitHub connector: main/PR refs, pinned instruction/docs/CSV/JSON, workflow runs/jobs và relevant job logs.
- JavaScript orchestration: CSV parse, count và catalog key/screen-ID comparison.
- `exec_command` mandatory file-read attempt và explicit Windows PowerShell `Get-Location` attempt: process creation failed before command execution, `helper_unknown_error: setup refresh had errors`.
- Node filesystem read attempt và minimal cwd retry: `trusted Node process exited unexpectedly; kernel reset`.
- Build/typecheck/lint/unit/integration/SQL/browser/security scan/baseline scripts: NotRun trong session này. Local runtime-dependent verification BLOCKED bởi host execution failure.
- Independent review: Pending. Không reviewer execution hoặc security acceptance claim.

Không auto-review policy rejection được trả về; đây là host/runtime initialization failure, không thiếu implementation authorization. Khôi phục local execution trước code và private isolated test configuration. Không publish unvalidated product source, không mark blocked checks pass.

## Remaining work

1. Host filesystem/shell recovery; inspect real checkout/git status và preserved source hashes.
2. Apply bounded static verifier correction, configure actual Windows process identities và isolated SQL/browser configuration mà không commit credentials/runtime files.
3. Run complete 265 browser regression từ đầu cùng migration reconciliation; không dùng old focused reporter.
4. Full action/screen semantic + reverse implementation audit, statuses theo required vocabulary, exact source/handler/schema/UI/permission/AC/evidence.
5. Complete exact resumed module contracts và independent reviews; implement only contract-ready slices. Gate register/decision requests ở scope-amendment.md.

Release 1, FX30, FX34 và FX35 chưa được kết luận hoàn thành hay operational.
