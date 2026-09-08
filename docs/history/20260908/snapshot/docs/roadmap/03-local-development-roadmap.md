# Local Development Roadmap and Runbook

**Status:** PLANNED. Chưa cài SDK/Node/SQL/Redis, tạo project/database, chạy migration hoặc thay runtime configuration. Mọi config/command dưới đây chỉ là specification cho implementation tương lai.
[Master RM02–RM18](00-master-implementation-roadmap.md) · [Architecture](02-solution-architecture-roadmap.md) · [Local Stable gate](09-local-stable-release.md)

## 1. Local target profile proposal

Một developer clone source đã implement theo roadmap, setup dependencies và chạy đủ website trên máy. Đề xuất reference profile Windows x64, SQL Server local service, Redis Linux qua WSL2, .NET 10 API và React dev server trên Windows. Actual OS/architecture/RAM/disk/ports được kiểm tra RM02; không tự coi máy hiện tại đã tương thích hoặc đã cài đủ.

SQL Server 2025 có requirements x64/OS/edition riêng; chọn edition development và pin patch khi setup, kiểm tra support matrix thay vì suy từ .NET runtime. [SQL Server 2025 requirements](https://learn.microsoft.com/en-us/sql/sql-server/install/hardware-and-software-requirements-for-installing-sql-server-2025?view=sql-server-ver17)

Redis trên Windows có đường chạy qua WSL2/Ubuntu theo tài liệu Redis. Đây là lựa chọn local, không quyết định production container/hosting. [Redis on Windows/WSL2](https://redis.io/docs/latest/operate/oss_and_stack/install/archive/install-redis/install-redis-on-windows/)

| Dependency | Local proposal | Pin/verification tại implementation |
|---|---|---|
| .NET | .NET 10 SDK + ASP.NET Core runtime | Exact SDK global.json; compatible patch; dotnet --info |
| Node/npm | Node release còn supported, tương thích Vite/React/test tools; npm bundled | Exact version recorded; engine/lockfile; không cần ép pnpm/Corepack |
| React | Vite React+TypeScript proposal | package.json + lockfile; RM07 review |
| SQL Server | SQL Server 2025 development edition/service; local database Nexora_Dev | Edition/patch/collation/TCP setup; compatibility gate; không SQL production hosting |
| Redis | Stable supported Redis bản Linux qua WSL2; version pin | PING/cache integration/outage; no fake Windows Redis executable |
| Git/IDE | Git và IDE hỗ trợ .NET 10/TS | Developer chọn IDE; version/toolchain documented |
| Files | Protected local directory ngoài repository/webroot | ACL, free space, checksum and path controls |
| Development delivery | In-app SQL + test Email adapter/capture + test Push adapter; live transport sandbox check trước Local Stable | No fake “Sent” từ logging adapter; adapters labelled |
| Browser | Approved desktop/mobile representative profile | HTTPS, service worker/push support/denial cases |
| Optional local mail catcher | Chỉ khi thuận tiện hơn adapter fixture, không bắt buộc stack | Chọn package nếu giải quyết inspection need; chưa cài |

Vite có yêu cầu Node và template-specific compatibility; pin/recheck khi RM02 thực thi, không hard-code latest package tự trôi. [Vite getting started](https://vite.dev/guide/)

## 2. Local topology và ports

~~~mermaid
flowchart TD
  B["Browser"] --> FE["React HTTPS :5173"]
  FE --> API["Dev proxy /api → HTTPS :7043"]
  API --> SQL["SQL Server local :14333"]
  API --> R["Redis WSL2 :6379"]
  API --> F["Protected local files"]
  API --> W["SQL-backed worker"]
  W --> D["Email and Web Push adapters"]
~~~

| Service | Proposed address/binding | Purpose |
|---|---|---|
| React | https://localhost:5173 | Browser origin, HMR, service worker/push development |
| API | https://localhost:7043 | Local API/OpenAPI/health, HTTPS only profile |
| SQL | localhost:14333 TCP | Dedicated local test/dev instance port, configurable nếu collision |
| Redis | localhost:6379 hoặc private WSL endpoint được verify | API↔WSL connection; không mở LAN/public |
| Dev email capture (nếu chọn) | Loopback port theo tool được chọn | Inspect test mail; không fixed provider requirement |
| File storage | Absolute external path, ví dụ developer-selected NexoraData/Development | Không route static/public |

Port values là technical proposal. Dev proxy từ React origin tới API giúp browser dùng same-origin /api path. Vite proxy/HMR/Web Push service worker config phải được implementation kiểm thử với local certificate; diagram không có nghĩa proxy code đã có.

CORS: default không wildcard. Proxy profile không cần browser cross-origin API call; nếu direct API profile dùng cho debugging thì allow exact https://localhost:5173 + credentials + methods/headers cần thiết, CSRF vẫn bắt buộc. Không AllowAnyOrigin với credentials; CORS không là security boundary cho resource ownership.

Windows↔WSL localhost routing phải được xác minh trên actual machine; nếu không hoạt động, chọn endpoint private có giới hạn và record lại. Không xử lý connection failure bằng bind DB/Redis ra 0.0.0.0/public.

## 3. Configuration contract

Tên config/key dưới đây là proposed contract, chưa phải file appsettings hiện có.

| Configuration | Giá trị/phân loại dự kiến |
|---|---|
| Environment | Development explicit local profile; test dùng isolated Test |
| appsettings.json | Schema/default non-secret hợp lệ, không personal/production values |
| appsettings.Development.json | Local ports/providers/log levels non-secret; không plaintext credentials |
| ConnectionStrings:SqlServer | Local DB connection; runtime credential ngoài source |
| ConnectionStrings:Redis | Private endpoint + timeouts; password/ACL credential ngoài source |
| FileStorage:Root | Absolute protected path ngoài source/webroot |
| Crypto:KeyProvider / KeyRingPath | Reference protected key store; không raw key trong appsettings |
| Notification:Email / Push | Adapter kind và non-secret options; credentials/VAPID private key external |
| Jobs | Worker enabled, leases/concurrency/retry limits từ approved profile |
| Cors:AllowedOrigins | Exact local origin nếu direct browser API profile được bật |
| Logging | Safe structured sink; redaction; không HTTP body/token dump |
| Seed | Explicit synthetic fixture mode, false ngoài dev/test |
| Bootstrap | One-time protected operator flow/secret, không default admin password |

Ví dụ connection string local cho Windows-integrated development account:

~~~text
Server=localhost,14333;Database=Nexora_Dev;Integrated Security=True;Encrypt=True;TrustServerCertificate=True
~~~

TrustServerCertificate=True chỉ là local developer-certificate accommodation trong proposal; không dùng làm production policy. Runtime SQL user dùng quyền app cần thiết; migration identity tách khi rehearsal. Nếu SQL authentication cần thiết thì password nhập qua local secret source, không nhúng vào ví dụ/command history.

Environment overrides theo .NET configuration key convention, ví dụ ConnectionStrings__SqlServer, ConnectionStrings__Redis, FileStorage__Root. Dùng đúng intended configuration keys, không repurpose HOME hay biến hệ thống cho task values. Frontend chỉ nhận public config; mọi biến exposed trong browser bundle đều không được chứa secret.

## 4. HTTPS và secrets

- Dùng local trusted developer certificate cho API/React; pin hostname localhost. Không tắt certificate validation toàn HttpClient để hết lỗi.
- Browser session cookie Secure/HttpOnly; dev CSRF flow phải hoạt động như intended production semantics.
- Secret Manager chỉ giúp tránh đưa development secrets vào repo, **không mã hóa secret**. Không dùng nó làm Vault encryption-key store an toàn hoặc production secret store. [ASP.NET Core development secrets](https://learn.microsoft.com/en-us/aspnet/core/security/app-secrets?view=aspnetcore-10.0)
- Vault/integration-key protection cần ADR-R08: OS-protected store/private key access riêng, synthetic keys/data cho tests; SQL/files backup không kèm plaintext key.
- Test Email/Push dùng sandbox credentials; không phát tới real users từ seed hoặc test fixtures.
- Missing required connection/secret phải fail-fast có safe instructions, không print values/connection strings.
- Không dùng production data hoặc real Vault secrets trước encryption/key recovery gate.

## 5. Database/Redis setup và first-run responsibilities

Local SQL service setup, DB tạo từ approved migrations; không hand-edit schema để “chạy được”. Migration runner có explicit target guard, lock và module journal; credentials/schema pre-check trước change. Full migration order theo dependency manifest, không alphabetical folder order.

Seed reference data: roles/permissions, module definitions, approved DocumentTypes; IDs stable và idempotent. Synthetic users/Projects/Documents/Events optional, tách fixtures khỏi data thật. Bootstrap SuperAdmin thông qua one-time secure flow, không seed password hoặc mở registration bypass.

Redis setup gồm private bind/ACL nếu cần, memory/timeout policy và connection health. Không bật persistence để giả làm source of truth jobs; durable jobs ở SQL. Cache prime optional/lazy, no seed secrets into Redis.

## 6. Runbook command contract — FUTURE, chưa tồn tại đầy đủ và chưa chạy

Các script scripts/local/*.ps1 dưới đây là **deliverables cần implement**, không phải scripts hiện có. Chúng phải có help, target guard, idempotency và safe diagnostics. Không được bảo developer chạy bảng này như runbook đã tested trước RM18.

Chuỗi lệnh và paths đề xuất cần được pin/verify từ clean checkout đã hoàn thành implementation. Script implementation phải materialize exact commands phù hợp actual versions/contexts; không install “latest” tự trôi, không silently create/delete target production.

| Step / Objective | Input | Tasks / command contract | Technical Decisions | Dependencies | Deliverables | Definition of Done | Next Step |
|---|---|---|---|---|---|---|---|
| L01 — lấy source | Git, repo access | git clone https://github.com/hakodev2k/Nexora.git rồi cd Nexora | Clone branch/tag chứa implementation đã duyệt | RM18 candidate để rehearsal full run | Working checkout | Correct commit, no local source alterations | L02 |
| L02 — kiểm tra prerequisites | Profile file/OS support | dotnet --info; node --version; npm --version; git --version (từng lệnh riêng trong future runbook) | SDK/Node/SQL/Redis exact versions đã pin | L01 + dependency installation plan | Tool version report | Compatible versions; scripts không tự đổi toolchain ngoài approval | L03 |
| L03 — cài dependencies của app | Solution, lockfiles, local tool manifest đã tồn tại | dotnet tool restore; dotnet restore Nexora.slnx; npm --prefix frontend/nexora-web ci | Locked tools/packages; không global package drift | RM03/RM07 artifacts; L02 | Restored build dependencies | Restore succeeds on clean cache; no credentials printed | L04 |
| L04 — local configuration/HTTPS | Private local roots, available ports | dotnet dev-certs https --trust; ./scripts/local/configure.ps1 | Planned configure script creates only permitted local config/key references, validates origins/roots | L03; chosen local cert/key provider | Configured development profile | No secret committed; exact cert/proxy setup verified | L05 |
| L05 — dependency startup/connectivity | SQL service/WSL Redis installed | ./scripts/local/dependencies.ps1 -Action Start; ./scripts/local/dependencies.ps1 -Action Verify | Planned script resolves actual service names, private ports, PING/SQL connection and safe failure | L04 + selected dependency versions | SQL/Redis reachable | No public bind; no DB schema edits yet | L06 |
| L06 — migrate local DB | Nexora_Dev target confirmed; schemas/manifest | ./scripts/local/migrate.ps1 -Environment Development | Planned wrapper uses approved EF contexts/order/lock; same target guard as future CLI | L05; RM04/domain migrations | Schema/migration journal | Empty and upgrade path pass; wrong environment blocked; no silent partial ready | L07 |
| L07 — reference/optional seed | Migrated schema | ./scripts/local/seed.ps1 -Environment Development (optional synthetic fixture flag documented separately) | Script must not seed default password or production data | L06 | Reference catalog/optional fixtures | Idempotent repeat; no duplicate users/modules; secrets absent | L08 |
| L08 — run API | Config/schema/ref data | dotnet run --project src/Nexora.WebApi --launch-profile Nexora.Local | Future launch profile defines HTTPS :7043 and development worker settings | L07 + profile implemented | API/worker runtime | Liveness/readiness and safe dependency statuses; bootstrap gate enforced | L09 |
| L09 — run React | API active; package scripts present | npm --prefix frontend/nexora-web run dev -- --host localhost --port 5173 | Vite HTTPS/proxy configured in future app; command alone does not set HTTPS | L08 + RM07 config | React website on local HTTPS | Browser/API credentials/CSRF/navigation work; no insecure fallback | L10 |
| L10 — first account/use | API/React/bootstrap closed correctly | Use one-time bootstrap UI, then public registration → inspect verification mail → verify → login → module flows | Operator secret entered through protected UI, no shell password | L08–L09 + RM05–RM16 | Active accounts/personal boundaries and usable full app | Email verified required; whole catalog usable; no Admin approval | L11 |
| L11 — validate local candidate | Seeded isolated test profile | ./scripts/local/verify.ps1 -Profile LocalStable | Planned wrapper builds/lints and runs suites in 08; never production target | L10 + RM17 tools/scenarios | Test/runbook evidence | Required suites pass; runtime output redacted; artifacts linked to commit | RM18 approval |
| L12 — stop/restart safely | Running local environment | Stop foreground hosts; ./scripts/local/dependencies.ps1 -Action Stop | Stop services, không delete volumes/files/DB/keys; destructive reset separate explicit operation | L11 or session end | Preserved local data | Restart retains SQL/files/key data, cache loss handled | Resume L05/L08/L09 |

Bảng là contract cho kế hoạch tương lai. Nếu script/path/version/profile chưa tồn tại hoặc chưa verified, runbook status là NOT READY; không báo setup hoàn thành.

## 7. Troubleshooting plan

| Symptom | Diagnostic / safe correction path |
|---|---|
| SQL connect failure | Kiểm tra selected instance/TCP port/service, auth identity, target DB và firewall private; không đổi sang sa mặc định hoặc public bind |
| Redis unreachable từ Windows | Verify WSL service/routing/port/ACL; show degraded-cache path; không bỏ owner/permission checks |
| HTTPS/browser cookie failure | Certificate trust/hostname/origin/SameSite/proxy/CSRF; không disable HTTPS hoặc leak token into URL |
| Migration failure | Dừng module readiness, inspect safe migration ID, rollback/restore rehearsal; không mark migration success thủ công |
| Bootstrap hoặc verify bị retry | Show deterministic consumed/already-provisioned outcome; no duplicate PersonalSpace |
| Email/Push chưa tới | Logical intent + per-channel attempts/source status; fixture adapter không giả Sent; browser-denied khác provider failure |
| File upload/crop failure | Size/type/ACL/free space/staging cleanup; no overwrite prior content/version |
| UI stale after permission/revoke | Clear client private cache, re-authenticate/context check; server SQL policy vẫn deny trước UI refresh |
| Dirty Document mất kết nối | Keep draft in memory, safe retry with same request identity; no autosave/version duplication |
| Clock/timezone issues | Compare UTC/zone/date-only config và source fixtures; không shift existing event instant |

## 8. Local delivery completeness và phase gate

Mock/test adapters cần cho repeatable tests nhưng không đủ chứng minh Email/Push thực. Trước RM18 phải có local application gửi controlled verification/reminder/security notification qua real test Email transport và Browser Push trên browser hỗ trợ; ghi bằng chứng của cả success và denial/outage. External push delivery có thể cần Internet dù app host vẫn local. Không phải lý do chọn cloud hosting trước RM18.

Fresh-machine/full-checkout rehearsal tại RM17 xác minh toàn bộ runbook trên dependency profile đã pin. Sau đó replace “planned script contract” bằng tested operational steps trong task implementation tương lai. Deliverable của task hiện tại vẫn chỉ là tài liệu này.
