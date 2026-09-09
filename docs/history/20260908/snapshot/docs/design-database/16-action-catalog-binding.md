# Action catalog → database / manifest binding

2026-09-07 · Technical design, docs only. Không thêm bảng, cột boolean từng module/action, DDL hoặc migration trong lượt này. Mô hình181 bảng không đổi.

## Authoritative storage

| Contract | Existing storage / authority | Rules |
| --- | --- | --- |
| Stable action key | [platform.Permission.Code](02-core-identity-platform.md#platform-permission), nvarchar(150), UQ Code | Chỉ catalog entry AdminGrantable mới cần persisted permission row; unknown/unregistered = deny |
| Owning installed module | Permission.ModuleId FK Module.Id | Logical FX namespace không tự định nghĩa assembly/entitlement; manifest binds exact ModuleId |
| Admin effect | AdminPermission(UserId,PermissionId,Effect), unique pair | Allow / Deny; absent = deny; only current Admin target, mutation SuperAdmin-only |
| User/module enablement | UserModuleGrant + Module.SystemEnabled + policy revision | Không UserActionGrant, không grant bitmap trong domain table |
| Labels and risk | Permission.Description; Permission.Risk | Risk SQL giữ Normal/Sensitive/Administrative; catalog detailed labels map sang3 mã, không chèn Secret/Financial ngoài constraint |
| Kind/contexts/guard/prerequisites/Q gate | Trusted versioned module action manifest, checked at startup/deploy | Không editable arbitrary JSON của Admin; manifest content/hash pinned deployed version; mismatch prevents activation |
| Support scope | Existing consent/session records + one ModuleId | Permission grant không thay consent; runtime target projection key phải registered |
| Change concurrency | Existing RowVersion + module policy revision; permission revision/cache epoch in architecture | Grant mutation transaction updates effect/audit/invalidation intent; optimistic conflict never blind overwrite |

Catalog.csv là review artifact, không seed script. Blocked row hoặc non-grantable operation không được import thành active permission chỉ vì có key. Runtime registry vẫn biết PUBLIC/CONTROL/LINK/SYSTEM/LOCAL contracts dù chúng không cần một AdminPermission row.

## Registration consistency

Trusted manifest is authority for semantic contract. Reconciliation chỉ insert/update catalog metadata tương thích, không tự tạo Allow cho Admin. Missing/stale manifest, duplicate Code, reused Code với semantic scope khác, missing ModuleId binding hoặc hash mismatch: module readiness failed. Existing permission GUID giữ ổn định khi label đổi. Code deprecated không gán lại nghĩa mới; audit giữ lịch sử. Remove key không cascade-delete grant/audit history.

New action = absent Admin permission → denied. Ordinary User owner baseline chỉ mở action trong module enabled và source workflow đã approved/delegated; major expansion cần product gate. Split/merge installed module IDs là migration/entitlement decision riêng, không suy từ40 FX rows.

## Relationship

~~~mermaid
flowchart TD
  M["Trusted manifest version"] --> R["Runtime action registry"]
  M --> P["Permission metadata"]
  P --> A["Admin Allow / Deny"]
  R --> E["Effective decision"]
  A --> E
  G["Module grant + owner + lifecycle"] --> E
~~~

## Query and integrity acceptance

- Admin editor joins target module state, registered permission metadata and explicit effect; missing row shown Not granted, not inherit/allow. List filtered by actor operational metadata permission; no private content.
- Grant save loads current actor/target roles, validates registered AdminGrantable/current gate, checks revision, protects last active SuperAdmin and writes diff audit; no role union bypass.
- Source request scopes owner before count/pagination and checks actual semantic actions before mutation. Source resource table never stores a copy of permission truth.
- Policy commit must invalidate cached capability; runtime cannot rely solely on UI cache or long JWT permission claims. Cache revision mechanism and concurrency tests follow [revocation contract](../action-catalog/02-revocation-and-runtime.md).
- Risk/detail metadata mapping and Code≤150 are validated before reconciliation; description label≤500. No schema change or migration was executed.

[Catalog](../action-catalog/README.md) · [Core tables](02-core-identity-platform.md) · [Architecture](../architecture/04-authorization-and-sensitive-data.md)
