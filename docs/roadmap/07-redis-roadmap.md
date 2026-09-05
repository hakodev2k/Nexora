# Redis Roadmap

**Status:** PLANNED; không cài Redis hoặc cấu hình runtime trong task này.
[Master](00-master-implementation-roadmap.md) · [Local](03-local-development-roadmap.md) · [Architecture](02-solution-architecture-roadmap.md)

## 1. Vai trò theo phase

Redis đã được User chọn trong stack nhưng không buộc mọi feature cache. RM02 kiểm tra local connection; RM06 có adapter/fallback/metrics; RM11–RM14 chỉ kích hoạt cache có use case; RM17 đo latency/outage/flush recovery; RM19+ mới thiết kế hosted Redis.

SQL Server giữ business state, grants, sessions, job/intents và history. Flush/loss Redis không làm mất tài khoản, dữ liệu, quyền hoặc scheduled notification.

## 2. Cache policy đề xuất

Các TTL dưới đây là technical starting values cần review/đo tại ADR-R09, không phải business SLA hoặc số User đã xác nhận.

| Dữ liệu | Cache? / key sketch | TTL đề xuất | Invalidation | Fallback |
|---|---|---|---|---|
| Public GitHub query response | Có; nx:{env}:github:{adapterVersion}:{queryHash}:{window} | 5 phút fresh; tối đa 60 phút stale-if-error có nhãn | Query/window/version đổi tạo key mới; refresh coalesce | SQL snapshot hoặc provider bounded; nếu không có valid data thì unavailable, không fabricate |
| Public RSS fetch body/ETag metadata | Chỉ raw public response hợp lệ; nx:{env}:feeds:{adapterVersion}:{urlHash} | 5 phút hoặc HTTP freshness bị cap | Revalidate theo provider; source change/version invalidates | Bounded fetch hoặc last valid SQL article/source status |
| Installed manifest/reference display data | Có thể; nx:{env}:catalog:{buildVersion}:{schemaVersion} | 30 phút | Build/version/config change | Trusted registry + SQL metadata |
| DocumentType definitions | Chỉ safe immutable definitions nếu đo thấy lợi; nx:{env}:documents:types:{definitionVersion} | 30 phút | Nếu sau này có approved admin update: DB commit → bump definition version → expire key | SQL/static trusted definitions; không tự thêm manage-types feature |
| Personal list/search result | Không ở baseline | Không áp dụng | Luôn SQL current scope | SQL bounded query |
| User Tags/Folder tree | Không ở baseline; query owner-indexed | Không áp dụng | SQL current data | SQL |
| Permission, share/support/emergency grant, module effective allow | Không cache positive allow | Không áp dụng | SQL authoritative check sau revoke commit | SQL; nếu SQL mất thì protected access fail-closed |
| Sessions/revocation/verification tokens | Không làm sole truth trong Redis | Không áp dụng | SQL session/token lifecycle | SQL |
| Vault payload/decrypted values, credential/token/key | **Không** | Không áp dụng | Không có key được phép chứa secret | Secure store/decrypt dedicated authorized action |
| Finance balance/ledger, reminder due state, notification attempts, audit/history | **Không làm source of truth**; baseline không cache | Không áp dụng | SQL transactions | SQL |
| Price snapshot | Source dữ liệu valid trong SQL; raw public acquisition cache chỉ sau price definition decision | Chưa chốt | Adapter/variant/currency/definition version | Last valid SQL price có stale label; failure không giá 0 |

Không cache toàn HTTP response chứa user-specific data trong public key. Public provider payload phải bỏ request credentials và tách read/saved/tracker/alert states của từng User. Không cache failure như valid empty result.

## 3. Key design, invalidation và concurrency

Nếu có personal cache sau này, key bắt buộc đủ environment/module/owner/query/schema/permission-version theo semantics. Không include email, token, password, full URL có credentials hoặc query secret. Query canonicalization phải deterministic trước hash; pagination/filter/window/timezone/currency/variant không được dùng nhầm key.

Cache-aside: read authoritative policy trước; miss tải nguồn bounded; cache safe projection. Mutation commit SQL trước, rồi invalidation qua durable outbox nếu needed. Tránh race stale refill bằng versioned keys/source revision và verify revision khi fill. Không chỉ “DELETE key” rồi giả định race đã hết.

Không dùng TTL để trì hoãn revoke/Trash/module disable. Stale search/index/cache entry vẫn bị current resource authorization chặn trước serialize/count. Không serialize previous User state vào shared provider cache.

Stampede control có thể coalesce trong process ban đầu, TTL jitter và bounded concurrency. Redis lock nếu sau này dùng chỉ là optimization cho duplicate fetch; lock expiry/crash không được quyết định uniqueness của business effects. Unique keys/SQL transaction mới bảo đảm logical idempotency.

## 4. Outage behavior

- Redis timeout phải nhanh và bounded; không nối chuỗi retry trên mỗi request.
- Redis down: core CRUD/auth/permissions/jobs vẫn dùng SQL. Readiness báo degraded cache, liveness không buộc restart loop.
- Provider down và Redis down đồng thời: last valid durable snapshot hoặc unavailable có timestamp/status; không đẩy unbounded traffic sang provider.
- Sau reconnect: lazy refill, không tải toàn bộ personal dataset; no cross-environment/cross-user keys.
- Cache metrics chỉ label module/type/outcome; không high-cardinality owner/email/secret labels.
- Security throttling không dựa riêng vào volatile cache. Nếu dùng Redis counters tương lai phải có SQL hoặc fail-safe control phù hợp, không disable rate limit khi cache down.

## 5. Validation và deliverables tương lai

| Suite | Bằng chứng cần có |
|---|---|
| V-CACHE-01 | Query/filter/window khác nhau không dùng nhầm cache; User metadata không chui vào public key |
| V-CACHE-02 | TTL expiration, stale-if-error nhãn đúng, version bump và stale-fill race |
| V-CACHE-03 | Redis unavailable/flush/restart không mất SQL business state, login/revoke/jobs vẫn đúng |
| V-CACHE-04 | Grant/resource/module revoke chặn request dù cache giữ response cũ |
| V-CACHE-05 | Marker Secret không có trong keys/values/log/metrics |
| V-CACHE-06 | Cache hit/miss latency, timeout/fallback/load test theo approved profile |
| V-CACHE-07 | Concurrent refresh/lease expiry không tạo duplicate logical price/article/notification |

Done tại RM17 khi Redis hoạt động ở local, cache enabled có evidence, outage/flush recovery pass và không cần cache để chứng minh correctness. Một feature không có use case cache được phép dùng SQL trực tiếp; không bịa hit-rate để đáp ứng stack.
