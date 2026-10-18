# Kế hoạch triển khai phần còn thiếu sau audit Nexora

Ngày khảo sát: 2026-10-16; audit bàn giao cập nhật 2026-10-17. Tài liệu **plan-only**, chưa thực thi remediation. Baseline sản phẩm là PR #4 tại `f0611fad590269fdc73a3397fd785a97d2c0ef5e` (ký hiệu **R**); source requirements tại main `8782f46be51f4b0f3cb54f0f0f7a06eae3b0d3e3` (**M**), có các amendment local đã đối chiếu. Tên file/API/schema mới trong tài liệu đều là **PROPOSED** trừ khi ghi existing. Các technical recommendation cần được ghi vào contract của slice trước code; không trở thành quyết định PO bằng việc xuất hiện ở đây.

Đọc cùng [task brief](task-brief.md), [reading/evidence ledger](source-ledger.md), [P1 work packages](p1-work-packages.md), [test và trace matrix](verification-plan.md). Các phụ lục là bộ phận của kế hoạch này.

## 1. Tóm tắt cho developer và reviewer

Người dùng có thể gửi lệnh khi còn quyền nhưng quyền bị thu hồi trước lúc ghi; kiểm tra quyền ở trước transaction không chứng minh command được phép commit. Goals/Habits/Planner hiện còn đường này. Sau khi ghi, các service trả full detail dù quyền mutation không tự cấp quyền đọc. Khi phản hồi mất, retry hiện thường trả `409 IdempotencyReplay` hoặc thất bại bởi ETag/state mới, thay vì khôi phục outcome cũ. UI ba module còn giả định mọi thành công có full DTO và tự tải lại.

Ưu tiên: chốt receipt/authority/response contract → sửa từng vertical slice đủ backend/API/UI → chứng minh real SQL/privacy/race → review độc lập. Windows A/B/C triển khai song song trên disposable Windows target; CI phải tiếp tục fail nếu actual-principal evidence thiếu. Không cần chờ Windows để chuẩn bị Documents và các contract P1 độc lập.

P1 giữ nguyên toàn scope R1: Documents hierarchy/editor/cohorts/compare/restore; Organization assignments/Collections/Templates; Discovery providers/facets/saved query/recent/palette; Dashboard layout; Notifications source resolver/inbox; News/GitHub stored flows; Career Resume/Calendar; Learning evidence/plan/certification/WorkLog; re-audit đủ 40 FX. P0 hoàn thành không có nghĩa R1 hoàn thành.

Không tự mở advanced Finance, Task extensions, Vault plaintext, real provider, production, paid infrastructure, merge hoặc main write. `DEC-20260909-014` đã cho phép local contracted implementation; không xin lại blanket approval. Yêu cầu đính kèm chỉ cho lập kế hoạch ở lượt này; test/fixture/UI smoke là **planned** cho đợt thực thi sau.

## 2. Baseline thực tế và phần phải giữ

| Bề mặt | Quan sát authoritative | Ý nghĩa |
| --- | --- | --- |
| Remote PR | [PR #4](https://github.com/hakodev2k/Nexora/pull/4), open/draft/unmerged, head R, base M; GitHub connector get_pr_info | Không tạo PR song song hoặc merge |
| Local | `impl/m01-s00-scaffold`, HEAD `1cd27a46a2ba1f24ab2b6f32f84771230f4b1761`, tree `b02b8efc9c6181a14d8788dab6c920e08c24c944` | Local chậm remote 5 commits, không thể dùng local để phủ định remote fix |
| Dirty files | 12 tracked modified, các untracked test/script/report/artifacts; danh sách ở task brief | Giữ nguyên; chưa có bằng chứng ai đang edit, không tự nhận author |
| Dirty fingerprint | SHA256 `439dd663d0d6f39ffd958bbbef50c7b95097c9e4efb2811c7a2d5c7b3d816e4e`; [per-file hashes](worktree-modified-fingerprint.csv) | Hash của path:SHA256 các modified tracked files đã sort; không đại diện toàn worktree/untracked |
| CI | [38915340562](https://github.com/hakodev2k/Nexora/actions/runs/38915340562): completed/failure; SQL/API, browser, backend, frontend, Linux success; Windows failure | Job IDs ở task brief; success job không chứng minh mọi business requirement |
| Agent baseline | `38915341389` success | Chỉ agent assets, không application acceptance |
| Counts đối chiếu log | SQL/API219 passed/0failed/0skipped; xUnit49/0/0; frontend143passed/1skipped/14files; Windows30passed/0failed/4skipped, required assertion thiếu | Đã đọc summary log của jobs tương ứng. Browser10 và local production4/dev2 vẫn là PR/report baseline, browser job success được kiểm tra nhưng chưa parse lại ZIP JUnit |

Các báo cáo `full-product-rerun-20261016` có trên R, chưa có local. Một số báo cáo tự ghi implementation `1cd27a46`; `verification.md` còn các đoạn kết quả cũ 141/216/200. Đọc chronology và CI cuối, không chọn một câu đầu file để chứng nhận R. Log checkout CI xác nhận tested revision là PR merge ref `c781cb13edc7b3c0ffc8bff4152f507e6c62114f` (R kết hợp base M), không local dirty tree. Artifact IDs: SQL12052840284, backend12052795023, frontend12052251743, browser12052845315. Mọi implementation sau kế hoạch phải rebaseline trước khi sửa và trước handoff.

Giữ remote Finance paging/exact read/projection ACK/exact per-currency totals/currency và owner-IANA defaults; Bookmark/Reading Trash/opaque replay/quoted ETag; Search current authorization/cursor/SQLGuid/IANA; Documents dedicated Tag/library/history/snapshots/projection digest; Notification trusted SYSTEM boundary/safe unavailable fallback; runtime navigation/skip/focus/dirty/modal isolation; job-owned SQL/browser harness và encryption key 32 decoded bytes. Những phần này cần regression và final independent review, không viết lại theo local cũ.

## 3. Requirement/gap register và goal binding

`CONFIRMED` dưới đây là source counterexample; runtime exploit chưa chạy trong lượt này. Implementation state và confidence là hai cột độc lập. `DISPROVED` chỉ bác giả thuyết cụ thể, không chứng nhận module.

| ID / priority / severity | Confidence / implementation | Source cụ thể ở R hoặc M | Tình huống → hướng xử lý |
| --- | --- | --- | --- |
| G-AUTH / P0 / High | CONFIRMED / INCORRECT | `SqlGoalService.cs:35–96,102–125,263–280,391–428,443–447` | Preflight connection khác; không final current proof → same-transaction checks cho List/Get và mọi mutation, kể cả keyless |
| G-REPLAY / P0 / High | CONFIRMED / INCORRECT | Goal `CheckReceipt:599–612`, `Transition:372–408` | Completed receipt thành409; fresh ETag/state chặn trước replay → replay branch trước fresh-write validation |
| H-AUTH / P0 / High | CONFIRMED / INCORRECT | Habit `Update:114–161`, `RecordCheckIn:236–307`, `HasCapability:366` | Separate connection khi business TX giữ lock; full detail sau commit → same TX, stage projection trước commit |
| H-REPLAY / P0 / High | CONFIRMED / INCORRECT | Habit `HabitReceiptOperations.CheckIn`, `CheckReceipt:666–678` | Namespace `habits.checkin` đã ổn định, nhưng replay409; action Record/Correct phải lưu riêng → giữ namespace và original action |
| P-PROJ / P0 / High | CONFIRMED / INCORRECT | Planner `ReadPlan:273–301`, `ReadPin:304–328`, `Reorder:182–223` | SQL trả Task/Project payload, source availability bool không loại title; reorder trả full plan → provider safe projection/ACK |
| P-CLEAN / P0 / High | CONFIRMED / PARTIAL | action15 guard `planner.plan.unpin`; service231–259 | Contract cho cleanup unavailable source; query hiện INNER JOIN source → owned-pin metadata read riêng, không đòi thêm source-read |
| UI-P0 / P0 / High | CONFIRMED / INCORRECT | R `App.tsx` Goals5343/Planner5565/Habits5740; api1817–1996 | `.goal/.habit` và `await load()` sau success → typed union + immutable uncertain intent + privacy clearing |
| ETAG-P0 / P0 / Medium | CONFIRMED / INCORRECT format enforcement | Goal `TryETag:659–673` dùng Trim quotes, nhận unquoted base648byte | Giữ quoted ETag contract; reject malformed/wildcard/weak/multi-value; regression middleware/API |
| WIN-ABC / P0 / High gate | CONFIRMED / MISSING | `LocalAccountMessageSinkTests.Register:17`; Windows CI job117021240778 | Permanent Skip; một SID argument không là token → ba child PID/SID thật và OS deny |
| REVIEW / P0 / security gate | CONFIRMED / UNVERIFIED | R verification follow-ups + PR body | Final security deltas chưa independent review → review queue mục10 |
| DOC/ORG/DISC/DSH/NTF/NEWS/GH/CAR/LRN / P1 / mixed | CONFIRMED registered workflow gaps / PARTIAL hoặc MISSING | P1 appendix đối chiếu interfaces/registered endpoints/feature AC | Làm theo từng story, không thay missing bằng metadata CRUD |
| SHOP-EXT / P1 / chưa định severity | SUSPECTED / UNVERIFIED | Wishlist interface/endpoints có; chưa full trace Comparison/Orders/Sellers/Warranty | Re-audit FX31 trước khẳng định từng capability MISSING |
| FIN-DEFAULT / đã repair / review pending | DISPROVED giả thuyết “R vẫn auto VND” / PARTIAL review | R App FinanceScreen + ResourceFormDialog + commit f0611fa | Không sửa lại defaults; review cả hai entry và regression boundary |
| NTF-RESOLVER / P1 / privacy | CONFIRMED / PARTIAL | R SqlNotificationService; report còn thiếu resolver | Safe source-unavailable fallback không chứng minh source open/resolve hoàn thành |
| DOC-RESTORE / P1 / data integrity | CONFIRMED / MISSING workflow | IDocumentService có History/Version, không Compare/Restore; registered routes | History browse không phải restore; snapshot cũ bất biến |

| Goal binding | Source requirement/AC + exact action | Code/evidence và closure |
| --- | --- | --- |
| NXG-SYS-02/03/05/08/13/14; NXG-X-02/07 | authorization00 effective order6–8 + mutation response; composition01; POAC001 | Cả ba service/API/UI; scenario matrices trong verification-plan; runtime/review pending |
| NXG-P02-01; NXG-RM10-01; NXG-FX16-G01/G02/G03 | FX16 BR001–005, AC001–003; `goals.goal.create/update/start/complete/abandon/reopen`, `goals.target.create/read/record_progress` | Numeric subset trước; Tasks/Boolean/history/lifecycle vẫn riêng backlog, không lấy numeric pass đóng AC Task |
| NXG-FX17-G01/G02/G03; NXG-X-13 | FX17 BR001–005/AC001–003; exact Habit actions ở mục5.3 | Authority+replay không tự implement actual daily reminder dispatch; G03 còn pending khi chỉ local metadata |
| NXG-FX15-G01/G02/G03; NXG-X-04 | FX15 BR001–005/AC001–003; `planner.plan.read/pin/reschedule/notes/reorder/unpin` | Source unchanged assertions, safe unavailable và cleanup |
| NXG-P03-01; NXG-X-05/06/10 | FX20/24/25/26 BR/AC tại từng WP | Source providers trước consumer; history/current projection tách rõ |
| NXG-SYS-10; NXG-X-09/11/12/14; NXG-P05-01/NXG-P06-01/NXG-P07-01 | FX06/29/33/39/40 + POAC007/008 | Exact per-slice action/AC ở P1; provider/production vẫn execution-gated |
| NXG-SYS-16 | current scope40FX + full semantic RTM | WP-RTM forward/reverse evidence, không đóng bằng inventory733 |

## 4. Kiến trúc authority, receipt, projection và consistency

### 4.1. Transaction algorithm

Reuse existing `SqlSelfCapability.IsAllowed(connection, transaction, actor, module, actions)`; không tạo authorization engine mới. Primitive này kiểm tra live Session/security stamp/account/PersonalSpace/current role/module/dependencies/resolved action và Admin Allow/Deny. Principal lấy từ cookie/session server, không body OwnerId. Preflight chỉ tối ưu fail-fast; không được thay current proof.

```text
validate syntax/typed fields (không private read)
begin Serializable transaction
  establish current actor + owner + module authority bằng cùng connection/TX
  inspect existing receipt trong bounded compatible namespaces
  if completed:
    validate trusted envelope/binding/original actions + current authority
    reconstruct original outcome; không execute business mutation
    stage DTO chỉ khi original-readable + current read + projection digest match
    otherwise safe ACK; legacy thiếu proof dùng reconciliation error đã định nghĩa
  else:
    claim receipt nếu có key; keyless vẫn đi toàn bộ authority path
    lock owned aggregate/source theo order đã review
    derive exact required action set từ trusted intent + current row + normalized diff
    check tất cả actions/dependencies bằng cùng TX
    validate fresh ETag + lifecycle + domain constraints
    mutate + audit + durable effects nếu thuộc installed slice
    stage authorized projection hoặc ACK
    persist minimal versioned receipt proof
  final recheck write/source proofs; re-evaluate read proof của staged detail
  write/source invalid => rollback business/audit/receipt
  chỉ read proof đổi => bỏ staged detail, cập nhật receipt thành opaque và
     recheck lần cuối; không downgrade original opaque thành readable
commit đúng một lần
return staged safe outcome; không SQL detail read sau commit
```

Nếu policy của slice đòi original read là prerequisite mutation (ví dụ catalog `goals.goal.update → goals.goal.read`, `habits.habit.update → habits.habit.read`), mất prerequisite là mất write proof và phải rollback. Không tự bỏ explicit dependency này. Đồng thời không thêm read mới cho create/transition/Record chỉ để tránh ACK. Đối với partial detail, chưa có approved selective-detail contract thì dùng ACK, không tự trả target/schedule/progress bị ẩn bằng zero hoặc giả empty.

Read List/Get: mở short Serializable TX, current read check, owner/lifecycle/source predicates trước query/count, stage rows + derived fields trong cùng boundary, dispose reader rồi final proof và commit read transaction. Bỏ `NOLOCK` ở Goal source query. Snapshot isolation chỉ thay thế sau khi chứng minh current-policy proof và dữ liệu snapshot cùng semantics; không bật DB option ngầm. Revocation commit sau command linearization không phủ định command đã commit hợp lệ.

### 4.2. Lock order có căn cứ và gate còn thiếu

Observed: Goal Create/Update/Progress receipt→Goal→Target; Transition Goal→receipt. Habit Create/CheckIn receipt→Habit, Update/Schedule/Transition Habit→receipt. Planner Pin receipt→Task/Project→pin/day; Update/Reorder/Unpin pin/plan→receipt. Existing Task `SqlProductivityService.MutateTask` có transitionSource Task lock trước receipt ở line494; không được tuyên bố một global order đã tồn tại.

| Family | Recommendation trong bounded rewrite | Điều kiện chứng minh |
| --- | --- | --- |
| Goals | current capability→bounded receipt namespace/key locks→Goal→Targets sorted trusted ID→progress/audit/receipt completion | Rewrite tất cả sáu methods cùng slice; create giữ receipt trước insert parent/child; transition không lấy Goal UPDLOCK trước receipt lookup |
| Habits | current capability→receipt→Habit→effective Schedule intervals→CheckIn slot→audit/receipt | Mọi mutation trên một Habit lock Habit trước slot. Unique `(OwnerId,HabitId,LocalDate)` giữ; schedule trigger NoOverlap giữ |
| Planner | receipt→same-owner plan-day key ranges theo ngày tăng→owned pins sorted SQL GUID order; source proof qua Tasks-owned participant cùng TX | Không đưa source Project UPDLOCK trước Task một cách tùy tiện. Xem tiếp Task/Project close/Trash/Calendar/reminder lock graph; chọn source lock mode/order tương thích trước code. SQL JOIN text order không chứng minh physical acquisition order |
| Authority | cùng SQL transaction từ entry tới final proof, actions normalized/deduped | Check absent grants/dependency rows dưới Serializable, deadlock traces; không mở connection phụ khi giữ business locks |

Planner source participant nên đọc current Task→Project bằng contract do Productivity sở hữu, trả source classification/version proof và safe projection; không cho Planner join thêm private tables. Cần inventory hết writers chạm pin/source trước chọn order cuối. Nếu graph chưa compatible: chặn riêng Planner mutation readiness, tiếp tục Goal/Habit/Windows/contracts. Không chữa bằng catch1205 rồi tự tạo key mới. Deadlock victim rollback toàn bộ; same immutable intent retry bounded chỉ khi outcome rollback đã rõ, uncertainty dùng receipt reconcile.

Planner ngày rỗng phải khóa range của index `IX_PlannerPin_Owner_Date_Rank`; move khóa cả old/new day theo cùng thứ tự, rồi verify source và pin. Reorder không update ranks trước validate exact day set; dùng rank assignment không tạo temporary collision nếu sau này có unique rank constraint. Hiện unique key chỉ owner/task/date, không được bịa unique rank đã có. Nếu cần day-level serialization primitive, proposal/ADR phải so với SQL range locks hiện tại; không thêm broker/Redis lock.

### 4.3. Receipt envelope và compatibility

Existing `identity.RequestReceipt` đã có `ResultCode`, `ResultStatusCode`, `ResultJson`; `SqlRequestReceiptStore` HMACSHA256, fixed TTL24h, subject dual-read operation-qualified và operation-independent namespace. **P0 ưu tiên không migration**: versioned safe envelope trong ResultJson. Không lưu raw DTO/title/body/note/schedule. Các private fields chỉ nằm tạm trong memory canonical input; receipt chỉ chứa keyed digest.

```json
{
  "version": 2,
  "operation": "habits.checkin",
  "ownerId": "<PersonalSpace UUID>",
  "actorId": "<User UUID>",
  "resourceId": "<Habit UUID>",
  "authorizedActions": ["habits.checkin.record"],
  "outcome": {"code": "HabitCheckInRecorded", "status": 200},
  "requestSchemaVersion": 2,
  "originalDetailAvailable": false,
  "projectionSchemaVersion": 1,
  "projectionDigest": null
}
```

Envelope fields là **PROPOSED**, validate exact finite operation/action/outcome combinations, nonempty IDs, current owner/actor binding; reject unknown versions/action hints. Reference IDs cũng private metadata: receipt không thành public/read endpoint. Projection schema/digest gồm mọi serialized field: Goal child/count/progress; Habit effective schedules/check-ins/streak/date-window/current-day boundary; Planner source title/status/Project/date/rank/order and all returned pins. Aggregate rowversion một mình không chứng minh original projection. HMAC primitive reuse `DigestSensitive` với purpose/schema separation; key từ existing configured idempotency secret phải ổn định qua restart, không dùng random per-service fallback trong durable runtime.

Canonical v2 là typed object serialized deterministic với named fields, null khác omitted theo contract, decimal invariant exact scale, dates ISO, route IDs, original quoted If-Match, requested lifecycle intent. Không dùng delimiter-joined title/note. Ví dụ `{"version":2,"operation":"habits.checkin","habitId":"…","ifMatch":"\"…\"","localDate":"2026-10-16","count":1,"note":null}`. Request body khác intent với cùng key →409 IdempotencyConflict; same normalized input coi tương đương chỉ khi source normalization cho phép.

| Trạng thái receipt | Kết quả đề xuất / compatibility |
| --- | --- |
| Fresh valid key | Claim + mutation + audit + completion atomic; syntax invalid key trả422 safe ValidationFailed, không claim |
| Keyless theo installed policy | Vẫn authorize/field/source/final-proof/ACK; không cam kết exactly-once khi retry create không key; không ép toàn bộ keyless thành422 |
| V2 original opaque | Current original write authority còn hợp lệ → cùng committed ACK dù read được cấp thêm |
| V2 original readable | Chỉ DTO khi current read/source/lifecycle và full projection digest đúng; derived field đổi → ACK, không trả latest DTO như original |
| Current write/session/account revoked | Deny safe theo common contract, không disclose receipt/resource; mutation không chạy lại |
| Legacy resultJson có trusted resource/action binding | Parse allowlisted historical format; giữ opaque nếu không original detail proof; ACK khi binding chứng minh được |
| Legacy code-only/null ResultJson của ba service hiện tại | Phân loại theo bảng legacy dưới đây: một số typed canonical v0 + trusted ResultCode/operation đủ binding để ACK/204; generated create hoặc ambiguous/composite action thiếu proof dùng409 reconciliation-needed (tên code **PROPOSED** `CommittedOutcomeUnavailable`). Không blanket reject mọi legacy, không đoán ID/action hay re-execute |
| Running/concurrent duplicate | Existing SQL uniqueness/range lock serialize; sau first commit đọc completed; first rollback claimant kế tiếp fresh. Bounded lock timeout trả documented RequestInProgress409, giữ key/body; không toast success |
| Expired ≥24h | Existing store reclaim cho phép fresh intent. Không thay TTL âm thầm; UI báo retry sau retention không còn dedupe guarantee, phải reconcile/abandon trước new command; boundary tests trước/đúng/sau24h |

Thay canonical digest không được làm v0 receipts thành fresh. Extend lookup store (PROPOSED internal API) để inspect existing dưới lock rồi so digest bằng đúng canonical version; dual-read v0 delimiter digest chỉ cho compatibility, v2 chỉ cho new writes. Không backfill private payload hoặc sửa historical receipts thành proof tưởng tượng. Hai subject hashes cùng tồn tại phải giữ conflict/expiry resolution của existing store.

**Legacy classification bắt buộc cho supported producer formats:** recompute exact old HMAC từ request typed đã validate và đối chiếu row Succeeded của đúng actor/operation/key; fixed typed prefix/route+ETag binds opaque resource ID. Current actor→same PersonalSpace mapping và original write/prerequisite proofs vẫn phải hợp lệ. Chỉ recognized ResultCode/operation combinations dưới đây mới cấp trusted action; không đoán từ current row. Không format nào thiếu original projection proof được trả DTO.

| Legacy v0 operation / trusted outcome | Binding / current original action | Safe result |
| --- | --- | --- |
| `habits.checkin` + `HabitCheckInRecorded` hoặc `HabitCheckInCorrected` | Fixed Habit UUID/quoted E/localDate/count prefix; note là free field cuối cùng nên không có delimiter ambiguity giữa hai free fields. ResultCode maps đúng Record hoặc Correct; `habits.checkin` namespace tự nó không là action |200 opaque Habit ACK; Record-only retry Record không đòi Correct. Count/date/note/E khác canonical phải conflict, không claim fresh |
| Goal progress + `GoalProgressRecorded` | Fixed Goal/Target UUIDs/E/invariant decimal rồi note cuối; exact operation `goals.target.record_progress`, current documented prerequisites |200 opaque Goal ACK; không append GoalProgress lần nữa |
| Goal transition + `GoalTransitioned` | Fixed Goal/E/requested enum; original namespace trong finite start/complete/abandon/reopen set xác định trusted original action |200 opaque Goal ACK dù fresh graph/ETag đã đổi; lookup alias trước current graph |
| Habit schedule + `HabitScheduleUpdated`; Habit transition + `HabitTransitioned` | Typed fixed fields, Habit UUID/E; exact schedule hoặc pause/resume/archive/unarchive namespace; supported known producer mapping |200 opaque Habit ACK, không insert schedule/change local day lần nữa |
| Planner reorder + `PlannerReordered` | Fixed date/E/ordered UUID list canonical; exact reorder namespace + all declared current write/source guards; first original pin ID là authorized anchor |200 opaque anchor ACK, không reorder lại; no full Plan legacy projection |
| Planner unpin + `PlannerUnpinned` | Fixed pin UUID/E and exact unpin namespace, current own actor/space/unpin proof; request HMAC proves original pin binding dù row đã xóa |204, no body/no source-read requirement; không xóa lần nữa |
| Goal/Habit create; Planner Pin | Generated result UUID không nằm trong legacy receipt; source Task UUID không phải generated pin identity. Không tìm current list rồi đoán |409 CommittedOutcomeUnavailable, no second create/pin |
| Goal/Habit metadata Update v0 | Multiple free title/description/unit fields trong delimiter canonical có thể ambiguous; thiếu unambiguous original intent proof |409 safe reconciliation, không accept một collision như same intent |
| Planner Update v0 | Route/date/note binding có thể verify, nhưng old ternary chỉ ghi một action khi date+notes cùng đổi; thiếu original before-state/composed action set |409 safe reconciliation thay đoán missing original action. New V2 lưu đầy đủ diff-action set |
| Unknown code/namespace/envelope version, inconsistent dual rows, unverifiable grammar/owner binding | Không có đủ trusted proof |409 reconciliation/conflict theo parser contract, no DTO/no execution; current denial trước receipt disclosure |

Legacy recognition là bounded compatibility reader dựa producer method formats đã đọc, cần reviewer xác nhận exact byte serialization/normalization/ETag syntax; không parse user-provided text thành action. Khi grammar/request digest không chứng minh được matching binding, fail safe thay cấp ACK. UI giải thích old outcome unavailable và chỉ explicit authorized reconcile; create missing ref không tự GET list để suy result. V2 new writes dùng structured canonical để hết ambiguity, existing v0 receipts không bị mutate/backfill.

Dynamic namespaces: Goal Active intent phải lookup bounded existing `goals.goal.start` và `goals.goal.reopen` trước derive action từ current state; Habits Update lookup `habits.habit.update` và `habits.habit.set_reminder`; Planner Update lookup `planner.plan.notes` và `planner.plan.reschedule`. Stable Habits CheckIn giữ `habits.checkin`, lưu Record/Correct trong envelope. Nếu legacy double-namespace outcomes mâu thuẫn, fail reconciliation; không chọn cái dễ pass. Thứ tự lookup bounded namespaces deterministic cho tất cả callers; không đổi operation key để bypass receipt.

### 4.4. ACK, status và safe projection

```json
{"id":"<authorized UUID>","committed":true,"detailAvailable":false}
```

Goal mutations dùng Goal.Id; Habit dùng Habit.Id; Planner Pin/Update dùng PlannerPin.Id. Create201, other detail-producing mutation200 giữ status của original outcome; Unpin giữ204 no body và completed retry204, resource binding chỉ trong receipt. Không gắn ETag của unreadable DTO vào ACK nếu common contract chưa cho phép. Error412 không tự echo current detail.

Planner Reorder chưa có persisted Plan UUID. **Chốt contract đề xuất cho slice:** status200, ACK `id` = UUID của **pin đầu tiên trong danh sách PinIds của original canonical request** sau khi toàn bộ selection đã được server validate cùng owner/day. Đây là documented authorized-resource anchor theo common00 (authorized opaque reference), không giả một Plan aggregate UUID. Lưu anchor nguyên trong receipt; same-key replay giữ cùng ID dù order/source/anchor lifecycle thay đổi. ACK chỉ nói original reorder committed, không nói anchor hiện readable/Active. Không random UUID, không dùng OwnerId và không cần thêm Plan table chỉ để trả ACK. API reviewer kiểm tra binding+no new disclosure trước đóng contract; nếu sau này đổi sang collection identity thì phải version contract/migration riêng, không tự thay shape trong remediation. UI reconcile theo immutable planDate+selection, không GET anchor tự động.

Planner source Trash/unreadable: contract FX15 cho unavailable/readonly state. Return owned-pin reference/date/rank/notes chỉ khi Planner read cho phép, source fields null/absent qua DTO union **PROPOSED**; không title/status/ProjectName/StartAt/EndAt cache. Terminal source còn readable có thể safe read-only projection, không đồng nhất terminal với unreadable. Tasks/Project ownership luôn server scoped. Unpin dùng owned metadata, không source-read; giữ Task/Event/reminder không đổi.

## 5. Work packages P0

### 5.1. WP-CONTRACT — Shared P0 contract trước code

| Field | Nội dung |
| --- | --- |
| Priority / risk / owner | P0 / High privacy+integrity / TL+backend owner; independent security/API reviewer |
| Sources | common authorization00, composition01, action module15/16/17, application contracts, RequestReceipt store, SQL migrations0014/0023/0024; R methods trong gap register |
| Before → after | Duplicate không recover và DTO leak → exact committed outcome có original-action proof và opaque branch |
| Implementation steps | Ghi action/input/error/response matrix mục6 vào story; định nghĩa typed mutation outcome (proposed `CommandOutcome<T>` gồm readable/opaque, dùng existing ACK convention); chốt envelope/parser/dual canonical versions; audit namespace aliases/TTL; chốt strict ETags và Planner anchor; ghi lock graph từng family; review consistency provider |
| Existing files cần đổi sau lệnh thực thi | `src/Nexora.Application/{Goals/GoalServiceContracts,Habits/HabitServiceContracts,Planner/PlannerServiceContracts}.cs`; `src/Nexora.Infrastructure/Identity/SqlRequestReceiptStore.cs` chỉ khi cần inspect/compatibility API; three API contracts/endpoints; `web/Nexora.Web/src/api.ts` |
| Proposed files | Bounded module receipt outcome helper hoặc shared safe envelope chỉ khi schema stable; không generic CRUD/auth abstraction. Story amendment tại thư mục plan hiện tại hoặc delivery contract phù hợp |
| Dependencies / commits | Không chờ Windows. Contract+negative scenario design đầu tiên; backend/API/UI change cùng buildable checkpoint, không publish ACK backend làm consumer crash; Reorder anchor đã chốt ở4.4, chỉ còn review/envelope binding |
| Verification / DoD | Mỗi matrix row trả lời input/action/read/ETag/key/legacy/status; original action độc lập namespace; no private JSON; review gate open rõ. Anchor đã định nghĩa; source lock order/response read-field contract chưa chốt vẫn chặn corresponding code |
| Migration / rollback | Ưu tiên ResultJson existing, không schema change. Rollback phải về phiên bản đã hiểu V2 envelope và ACK; revert về binary cũ không hiểu replay sẽ tái gây409 hoặc parse lỗi, không là rollback an toàn. Giữ compatibility reader tối thiểu24h sau last V2 write; không xóa receipts để rollback |

### 5.2. WP-GOAL — Goals numeric authority/replay/projection

Tình huống: Admin có create hoặc transition nhưng không read vẫn nhận GoalDetail; retry transition sau thành công gặp ETag mới trước khi thấy receipt. Một target khác Goal trong route phải unavailable, không chỉ cùng owner là đủ.

| Field | Contract/solution và cách implement |
| --- | --- |
| Priority / owner / sources | P0 High; backend+React owner; FX16 BR001–005/AC001–003, action16, UX16 progress/lifecycle, DB05 Goal/Target/Progress; source anchors mục3 |
| List/Get | `List` goal.read; nếu progress/count đọc target thì cần đúng target/progress read proofs hoặc approved safe summary contract. `Get` goal.read+target.read, bổ sung streak-equivalent progress guard khi exposed action contract đòi; W0 xác định exact progress action, không tự assume base read đủ. Query/count cùng TX, no NOLOCK; no title/count của denied sources |
| Create | `{title,description,startDate,endDate,numericTarget?}`; goal.create, thêm target.create khi child tồn tại; missing child action rollback Goal/Target/audit/receipt. Title1–200, description≤20000 theo installed contract, date end≥start. Numeric target title1–200, target>initial, scale≤8 và SQL decimal(28,8) range; reject overflow trước SQL, không làm tròn silently |
| Update | Existing current Goal + normalized field diff; `goals.goal.update` và explicit source prerequisite `goals.goal.read`. Không nhận lifecycle/OwnerId/target mutation trong generic body. Đối chiếu ignored NumericTarget trên PUT: proposed reject non-null thay vì silently drop, compatibility note cho client đang gửi null |
| Numeric progress | `goals.target.record_progress`; route Goal+Target same owner AND relationship; Kind Numeric; goal writable Draft/Active theo installed graph; validate scale/range/note≤2000; currentValue được ngoài initial/target nếu contract cho phép clamp, không tự cấm để đổi BR001. Preserve initial/target, append GoalProgress một lần, touch Goal rowversion; no automatic Complete |
| Transition | Derive start Draft→Active; complete Active→Completed; abandon Draft/Active→Abandoned; reopen Completed/Abandoned→Active. Original completed receipt không chạy lại graph của request mới. Archive/Trash/history chưa shipped không được bật trong P0 |
| Response | Safe GoalDetail chỉ current exact read/projection đủ; write-only create/start/progress hợp lệ trả Goal ACK; progress action có explicit read dependency phải theo actual contract, không tùy tiện gỡ. Digest gồm target values/progress/count/order, không chỉ Goal.ETag |
| Files / steps | `SqlGoalService`: tách parse/replay/fresh command; replace ModuleAvailable helper ở TX bằng overload conn/TX; change List/Get; final proof sau audit/CompleteReceipt. `GoalServiceContracts`, `GoalsContracts`, `GoalsEndpoints.MapResource`, api.ts mutation unions; `GoalsScreen` result branch trước `.goal`; exact getGoal khi explicit conflict compare |
| SQL / rollback | Không đổi applied0014; FK/precision existing giữ. Nếu range validation phát hiện data cũ vi phạm, report, không sửa data. Backend/UI rollback bundle giữ receipt V2 reader. Numeric formula/state unit regression + real SQL race/privacy/replay trong verification-plan |
| Dependencies / DoD | WP-CONTRACT + Goal family lock graph; Goal/Habit có thể làm độc lập. Done khi sáu operations trace đầy đủ, keyless cũng current proof, all/none composite rollback, no full detail without read, replay one effect và independent finding disposition |

Numeric SQL binding: `decimal(28,8)` nhận trị tuyệt đối tối đa `99999999999999999999.99999999`; validate trước SQL, đặt SqlParameter.Precision=28/Scale=8 rõ ràng, không dựa implicit provider inference/rounding. Canonical digest dùng invariant decimal đã normalize theo contract. Khi đọc client consumer phải kiểm tra input text→JSON→.NET decimal round-trip ở boundary; nếu JavaScript Number làm mất phần thập phân thì hoàn thiện exact decimal wire contract tương thích trước code, không đổi công thức clamp hoặc quietly giảm precision của dữ liệu nguồn để test pass.

### 5.3. WP-HABIT — Habits từng operation

Tình huống: owner chỉ có Record gửi một check-in, server commit nhưng response mất. Retry là **Record cũ**, không phải Correct mới; presence của row sau commit không đổi quyền gốc. Một request mới vào slot đã có mới cần Correct.

| Operation | Before → after / contract / steps |
| --- | --- |
| List | Hiện preflight habit.read và nhiều schedule/streak reads ngoài TX. Stage list+current schedule dưới same TX; field schedule/history/streak chỉ khi exact read contract đủ. Không trả protected schedule/note/progress từ count DTO; W0 chốt summary read versus `habits.streak.read`, không tự mở rộng grant |
| Get | Habit.read; check exact history/streak prerequisites cho from/to detail. Owner/date range validation trước query, all detail under one boundary; no private error echo |
| Create | habit.create; reminder non-null thêm habit.set_reminder; create current/future effective schedule atomic. Boolean/Count bất biến; targetCount null cho Boolean, >0 cho Count; weekday1..127, valid IANA, title1..100/unit≤50; missing reminder action rollback all |
| Update | Remove unconditional generic update requirement cho reminder-only nếu action composition xác nhận diff-only intent. Core title/unit/timezone →habit.update (+explicit habit.read prerequisite); reminder change/clear→habit.set_reminder; core+reminder cần cả hai. Kind/target/schedule không vào generic Update. Same connection checks, no auxiliary HasCapability |
| SetSchedule | habit.schedule, current Active; preserve existing future-only contract EffectiveFrom>habit-local today và non-overlap, current kind target rules; close exclusive end + insert version + touch parent atomic. Đây là installed policy, không tự đổi thành retroactive vì feature nói effective date; nếu cần today support ghi delegated contract reconciliation riêng |
| RecordCheckIn fresh | Stable `habits.checkin`; lock Habit→effective schedule→unique day slot. Không future/unscheduled/paused, Boolean0/1/Count≥0, note≤1000. Empty slot→checkin.record; present slot→checkin.correct; replace count không increment; schedule link cùng Habit và owner |
| RecordCheckIn replay | Read receipt trước ETag/lifecycle/local-today fresh checks, parse original `HabitCheckInRecorded` action proof; current Record đủ, không Correct; no second check-in/audit/updatedAt. Nếu original opaque/read revoked/derived streak đổi thì ACK; không cấp read thêm cho Record |
| Transition | Pause/Resume/Archive/Unarchive exact graph + preserved PreArchiveState, schedule pause intervals không rewrite past. Receipt replay trước current transition derivation. Final write/source checks sau all schedule/Habit/audit/receipt writes |
| Reminder | Current `set_reminder` chỉ local time metadata; P0 không dispatch/create actual reminder job. Full FX17 BR005 all3 + achieved-before-dispatch cancellation thuộc WP-HABIT-REMINDER tiếp theo, dependency module-owned reminder acceptance; không report complete vì lưu giờ |

Owner: backend+React developer; priority P0 High, sources FX17 BR001–005/AC001–003, HabitPolicy, action17, UX17, DB05 and migrations0023/0024/0029. Files: `SqlHabitService`, `HabitPolicy` chỉ khi cần deterministic clock injection (không đổi business formula), Habit service/API contracts/endpoints, api.ts, `HabitsScreen`; proposed focused `HabitReceiptOutcome` nếu cần. Preserve `HabitReceiptOperations.CheckIn` literal.

Thứ tự implement: typed outcome+legacy parser → same-TX List/Get → Create/Update/Schedule → CheckIn replay/action → transitions → UI uncertainty/recovery. Stage `ReadDetail(connection,transaction,...)` trước commit; xóa mọi `ReadDetail(connection,null,...)` sau mutation commit. Clock snapshot capture một lần/command từ controlled clock; historical LocalDate không reinterpret khi đổi timezone. Digest includes schedules/check-ins/date-window/current streak and clock boundary; replay qua midnight có thể ACK thay latest detail.

Migration P0 không cần đổi unique owner/Habit/date hoặc NoOverlap trigger; additive-only nếu future reminder rule cần table/index. Rollback backend/UI cùng receipt compatibility reader, giữ ScheduleId/history. DoD: Record-only retry test pass nhưng new intent correction deny; missing từng composed action rollback; timezone/DST/history assertions giữ; independent auth/concurrency review. Tests cụ thể H01–H10 trong phụ lục; chưa thực thi.

### 5.4. WP-PLAN — Planner source và metadata

Tình huống: pin vẫn có sau khi Task vào Trash hoặc mất read. Planner không được dùng title/ProjectName cũ. Owner vẫn phải dọn pin theo `planner.plan.unpin`; cleanup không reschedule/delete Task.

| Operation | Contract/solution |
| --- | --- |
| List | planner.plan.read + current tasks.task.read trên mỗi returned source; bounded from/to inclusive span≤32 ngày theo PlannerPolicy; omitted date→owner IANA today trong TX. Stage safe per-pin unavailable branch, count/ETag phản ánh authorized output; không zero giả cho denied plan |
| Pin | planner.plan.pin + Tasks-owned source current read/lifecycle/owner. Active Task+Project mới được pin; same owner/task/day unique; rank allocated under day range lock. No Task/Event/reminder write. Response pin201 hoặc pin ACK, final source recheck |
| Update | Lock own pin; normalized date changed→planner.plan.reschedule; note changed→planner.plan.notes; both→both. Hiện ternary chọn một action không đủ khi cả hai thay đổi. Date move locks old/new day sorted, duplicate target-day conflict409, no partial move; unchanged field không thành quyền khác |
| Reorder | planner.plan.reorder + source guards theo action15, full selection same owner/day, IDs distinct1..100, exact set match và plan ETag. Validate entire set trước any rank write, serialize same day incl empty range, derive output auth separately, no full PlannerPlan for reorder-only. Original replay không reorder state mới |
| Unpin | planner.plan.unpin + owned pin/current ETag. Read metadata khỏi PlannerPin không join Task; terminal/Trash/source-read denied vẫn removable theo action15. Delete pin only, audit+receipt atomic; fresh success204; replay204 dù pin nay không còn |
| Source contract | PROPOSED Productivity-owned participant accepts current actor/owner/conn+TX và typed Task IDs, returns current eligibility/projection/revision proof. Không HTTP nội bộ, không connection riêng, không thêm cross-module private join. Task missing/Trash/disabled không lộ cached fields; terminal-readable phân biệt unavailable |
| Plan ETag | Existing rowversion-set digest phục vụ optimistic plan revision không phải projection replay proof. Digest riêng cover date range/order/rank/note + source fields/revisions; stale source projection không được trả như original outcome. ACK anchor là original PinIds[0], status200 theo4.4, lưu và validate trong WP-CONTRACT |

Priority P0 High; owner backend+React+Productivity maintainer; FX15 BR001–005/AC001–003, action15 exact guards, UX15, `PlannerPolicy`, DB05 PlannerPin; compatibility lock gate mục4.2. Files: `SqlPlannerService`, Planner service/API contracts/endpoints, Productivity-owned new participant (**proposed**, không export raw SqlProductivityService), api.ts, `PlannerScreen`. API client Unpin giữ204 void branch; rest union. UI không Promise.all plan load với Task picker rồi coi source denial là global empty; picker/query generation fence độc lập, unavailable plan vẫn có safe exit/cleanup.

Migration: P0 source/authority fix dùng existing pin schema; ACK resource anchor không cần persisted Plan identity. Nếu SQL lock analysis chứng minh cần index mới thì migration additive riêng sau0058, exact number phân bổ lúc thực thi. Không mutate0058/0023/0024. Rank rollback restore qua reviewed plan intent/revision, không replay stale command với key mới.

DoD: P01–P10 SQL/source/privacy/reorder cases, Task/Event/reminder unchanged, same-key one effect, all-or-nothing, actual read and response boundary review; source-lock graph unresolved thì WP-PLAN **CONTRACT_PARTIAL**, không claim sẵn code.

### 5.5. WP-WIN — Actual A/B/C Windows process identities

Priority P0, security acceptance gate High; owner Windows harness developer + independent Windows/security reviewer. Existing files: `tests/Nexora.UnitTests/LocalAccountMessageSinkTests.cs`, `Program.cs`, `TestRunner.cs`, `scripts/dev/test-filesystem.ps1`, `.github/workflows/m01-ci.yml`; production sink `src/Nexora.Infrastructure/Identity/LocalAccountMessageSink.cs` là policy reference, không tự sửa DACL để test dễ pass.

Observed policy: directory/file protected DACL, allowed runtime/operator SIDs **FullControl**, không phải B read-only. `CreateWindowsAclPolicyCore`, `ValidateCurrentWindowsIdentityCore`, private file creation/validation và pending→final/restart paths phải giữ. C phải thử OS filesystem trực tiếp bên cạnh app read; app tự từ chối identity không chứng minh OS deny.

1. Preflight actual Windows disposable runner/VM, ACL APIs, account/token launch privilege; fail nonzero/Blocked nếu thiếu, không skip-pass. Lượt plan này không tạo account hoặc thử launch.
2. Recommendation launch dùng supported Windows credential `ProcessStartInfo` hoặc `CreateProcessWithLogonW` sau privilege spike; API sau tự authenticate, không cần gọi LogonUser trước. Nếu chọn token-based `CreateProcessAsUser` thì logon/token lifecycle là nhánh riêng cần review. Không dùng `LOGON_NETCREDENTIALS_ONLY`: nó giữ caller local token, không chứng minh A/B/C. Child tự lấy `WindowsIdentity.GetCurrent().User`, token group/privilege diagnostics chỉ safe booleans. Kiểm tra quyền Log On Locally/window-station/desktop/startup handles trên runner; không đổi global policy để né fail. [Microsoft API contract](https://learn.microsoft.com/en-us/windows/win32/api/winbase/nf-winbase-createprocesswithlogonw).
3. Tạo ba nonadmin accounts A/B/C với bounded≤20-char Windows names và random run nonce, passwords trong private parent memory/ACL-protected channel. A runtime và B operator đúng SID policy; C không Administrators/Backup Operators, không SeBackup/Restore/Debug đặc quyền. Parent orchestrator có quyền setup chỉ trong disposable scope; không dùng nó làm negative probe.
4. Run-owned manifest chứa run/attempt/nonce, actual account SID/creation proof, paths, process PID+creation time. Nếu account/path trùng có sẵn thì refuse; không reuse prefix. Stage probe binaries/config ngoài private capture, cho C read/execute và traverse parent/staging; không cấp DELETE_CHILD/write trên capture parent cho C vì có thể bypass child delete boundary. Control file đọc được bởi C để chứng minh C đến đúng capture boundary. Không cấp Users/Everyone quyền rộng vào capture.
5. A construct sink thật và Publish→commit/reconcile bằng configured operator/runtime policy; B chạy actual operator `ReadCaptured`/allowed workflow trên canary synthetic, chứng minh digest match; không copy capture ra staging để B đọc giả.
6. C cùng effective SID thử enumerate/list, open/read, create, overwrite, delete, set DACL/ownership theo standard token; assert OS access-denied + object không đổi. Check intermediate temp/pending/final, replace/rename atomics không lộ content. Expected denial phải nhận đúng unauthorized/native access error, không coi file-not-found/missing executable/crash/timeout là pass.
7. Restart A dưới cùng runtime SID; write/read/reconcile lại; B vẫn allowed, C vẫn denied. Insecure inherited hoặc foreign ACE fixture phải bị sink reject, không tự repair directory ngoài run. DACL inspection hỗ trợ, không thay actual process proof.
8. Deadline cho từng child, parent cancellation/failure finally cleanup. Terminate chỉ PID+creation time+nonce ownership đúng, không kill theo tên. Cleanup accounts theo recorded SID+creation ownership, files dưới resolved run root; không recursive delete computed path chưa verify. Nếu ownership không khớp, giữ artifact manifest secret-safe và fail cleanup gate.
9. Hook executable suite actual `AddWindows` assertion thay permanent Skip **chỉ khi probe chạy thật**; `--require-os windows` vẫn fail nếu unavailable. PowerShell wrapper và workflow giữ nonzero propagation; Linux suite không thay Windows.

PROPOSED CLI (không nhận password/key trên command line):

```text
Nexora.UnitTests --capture-acl-probe --mode runtime|operator|foreign \
  --scenario <allowlisted-id> --nonce <public-run-nonce> --control <run-owned-path>
```

Private credentials/envelope key vào inherited authenticated pipe hoặc per-SID config ACL; report đặt ở kênh child-owned, parent validate nonce/PID/effective SID và expected scenario, không nhận report giả từ process khác. Probe không tự chọn path arbitrary ngoài manifest.

```json
{"schemaVersion":1,"run":"…","attempt":1,"nonce":"…","scenario":"foreign-read-final",
 "pid":123,"processCreatedAt":"…Z","effectiveSid":"S-1-5-…",
 "admin":false,"privilegedBypass":false,"reachedBoundary":true,
 "osResult":"AccessDenied","nativeError":5,"captureUnchanged":true,
 "canaryMatched":null,"cleanup":"owned-and-removed"}
```

Shape mới là proposed; không token/password/encryption key/plaintext capture trong JSON/stdout/JUnit/screenshots. Native error5 chỉ ví dụ expected Win32 result, exception mapping phải test trên selected launcher. Negative operation reports attach exact path **kind**, không sensitive absolute path khi không cần. For positive A/B dùng canaryMatched true, không expose content.

Migration none. Rollback harness về previous implementation vẫn Windows fail; tuyệt đối không remove required flag/Skip→pass để rollback xanh. DoD: 3 distinct actual PIDs/SIDs/token contexts; A/B positive; C OS negative vì capture DACL; restart/temp/final/insecure fixture; deterministic timeout/failure/cancel cleanup; required CI assertion executed; independent reviewer xác nhận report provenance. Chưa có privilege/launcher evidence ở plan-only nên readiness **DESIGN_READY, ENVIRONMENT_UNVERIFIED**.

## 6. API/DB/receipt contract matrix

Mọi row dùng server principal/PersonalSpace, no forged owner/context. Existing paths dưới `/api/v1`; `K` = optional UUID Idempotency-Key theo installed behavior, preserved namespace+24h policy; `E` = strict quoted8-byte rowversion hoặc plan digest đúng endpoint; replay original E không fresh compare. Giữ existing CSRF/Origin/session middleware và protected `Cache-Control: no-store`. Malformed JSON400/unknown fields422 phải bị binding/validation deny trước writes, không silently ignore protected fields; module-specific body limits phải đủ approved content contract, không áp M01 auth64KiB cho Documents1MiB một cách máy móc.

| Operation | Input / E | Required fresh-write action(s) | Response/read / receipt target | SQL invariant / errors |
| --- | --- | --- | --- | --- |
| GET goals / goals/{id} | status/query/limit or id; no K/E | goal.read; detail target.read; derived fields exact read contract W0 | GoalPage/GoalDetail only current safe data | owner/lifecycle/child counts consistent; invalid filters422; missing404 |
| POST goals | GoalRequest + optional NumericTarget; K | goal.create [+target.create] |201 detail or ACK Goal.Id; namespace goals.goal.create | Goal+Target+Audit+receipt atomic; bounds422; no partial child |
| PUT goals/{id} | metadata only; E+K | goal.update + catalog goal.read prerequisite |200 detail if full read or ACK; Goal.Id | immutable/lifecycle409; fresh stale412; E invalid428 |
| POST goals/{g}/targets/{t}/progress | decimal currentValue/note; Goal E+K | target.record_progress + documented explicit prerequisites |200 detail or ACK Goal.Id | target belongs route Goal/owner/Kind Numeric; invalid422, missing404, locked409 |
| POST goals/{id}/transition | status; E+K | start/complete/abandon/reopen derived current graph |200 detail/ACK Goal.Id; finite existing namespaces | replay before graph; no auto Complete; state409 |
| GET habits / habits/{id} | state/query/limit; from/to detail | habit.read; history/streak exact projection requirements W0 | safe HabitPage/Detail | read schedules+streak same boundary; bounded dates; no note leak |
| POST habits | create fields+initial schedule+optional reminder; K | habit.create [+habit.set_reminder] |201 detail/ACK Habit.Id | mode/target/mask/IANA/title validation422; atomic initial schedule |
| PUT habits/{id} | title/unit/timezone/reminder; E+K | semantic update/read prerequisite and/or set_reminder |200 detail/ACK Habit.Id; finite legacy namespaces | missing one action no change; same key diff body409 |
| PUT habits/{id}/schedule | effectiveFrom/mask/target; E+K | habit.schedule |200 detail/ACK Habit.Id | nonoverlap/exclusive interval/history preserved; ScheduleConflict409/history422 |
| POST habits/{id}/check-ins | localDate/count/note; E+K | Record if absent, Correct if present; original action on replay |200 detail/ACK Habit.Id; namespace habits.checkin | unique day; no future422/unscheduled409; repeat no extra row/audit |
| POST habits/{id}/transition | Active/Paused/Archived/Unarchive; E+K | exact pause/resume/archive/unarchive |200 detail/ACK Habit.Id | previous state+schedule history atomic; stale412/lifecycle409 |
| GET planner | from/to or owner today | planner.plan.read + source read per contribution |200 safe Plan with unavailable branch | 1..32 local dates; current authorized count/order/projection |
| POST planner/pins | taskId/planDate/notes; K | planner.plan.pin + Tasks source contract |201 pin/ACK pin.Id | UQ owner/task/day409; active source only; no Task changes |
| PUT planner/pins/{id} | planDate/notes; E+K | notes and/or reschedule by diff + source guards |200 pin/ACK pin.Id | both date ranges locked; target day duplicate409; no partial move |
| POST planner/reorder | planDate/pinIds1..100; plan E+K | planner.plan.reorder + declared source guards |200 Plan only if read; otherwise ACK original PinIds[0], stored in receipt | duplicate IDs422; foreign/mixed/missing set safe409/404; stale412; all-or-nothing |
| DELETE planner/pins/{id} | no body; pin E+K | planner.plan.unpin; owned metadata, no source-read |204, same204 replay; pin binding in receipt | no Task/Event/reminder delete; source missing still cleanup |

Common errors giữ safe contract:401 current unauthenticated;403 explicit ActionDenied khi current mapping hỗ trợ, module/dependency unavailable409 code;404 wrong owner/unknown resource;422 validation;428 missing/invalid precondition;412 stale fresh revision;409 IdempotencyConflict/RequestInProgress/lifecycle;503 persistence unavailable. Hiện services gộp denied vào ModuleUnavailable409: không tự đổi toàn error taxonomy trong auth repair; nếu tách dùng `SqlCapabilityStatus` phải cập nhật API/client/tests/docs cùng slice. UI bắt cả semantic code, không chỉ status.

Envelope legacy reconciliation code/projection action set là contract deltas phải đóng trước corresponding implementation; Planner ACK anchor đã được định nghĩa ở4.4 và cần review parser/consumer cùng slice. Unsupported actions tiếp tục unavailable, không thêm grant/enable prefix hoặc endpoint generic để chạy catalog.

## 7. UI state/journey matrix

Áp dụng cho `GoalsScreen`, `HabitsScreen`, `PlannerScreen` và reuse patterns đã repaired ở Finance; không chép lại một màn hình generic.

| State / trigger | State cần giữ/xóa | Request / UX / focus |
| --- | --- | --- |
| Loading | Không render old actor private data; skeleton khác empty | aria-busy; generation keyed actor+route+filters, cancellation/fence |
| Ready | Chỉ current authorized list/detail | exact selected-ID read độc lập pagination; Planner source unavailable card không chứa hidden title |
| Editing | Draft, base ETag, typed normalized intent riêng | dirty navigation/beforeunload, labels/errors; source read mới phải explicit khi cần compare |
| Submitting | Freeze pending `{route,method,body,key,ifMatch,actorGeneration}` | Disable duplicate submit and edits changing intent; closeBlocked/focus trap giữ |
| OutcomeUnknown | Giữ nguyên immutable pending request in memory; không current draft rebuild body | timeout/network/5xx chưa chứng minh rollback → explicit retry **same body/key/ETag**; không onChange clear key; không browser persistent private payload |
| CommittedReadable | Apply confirmed DTO, clear matching pending only | Explicit authorized subsequent refresh theo screen contract; không cần duplicate read nếu DTO đủ |
| CommittedOpaque | Xóa private list/detail/draft/selection/conflict preview thuộc affected projection; retain only safe ACK/outcome | “Đã lưu, chi tiết không khả dụng”; không `.goal/.habit`, không fake empty/zero, không auto GET hoặc re-submit; focus status/safe heading, aria-live |
| RevisionConflict412 | Giữ authorized draft, không overwrite với latest | Explicit exact getGoal/getHabit; Planner explicit selected-day plan fetch đúng scope. Compare then new intent/key with new E, không dùng row trang đầu |
| Validation422 | Chỉ field errors safe, command đã xác nhận không commit | Fix creates new intent according contract; không same key với body đổi nếu receipt đã reserved/succeeded |
| Denied401/403/404 hoặc409 ModuleUnavailable/DependencyUnavailable | Clear private caches/drafts/picker/selection/pending protected body; invalidate generation | Security clearing ưu tiên dirty confirm; safe exit/Home; stale late response không repopulate |
| Explicit abandon | Clear pending sau confirmed warning; không nói rollback | “Server có thể đã commit; dừng đối chiếu không hủy kết quả”; next command new key, explicit read nếu được phép |

Goal journey: create without target→201 ACK khi no goal.read; clear editor/private detail, không gọi getGoal/listGoals. Habit journey: check-in response lost→frozen same original key/date/note/count/E; retry Record dù UI có latest CheckIn không đổi thành Correct. Planner journey: Task source revoked→source fields cleared; unpin confirmation metadata-only vẫn allowed theo own pin rights; no Task mutation.

Reflow desktop và mobile320px; Week view day tabs/list giữ selected week; keyboard move/reorder thay drag-only; dirty Back/unload; inner conflict/abandon modal không phá outer inert/aria isolation. Reuse `ResourceFormDialog`, `modalIsolation.ts`, `navigationGuard.ts`, giữ `closeBlocked`, focus return nếu trigger gone→safe heading. StrictMode giữ bật; no-auto-read assertion so settled pre-mutation count, không giả initial count=1.

## 8. Test matrix và quản lý bằng chứng

Chi tiết setup/action/assertions/environment từng module ở [verification-plan.md](verification-plan.md). Tất cả tests/commands là **PLANNED**, không executed trong lượt này. SQL/browser chỉ dùng disposable verified-owned target. Unit/component state machine, controlled-HTTP browser và real API/SQL/browser là ba loại evidence riêng; không cộng thành business coverage. Final artifact ghi revision/fingerprint, exact test names, counts/skips/reason, run/job/artifact IDs và reviewer.

## 9. Dependency, critical path và commit plan

| Wave | Work / dependency | Buildable checkpoint và gate |
| --- | --- | --- |
| W0 | Rebaseline R/local dirty; WP-CONTRACT; module exact read/action sets; source/lock graph; Windows launcher design; WP-RTM | No unresolved action/receipt ambiguity cho slice tiếp theo; actual implementation chỉ sau user execution request |
| W1-G | Goal typed contract+backend/API+client/GoalScreen | Composite rollback, safe ACK/replay + real SQL + browser; giữ V2 reader rollback |
| W1-H | Habit same-TX/read/receipt/field diff/check-in/schedule/UI | Record-only retry, no Correct escalation, history/time tests; no reminder-provider enable |
| W1-P | Planner after source contract+lock graph+ACK anchor | Safe source/cleanup/atomic day reorder; concurrency with Task close/Trash |
| W1-W / W2 | Windows A/B/C independent stream; final sensitive review queue chạy theo ready fingerprints | Real Windows required assertion; không CI skip-pass; review author khác reviewer |
| W3 | Shared provider/ref/query/config contracts → DOC1/ORG1/DISC1/DSH1/NTF1 | Provider boundary + safe fields + versions+current authority trước consumer; không chờ Windows để viết contract |
| W4 | NEWS stored→Reading/watch; GH stored/snapshot; CAREER Resume/Event; LEARNING evidence→plan/cert/worklog; confirmed40FX gaps | Each capability exact contract/API/DB/UX/AC; real provider execution stays disabled |
| W5 | Semantic forward/reverse audit; final regression; independent re-review và acceptance report | No forgotten active source requirement; unresolved gates giữ product INCOMPLETE |

Mỗi slice có bốn logical commit boundaries: contract/scenario harness → server/API → client/UI → evidence/review disposition. Gộp server+client khi cần để mỗi published checkpoint buildable và consumer hiểu ACK. Harness/tests có thể red trong local author cycle nhưng không công bố checkpoint giả verified. Không commit/push/PR mutation trong lượt plan này. Developer sau này làm trên existing PR4, trước tiên reconcile local dirty với remote bằng workflow được owner xác nhận nếu có overlapping edits, không reset/drop.

Rollback mỗi slice: stop affected enablement theo existing policy khi có regression, keep data+receipts, deploy last compatible backend/UI bundle; additive schema giữ nguyên nếu binary rollback đọc được. Rehearse rollback trong disposable target với existing V2 and legacy receipt+historical versions; không down-migrate/xóa applied receipts/keys. Không có estimate ngày công trước source/lock/Windows privilege spike; critical path là contract/provider và review availability, không một finish date bịa.

## 10. Independent review checklist

Gate hiện **PENDING**: chưa có reviewer độc lập được chỉ định và không spawn agent trong lượt này. TL self-review tài liệu không là final security review.

| Queue / exact sensitive delta | Reviewer phải kiểm tra | Closure evidence |
| --- | --- | --- |
| Notifications current/new | `SqlNotificationService`, contracts/endpoints, UI NotificationPrivacy: no ambient SuperAdmin SYSTEM, registered writers, final proof sau receipt, opaque/read-source fallback, safe counts/batch | Reviewer khác author, exact blob/tree fingerprint + finding disposition; source fallback không được ghi resolver complete |
| Documents digest | `SqlDocumentService` + `SqlDocumentReplayProjectionTests`, Tag labels/current/history | Tag rename không làm replay return new DTO; opaque never upgrades; full projected digest no body in receipt |
| 31-key SELF eligibility | `src/Nexora.Domain/Access/ActionGrantPolicy.cs`, both eligibility and allow validation | Exact31 shipped keys/canonical contexts, no prefix grants/actual grant changes/SYSTEM/restore-version exposure |
| Owner-revoked share metadata | `SqlSharingService` IsDeleted exception owner-management paths, read gates/cursors/epoch | Revoked inactive/token-null; LINK resolver404; source off/foreign owner still deny |
| Shared modal | `modalIsolation.ts`, `ResourceFormDialog`, `navigationGuard`, App shared dialogs | nested lifecycle reference count, closeBlocked, return focus, no global isolation disable |
| Finance recovery/defaults | `SqlFinanceService`, cursor, API, `FinanceScreen`, ResourceFormDialog, api.ts | exact-ID recovery, immutable unknown intent,409 clear, opaque no read, per-currency totals, blank explicit currency, owner-IANA date in both entry points |
| Goal/Habit/Planner | Entire affected call graph incl capability/receipt/source provider/API/client | Keyless/fresh/replay original actions; full normalized diff; final proof; no private projection/log; actual concurrency lock order |
| Windows | child launcher/probe/orchestrator, TestRunner required flag, filesystem wrapper/workflow | Actual token provenance; C reaches boundary; DACL matches production; privileged bypass absent; timeout/cancel cleanup ownership |

Review record: reviewer identity, author identity, revision/tree + dirty/source hashes nếu chưa commit, scope paths/methods, finding severity+counterexample, fix commit, retest reference và reviewer re-review. Không close High bằng static verifier/CI success/author đọc lại. Review scope mới sau code change invalidates affected older review; không dùng earlier bounded Finance/Bookmark review certify later deltas.

## 11. Decision/blocker register

| ID / loại / owner | Capability thật sự chặn | Options / recommendation / tác động | Work vẫn độc lập |
| --- | --- | --- | --- |
| D01 workspace reconciliation / integration owner | Bất kỳ implementation overlapping local12 modified/untracked với remote5commits | Preserve hashes; compare each dirty delta với R trước integrate; không reset/pull mù. Không cần làm mutation này để lập plan | Read-only R research và plan artifacts |
| D02 technical API / TL+API reviewer — RESOLVED_PROPOSED_CONTRACT | Planner Reorder opaque response contract review, không còn lựa chọn UUID để developer đoán | Status200; original PinIds[0] đã authorized làm anchor, lưu receipt, stable replay; không add Plan schema/no random/no OwnerId. Common00 cho authorized opaque resource ref. Reviewer xác minh parser/consumer/binding, không approval nghiệp vụ mới | Other P0 operations, Windows, P1 contracts |
| D03 security compatibility / backend+reviewer | Legacy compatibility reader review | Typed v0 CheckIn/progress/transitions/schedule/reorder/unpin đã có bounded ACK/204 mapping ở4.3; generated create/ambiguous metadata/missing composite proof dùng safe409. Dual canonical read trước new claim; reviewer xác nhận exact supported producer bytes, không blanket legacy failure | New V2 paths + UI safe reconciliation |
| D04 concurrency technical / Productivity+DB reviewer | Planner source participant final lock protocol | Inventory source writers, use existing ordering/lock modes; measured deadlock graph. Không chọn global Project→Task trái existing Task-first path | Goal/Habit and source-independent P1 contracts |
| D05 projection contract / module owner | Goal summary progress/target count; Habit list/detail schedule/streak fields | Exact separate read action sets vs approved reduced DTO. Recommendation ACK for mutation when complete detail unauthorized; explicit update prerequisites giữ | Mutation authority/final proof; no speculative read grant |
| D06 environment / Windows harness owner | Actual A/B/C runtime evidence | Disposable Windows with account/token launch privileges; fail blocked if unavailable; credential launch vs token API spike. Không chỉnh machine hiện có trong plan | Harness design, SQL/UI/P1 |
| D07 workflow / review coordinator | Closing security-sensitive remediation | Assign non-author reviewer, frozen fingerprint; no delegation approval inferred from role docs | All authorized preparation and evidence mapping |
| D08 business / PO | Advanced Finance, Task extensions | Current DEC006/007 keep denied; không infer from SQL columns. Decision request phải exact capability/options/data/UX/security impact | Manual Finance/flat Tasks/current P0 |
| D09 technical security / security owner | Encrypted Vault slice | Complete threat model, KDF/root/wrapping/recovery/recent-auth/package/tamper/key-lifecycle contract trước code; no metadata substitute/operator plaintext | Other40FX/field allowlists |
| D10 field allowlist / source+security owner; PO chỉ khi disclosure mới | Sensitive Share/Support/Emergency flows | Separate context-specific fields+owner preview; default hidden; no shared raw DTO | SELF Resume/Learning/Collections core |
| D11 execution permission / owner | Real network/provider/production/paid services | Local adapter contracts/simulation prepared; configuration/credential presence không grant execution; do not request blanket local approval | Stored data, guards, contract tests planned |
| D12 RTM evidence / analyst+QA | Product COMPLETE verdict | Complete unread/selected source semantics + exact per-operation acceptance; do not reclassify extracted metadata as normative | Ship/review bounded independent slices |
| D13 current source reconciliation / TL+module/security owners | Organization remove-link; Learning Plan source kinds; Certification credential persistence | UX24 cho safe remove unavailable member, action24 guard chỉ nói source-read khi thêm/xem: chốt metadata-only cleanup theo đúng verb, không tự đòi read để cleanup hoặc mượn Planner rule. Feature/phase Learning cho Goals/Tasks/Courses nhưng DB/UX có Skill: giữ source requirement ưu tiên, reconcile typed allowlist trước LRN3. Classification CredentialIdEncrypted cần encryption/key/recent-auth fields contract riêng trước cert code; không persist plaintext mặc định | P0, ORG catalog/collection metadata, Skill/Course/evidence không phụ thuộc disputed field |

Documents “move” không là open product decision: current FX20 BR002 và no-action list cấm move/reparent; implement current-location navigation/create đúng hierarchy, **không thêm move endpoint**. Nếu muốn thay business rule, cần PO requirement amendment ngoài kế hoạch này. Q11 DOCX/MD và Q12 internal Event đã resolved, không hỏi lại để chặn owner flow.

## 12. Definition of Done và readiness

Operation implementation done: exact normalized input/action/field/source/precondition/lifecycle/response/receipt contract tồn tại và code/server/client phản ánh cùng semantics; final reviewed source fingerprint được ghi. Verification done: exact negative/privacy/race/replay/UI assertions thực chạy đúng target+revision, evidence secret-safe; expected skip/platform state rõ. Review done: independent findings re-reviewed/closed, no unresolved High/Critical trong selected scope. Chỉ khi ba phần đều đạt mới đóng operation.

Slice done: tất cả operations+errors+consumer+legacy/migration/rollback thuộc slice được trace, tests fresh, review complete; no accidental scope/permission/provider expansion. Module/R1 done cần toàn committed requirements, cross-module acceptance và unresolved gates disposition hợp lệ; selected green suite không đủ. Windows failed hoặc required independent review pending giữ toàn remediation/product **INCOMPLETE** dù SQL/backend xanh.

| WP | Implementation-readiness ở cuối khảo sát kế hoạch | Gate trước code/closure |
| --- | --- | --- |
| CONTRACT | DESIGN_SPECIFIED | D02 resolved proposed contract; còn D03–D05, validate canonical compatibility + original action mapping |
| GOAL | CONTRACT_PARTIAL, giải pháp cụ thể | Exact derived read fields/action set; legacy reader; family lock review; runtime/review chưa chạy |
| HABIT | CONTRACT_PARTIAL, giải pháp cụ thể | summary/streak read contract, reminder-only diff/dependency doc reconciliation, V2/clock boundary proof |
| PLAN | CONTRACT_PARTIAL | ACK anchor đã chốt; còn source participant + source writer lock graph, safe unavailable DTO và review compatibility |
| WIN | DESIGN_READY / ENVIRONMENT_UNVERIFIED | Selected launcher/privilege proof trước harness execution; actual3SID/report/cleanup gate |
| REVIEW | QUEUE_READY / PENDING_ASSIGNMENT | Reviewer khác author và fingerprint final |
| P1 individual WPs | Xem readiness riêng tại phụ lục | Contracts đề xuất phải reconcile API/physical schema/UX/AC trước code, không blanket “R1 ready” |
| RTM | PROCESS_READY / SEMANTIC_WORK_REMAINING | Đủ40FX và source unread/selected exact semantics; không requirement denominator giả |

## 13. Hướng dẫn developer bắt đầu

1. Đọc AGENTS/engineering profile/core/routing + current PO014, rồi task brief và baseline diff. Lấy remote R làm implementation reference; bảo toàn dirty test/script. Đọc action00/01 và action15/16/17, application contracts, SQL0014/0023/0024, `SqlRequestReceiptStore`, `SqlSelfCapability` trước sửa method.
2. Viết contract/scenario đầu tiên `Habit_RecordOnly_CompletedReplay_DoesNotRequireCorrect` (proposed test name): real SQL current Admin chỉ Record, create original check-in dưới synthetic setup, commit, drop response, retry same key/body/E; assert original action Record, một row/audit, không Correct check, outcome ACK khi no read. New key vào same day phải deny nếu thiếu Correct. Code đầu tiên cần tách là Habit CheckReceipt/replay decision, **không chỉ đổi409 thành200**; cùng slice sửa all transaction final proofs.
3. Goal no-read example: actor có goal.create, no goal.read/target.read, POST hợp lệ→201 `{"id":"…","committed":true,"detailAvailable":false}`; SQL có row/audit/receipt; UI toast safe, không GoalDetail giả, `.goal` access hoặc GET. Khi grant read sau đó, retry original vẫn opaque.
4. Planner source example: Task read bị revoke sau pin; current List không trả cached title/Project/dates, unavailable safe state; owned pin cleanup dùng unpin rights/E và metadata-only query, không đòi Task read mới; Task/Event/reminder giữ nguyên. Source participant phải vào cùng TX để final proof có nghĩa.
5. Windows example: C child report PID/effective SID lấy bên trong process, control file readable, capture open OS errorAccessDenied; A write và B actual operator read canary pass. Không có A/B positive hoặc C chỉ đổi SID string thì test chưa chứng minh boundary.
6. Chạy planned commands ở verification appendix sau verify disposable target ownership; không dùng ambient connection/URL. Capture exact counts/errors/artifacts đúng final commit, không giữ219/143/10 làm quota. SQL pass nhưng Windows actual-principal/review pending → verdict vẫn INCOMPLETE.
7. Tránh các lỗi thường gặp: kiểm tra authority chỉ trong `if(key!=null)`; replay derive Correct/transition từ current row; canonical v2 đổi namespace để bỏ legacy; digest chỉ parent ETag; safe ACK rồi tự GET; current query nằm trên trang đầu nên coi missing; unpin join nguồn private; child Windows bị chặn ở binary folder; StrictMode initial read count hardcode1; metadata CRUD được gọi full Vault/News/GitHub.

Tài liệu này bàn giao thiết kế và thứ tự triển khai. Chưa có remediation code, migration, grants, workflow change, test runtime, Windows account provisioning, commit/push hoặc PR mutation trong lượt lập kế hoạch.
