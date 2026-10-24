# Kế hoạch kiểm chứng, trace và evidence

Tất cả ca/commands dưới đây **PLANNED / NOT RUN** trong lượt plan-only. Tên tests mới là **PROPOSED**, không phải assertion đã tồn tại. Bảng chỉ định expected behavior theo source, không cho phép đổi production semantics để chiều expected của test cũ. Current CI ở task brief là quan sát run đã có trên R, không chứng minh code tương lai.

## 1. Môi trường và fixture contract

| Label | Setup được phép khi nhận lệnh thực thi | Giới hạn evidence |
| --- | --- | --- |
| C | Component/unit fixture synthetic, controlled clock/network response, StrictMode bật | Chứng minh formula/state machine/typed outcome consumer, không SQL locking/auth middleware |
| S | Disposable **real SQL Server** xác minh run/owner/nonce, dedicated database, API server đúng revision; synthetic UserA/OwnerA, UserB/OwnerB, AdminAllow/AdminAbsent/AdminDeny, SuperAdminSELF; live sessions | Không dùng EF InMemory/SQLite/mocked capability thay SQL. Setup fixture có thể cấp synthetic grants trên target owned, không production; không product automatic grant |
| Bf | Chromium real desktop và320px mobile, controlled HTTP responses, synthetic canaries, inspect screenshots | Render/reflow/keyboard/dirty/modal/recovery; không business acceptance SQL/API |
| Br | Browser real API/middleware/S identity/SQL persisted state, actual private disposable URL+credentials from owned manifest | Chứng minh end-to-end selected journey; không external real provider delivery |
| W | Disposable Windows actual3accounts/tokens, probe binaries+config isolated, A runtime/B operator/C foreign | Linux DACL snapshot/SID argument không thay actual Windows principal evidence |
| L | Existing Linux filesystem-required executable suite | Linux permissions/cross-process locks only |

Canaries chỉ synthetic khi được yêu cầu thực thi sau này; không thêm fixture/data/test code ở lượt này. SQL triggers/barriers phải scoped exact run/owner IDs, install/drop trong finally, database verified disposable. Prefer transaction callback/barrier test hook tại internal boundary nếu có safe existing seam; không thêm public debug auth-bypass endpoint. Không output token/connection/password/notes/bodies vào evidence.

Common authority expansion cho **mỗi operation** G/H/P: User valid; Admin exact Allow; Admin absent; explicit Deny overrides Allow; SuperAdmin own SELF; SuperAdmin foreign owner denied; forged OwnerId/context/action hint; revoked session; expired idle/absolute; disabled/deleted account; inactive PersonalSpace; module/user grant off/hard dependency off; unsupported registered action. Assert fresh operation denied **zero business/audit/receipt/effect writes**, reads no payload/existence/count leak. UI-hidden button không là API negative test.

## 2. Goals — riêng từng failure mode

| ID / proposed test | Setup → thao tác | Assertions / source | Env / evidence |
| --- | --- | --- | --- |
| G01 `Goal_Keyless_FinalAuthorityFailure_RollsBack` | Admin create allowed at preflight; scoped in-TX trigger sau Goal insert revoke exact write action; POST keyless | No Goal/Target/Audit/receipt, deny, no saved toast; authorization00 order8. Chạy thêm keyed variant | S API response + SQL before/after counts + source fingerprint |
| G02 `Goal_CreateWithTarget_MissingEachAction_IsAtomic` | Có create thiếu target.create, rồi thiếu create có target.create; send composite | All-or-none, no parent/child/audit/receipt; full action set verified inside TX; no grant auto-created | S compare owner-specific rows |
| G03 `Goal_Create_WriteOnly_ReturnsOpaque` | Admin create only, no goal/target read; POST valid key |201 ACK exact3fields, authorized Goal.Id only; no title/target/progress/ETag disclosure; receipt no private JSON | S + C+Bf no GET, no `.goal` crash; Br persisted row proof |
| G04 `Goal_OpaqueReplay_RemainsOpaqueAfterReadGrant` | G03 committed; grant read in isolated fixture; same body/key | Same semantic201 opaque; one Goal/audit; no DTO upgrade. Same key different body409/no write | S + envelope privacy scan |
| G05 `Goal_ReadableReplay_DerivedProjectionChange_Downgrades` | Original full readable result; change child target/progress in allowed separate TX, parent ETag unchanged fixture only if schema permits | Same-key replay never returns new current DTO as old outcome; ACK, no second mutation; original projection digest covers full child/count/progress | S digest+safe response, original row/audit counts |
| G06 `Goal_Progress_RouteTargetOwnerKindBounds` | Same owner target belongs other Goal; foreign target; Boolean kind; decimal scale9/overflow; target=initial |404 relationship/owner safe,422 invalid numeric; no progress/audit changes; valid out-of-range current clamp BR001 retained; target>initial BR/AC002 | S + C formula0/1/no-target, no formula change |
| G07 `Goal_TransitionReplay_PrecedesFreshETagAndGraph` | Commit start Draft→Active, drop response; same key original stale E; later current state changes legitimately | Receipt original start accepted only under current original action; no reopen/second transition; projection changed→ACK; no fresh412 just because original E old | S audit/action one logical effect |
| G08 `Goal_FinalReceiptTrigger_Revoke_RollsBackAll` | Trigger receipt complete update changes write permission/owner/session inside same TX | Final proof after receipt sees invalidation; business+audit+receipt+fixture revoke rollback; assert grant restored after rollback as trigger also TX | S deterministic in-TX test, keyless test covered G01 |
| G09 `Goal_ListGet_CurrentProof_NoUncommittedTargetLeak` | Two connections stage uncommitted target/title; revoke current read before query linearization; list/get | No NOLOCK dirty derived rows/count; denied current projection clears staged data; wrong owner404 | S timing/barrier trace, not inferred from query text alone |
| G10 `Goal_ParallelSameKey_OneEffect_AndStaleFreshWrite` | Two S connections same body/key; first commit/rollback variants; different keys same currentE | Duplicate one Goal/Progress/Audit, replay safe; other fresh stale412 not overwrite. Invalid/unquoted/weak/wildcard/multi E rejected; original quoted E reused unchanged | S final state/counts + C precision/ETag parser |

Required actor expansion applies create/update/progress/each transition/read separately. Update's explicit `goals.goal.read` prerequisite retained; write-only response test uses create/start/progress **only where actual action contract permits**, không gỡ prerequisite để test pass. Existing Boolean/Tasks target AC001/003 ngoài numeric implementation phải giữ pending trong RTM, không dùng G06 negative kind làm proof complete Task-linked flow.

Legacy Goals variants: `Goal_LegacyProgressOrTransition_TrustedV0_ReturnsOpaque` uses exact existing producer canonical/HMAC + ResultCode/null ResultJson; correct route/key/body yields ACK without fresh-write ETag/state validation, one effect. Change target/Goal/status/body→conflict or safe current deny, never new claim. `Goal_LegacyCreateOrAmbiguousUpdate_ReconciliationNoExecution` verifies generated ID/ambiguous free-field binding gives documented409, no guessed resource/current DTO/second write.

## 3. Habits — Record khác Correct và schedule history

| ID / proposed test | Setup → thao tác | Assertions / source | Env / evidence |
| --- | --- | --- | --- |
| H01 `Habit_RecordOnly_CompletedReplay_DoesNotRequireCorrect` | Current Admin chỉ checkin.record, fixture Habit Active/scheduled day+E; first commit drop response; retry key/body/E | Original action persisted Record; same200 semantic outcome/ACK; one CheckIn+Audit, unchanged UpdatedAt/count, no Correct required; FX17 AC003 | S API+SQL+envelope; C/Br immutable retry body |
| H02 `Habit_NewIntentExistingSlot_RequiresCorrect` | Existing checkin from H01; new key with currentE; Record-only then Correct-only | Record-only denied no change; Correct-only allowed replace, không cộng count; state-derived exact action within TX | S audit action/slot count, owner-specific assertions |
| H03 `Habit_CoreAndReminderDiff_RequiresBoth` | Update title+reminder với thiếu từng action; reminder-only/core-only/clear variants | Combined missing either rolls back all; reminder-only theo exact composition, generic update không grant reminder; no schedule/Kind/target smuggle | S full fields+receipt/audit; C form disabled/error behavior |
| H04 `Habit_CreateReminder_MissingSetPermission_NoPartialHabit` | habit.create allow but set_reminder deny; POST with reminder vs null | Reminder present deny all rows; null normal create permitted if other checks pass, readable/ACK separate | S + Bf no false saved |
| H05 `Habit_KeylessAndKeyed_FinalProof_UseSameTransaction` | Scoped trigger after Habit/CheckIn/receipt completion revokes each authority class | Business/Schedule/CheckIn/Audit/receipt rollback atomically, no auxiliary connection stale allow/hang. Enumerate create/update/schedule/checkin/transition | S per-method regression, bounded command deadline |
| H06 `Habit_ScheduleEffectiveInterval_PreservesHistoricalAchievement` | Count target2 historical day achieved; set future target5/mask; conflicting schedule start | Past achievement/streak based old schedule; future target changed only date≥effective; overlapping409; same-key retry no schedule row added; FX17 AC002 | S schedules+CheckIn.ScheduleId + C deterministic clock |
| H07 `Habit_LocalDay_ClockDST_AndInvalidZone` | IANA LA/Kiritimati, before/after local midnight; DST23h/25h, invalid zone; future/past date | No future check-in; stored historical LocalDate never shifted by timezone update; unsupported zone fails422 per policy; schedule time ambiguity explicit, not invented instant | C+S+Br date input; timestamps controlled |
| H08 `Habit_ReadableReplay_ChangedStreakOrSchedule_IsOpaque` | Original readable detail; independent schedule/checkin/date-window changes not necessarily parentE; then same-key retry | Full projection mismatch→ACK; old opaque remains opaque after read grant; no current note/schedule leak when read revoked | S receipt no private JSON; C/Bf no auto GET/empty-success |
| H09 `Habit_ParallelCheckIn_UniqueSlot_NoDoubleCount` | Two real connections same Habit/day; same key and different intent/E variants, concurrent schedule change | One unique slot, counts replacement semantics, stale/duplicate safe result, no partial audit/receipt; no lock inversion; NoOverlap remains enforced | S lock waits/deadlock+final owner counts |
| H10 `Habit_LifecycleAndRead_FinalCurrentBoundary` | Pause/resume/archive/unarchive preserving previous schedule; List/Get current read revoked while staged | Historical schedules stable, paused/unscheduled not break streak; archived cannot checkin; no postcommit unguarded detail read; final read denial hides schedule/note/progress | S+C; actual reminder dispatch not covered by metadata tests |

Future `Habit_Reminder_AchievedBeforeDispatch_CancelsDayIntent` needs explicit Reminder provider/dispatch contract and all3attempts; keep pending in RTM. Passing H01–H10 does not certify full FX17 BR005.

Legacy Habit variants: `Habit_LegacyRecordedCodeOnly_UsesRecordNotCorrect` seeds supported v0 Succeeded receipt through real store/producer format (not V2 envelope), original Habit/date/count/E bound by HMAC and ResultCode=HabitCheckInRecorded; Record-only retry returns opaque Habit ACK, no new checkin/audit and no Correct requirement. Unknown code/mismatched digest/ambiguous metadata canonical fails reconciliation/conflict; legacy original read availability unknown remains opaque after granting read. Future-day/midnight fresh rules không chạy lại cho completed supported replay.

## 4. Planner — nguồn, cleanup và atomic reorder

| ID / proposed test | Setup → thao tác | Assertions / source | Env / evidence |
| --- | --- | --- | --- |
| P01 `Planner_Pin_ForeignOrTerminalSource_IsDenied` | Own/foreign Task IDs, Task in terminal/Trash Project, current source module off | No pin/audit/receipt on denied; generic unavailable no title/existence; pin valid creates one and no Task/Event/reminder mutations; FX15 AC001/003 | S API+counts before/after |
| P02 `Planner_DateAndNotesDiff_RequiresAllActions` | Change date+notes missing notes/reschedule in turn, each field alone | All-or-none; both checks on same TX; no client action-hint escalation; source field never edited | S both day sets+fields/audits |
| P03 `Planner_SourceReadRevoked_HidesAllSourceFields` | Existing readable pin; revoke tasks.task.read; list/unavailable/source transitions | No cached TaskTitle/status/ProjectName/start/end/snippet/count leak; own pin metadata only approved; terminal-readable branch distinct; safe current E | S+Bf+Br DOM/response secret canary |
| P04 `Planner_UnpinUnavailableSource_DoesNotRequireSourceRead` | Own pin, source Trash/terminal/read denied, current own pinE; unpin key |204 success, one audit/receipt, Task/Event/reminder remain; retry same key/E204 even pin removed; foreign pin404 | S source-specific counts, action15 cleanup guard |
| P05 `Planner_Reorder_InvalidSelection_WritesNothing` | duplicate IDs, foreign/missing pin, mixed dates, partial set, stale planE, zero/>100 IDs |422 duplicates/range, safe404/409 selection,412 stale; ranks/updatedAt/audit/receipt unchanged for all pins | S snapshot full owner/date state before/after |
| P06 `Planner_Reorder_WriteOnly_ReturnsDocumentedAck` | reorder/source allow, planner read deny, exact current selected set/E |200 minimal ACK id=original PinIds[0] theo contract4.4, no whole plan/task payload; retry retains anchor after separate source/order changes; client no auto list; original opacity persists | S+C/Bf; parser/binding/consumer reviewed cùng slice |
| P07 `Planner_ReorderReplay_DoesNotReapplyAfterNewOrder` | First key reorder commit; another legitimate order/date/source title change; retry original | No second reorder, one original audit; original digest mismatch ACK, not latest plan as old result; old E accepted replay | S compare ranks/audit/receipt |
| P08 `Planner_KeylessAndReceiptFinalRevoke_AtomicRollback` | Trigger at pin/rank/receipt completion revoke action/source/owner/session | All affected ranks/pins/audit/receipt rollback, no partial source data return; both keyless and keyed paths | S per-method cases incl move both dates |
| P09 `Planner_SameDayConcurrentPinMoveReorder` | Two connections pin same Task/day; move opposite days; reorder while source Task close/Trash | Unique owner/task/day; day-range lock correctness; no partial order; bounded deadlock policy no new keys, no Task reopening | S compatibility lock graph+wait diagnostics; source participant same TX |
| P10 `Planner_IanaTodayDateOnlyWeekBoundary` | Omitted range in owner zones around UTC/local midnight; explicit stored date, timezone change, Monday week | Owner today correct, explicit dates preserved, ≤32 inclusive days, Monday week; plan dates don't change Task Start/End/Reminder FX15 AC002 | C+S+Br320px Week keyboard day selection |

Legacy Planner variants: `Planner_LegacyReorder_BindsOriginalAnchor` uses exact v0 date/E/ordered UUID digest + PlannerReordered, returns200 opaque original PinIds[0] without reapplying ranks. `Planner_LegacyUnpin_AlreadyRemoved_Returns204` proves trusted old pin binding no source read. Legacy Pin generated UUID absent and Update missing composed-action proof yield safe409/no execution; không lấy current pin tìm theo Task/date để giả original result.

## 5. Race linearization protocol — mỗi G/H/P áp riêng

`RevokeCommitsFirst`: connectionR commits actual permission/session/module revoke, signal barrier, connectionC starts/continues authority stage→deny/no writes. `CommandCommitsFirst`: C holds Serializable authority locks, R revoke blocks, C final proof/commit valid, then R commits→C success hợp lệ; following read/retry denied by current authority. Không assert retroactive rollback của C.

`WithinCommandFinalProof`: scoped trigger/callback trong chính transaction thay authority sau business hoặc receipt writes→final proof rollback toàn bộ. Kiểm tra rollback cả trigger state vì cùng TX; test không fake external revoke-before-commit khi lock model không cho. `ReadOnlyRevocation`: stage rows+count, current read proof invalid→no staged output; write survives read-only loss **chỉ nếu read không là prerequisite mutation** và contract chooses committed ACK; otherwise full rollback.

Mỗi race ghi two connection/session IDs (không credentials), barrier stage, commit order, row/audit/receipt before-after. Sleep-based timing không đủ deterministic proof. Nếu lock-order/source participant chưa chốt, concurrency case BLOCKED đúng gate, không bỏ test để green.

## 6. Windows matrix — actual process token evidence

| ID | Setup/action | Expected proof / failure rule |
| --- | --- | --- |
| W01 | Preflight disposable Windows+launch+account/DACL APIs | Actual Windows, sufficient privileges; absent => nonzero BLOCKED, never skip-pass |
| W02 | Launch A/B/C, each self-report identity+PID+creation time and read stage-control canary | Three distinct SID/PID, C nonadmin/no bypass privileges, C executes binary/traverses parent; fake report/wrong nonce fails |
| W03 | A actual sink init/Publish/prepared/commit/reconcile/final | Runtime can write/read pending/final, protected exact FullControl runtime+operator SID policy; no broad foreign ACE |
| W04 | B operator actual `ReadCaptured`/permitted workflow | Original canary digest matches, no copied fake file; B FullControl existing policy preserved |
| W05 | C enumerate/read/create/overwrite/delete/ACL-mutate capture | Native OS denies, file/state unchanged. FileNotFound/probe failed/timeout/blocked staging is **failure**, not negative pass |
| W06 | Inspect actual temporary/pending/final access under child identities | No transient inherited exposure; C denied at each applicable stage, A/B required workflow allowed |
| W07 | Restart A same runtime SID; repeat positiveB/negativeC | Policy durable under real reopened process, not parent DACL snapshot alone |
| W08 | Owned fixture wrong inherited/foreign ACE; sink initialize/read | Reject insecure state; no auto repair unrelated directory; own safe legacy temp behavior follows existing contract |
| W09 | Force child timeout/cancel/crash/setup partial failure | Owned PID+creation/nonce cleanup; accounts exact SID/creation match; manifest mismatch refuses destructive cleanup; required cleanup failure visible |
| W10 | Executable suite + PowerShell requiredWindows CI | Actual assertion executed, no permanent required Skip; missing report or skipped test nonzero; sanitized JSON/JUnit artifacts no credentials/capture |

W03/W04 positive phải pass cùng run để W05 negative có nghĩa. Report nonce+actual effective SID và OS result được reviewer xác minh; account names/PID prefix không đủ ownership. Do not create/provision accounts in this plan-only turn.

## 7. P1 acceptance matrix

| WP / proposed scenario | Setup → operation → assertions | Environment / closure |
| --- | --- | --- |
| DOC1 `Documents_DirectLocation_ImmutableCreate` | Two folder levels + root/child pages, same titles across owners; query root/folder/sidebar with filters; direct children correct/folders navigation retained; create explicit mode/type, forged move/type/reparent rejected | S+Br desktop320; snapshot filtered order/count and keyboard/back |
| DOC2 `Document_SaveRetry_OneVersionAndSafePaste` | Block/Markdown fixtures at1MiB bytes boundaries, malicious HTML/embed/image URL; Save same key then distinct key identical body | Same retry one version, new key new version; content rendered escaped, manual Save/dirty conflict no auto save; C+S+Bf/Br screenshot |
| DOC3 `Document_ArchiveTrashCohorts_NoResurrection` | Parent Draft/Published children plus independentlyArchived/Trash child; archive/unarchive/trash/restore | Correct per-child prior state/cohort, no revive separate child/link; missing parent/Archived restore denied; real SQL+Sharing/Files |
| DOC4/5 `Document_CompareRestore_ExactSnapshotsAndRebinding` | OldTag renamed/removed, cover quarantined, exact old version; compare/preview restore vs staleE | Snapshot labels unchanged; explicit Tag/file rebind or blocked; new version SourceVersion, old snapshots unchanged; denied field action rollback. SQL+Files+Br |
| ORG1/2 `Tag_AssignNamespaceCardinality` | Project/Task shared namespace, Documents≤1 dedicated Tag, foreign tags, missing target write | Exact source actions, no duplicate generic catalog, whole command atomic; S/API+picking UI |
| ORG3 `Collection_DoesNotGrantOrDeleteSource` | Members readable/denied with same title, reorder duplicate/stale and delete container | No denied member title/count, no source authority inherited or resource delete; exact membership order atomic; S+Br |
| ORG4 `Template_InstantiateThroughTargetCreate` | Seed contains fake IDs/owner/lifecycle/share/secrets/history/reminder; required Task times/Document mode missing | Reject/strip only by declared sanitization report; target missing fields422, missing action deny; one resource on retry; template edit not live-bound; S+C+Br |
| DISC1/2 `SavedSearch_CurrentProviderAndUnknownVersion` | Save typed query, mutate/revoke source, run; unknown query version/filter provider | Current results only, unknown version requires migration not widened query; no snippets/facets/count denied; S+Br |
| DISC3/4 `RecentClearAndPalette_OnlyIntendedEffect` |101 authorized recent opens; clear; select destructive palette action, revoke before submit | Metadata cap100, resources/Audit retained; palette opens confirmation, direct target rechecks; keyboard/focus/IME+320px; S+Bf/Br |
| DSH `Dashboard_ConfigRevision_IndependentProviders` | Owner layout reorder/config, two stale saves; source disable/re-enable; one provider fault | All-or-none config, layout retained/no protected payload, other widgets usable, count/drilldown match, quick-create required fields enforced; S+Br |
| NTF `Notification_ResolverAndProtectedCursor` | Safe source readable then Trash/module/read revoked; category/date cursors wrong owner/filter | No stale title/body, source open current auth, cursor safe, counts private, clear ACK no auto GET; S+Bf/Br |
| NTF delivery `Notification_ThreeAttempts_WatermarkRecovery` | Logical event duplicate, push unavailable/email transient, crash lease; mark-all watermark then new notification | One inbox item/3independent attempts, truthful states, retry bounded, new after watermark unread, delete no unsend/security audit deletion; S+local transports |
| NEWS `Feed_DedupeState_Watermark_Unfollow` | Two owners same feed GUID, update, mark-all, later article, save then unfollow | One article/feed key, state isolation, later unread, saved refs/other owner survive; source freshness truthful; S+local adapter+Br |
| NEWS guard `Feed_XXE_PrivateRedirect_AndBoundedPayload` | Isolated adapter DNS/redirect to private/malformed XML/decompression bomb/timeout429 | No prohibited egress/XXE, bounded resource use, prior state retained/degraded, not fake successful fetch; local controlled integration, no real provider |
| GH `WeeklyCreatedUTC_TotalStars_ImmutableSnapshot` | Old repo huge stars, new-week ties, partial result, unknown subscribers; capture then change metrics/compare | Old excluded; exact tie/window semantics; unknown not stars; old metrics immutable; incompatible snapshots rejected, partial labeled; S+C+controlled adapter+Br |
| RES `Application_ExactResumeVersion_NoPrivateShare` | CleanFile/DocV3, application pins V3, append V4, quarantine/missing/foreign ref, share | Old pin staysV3, one version on retry, unsafe source denied, no Job/contact/salary/feedback in share; real Files+SQL+Br; projection review |
| EVT `Career_CreateLinkRetry_UnlinkRetainsSource` | Create-with-reminder lost response retry; missing each action; existing terminal/wrong-kind Event link | One Event/link/reminder, atomic rollback, no terminal reopen; unlink leaves Event/reminder; same-owner enforcement; S+Br |
| LRN1/2 `SkillEvidence_MergeKeepsHistoryNoAutoProficiency` | Owned evidence refs, current read revoke, source/target Skill merge with stale previews | No cross-source permission gain, history/evidence preserved, proficiency only explicit; S+Br |
| LRN3 `LearningPlan_SourceUnavailable_NotComplete` | Typed approved refs+empty/unavailable sources; reorder/unlink | Transparent denominator, no fake complete, source not rescheduled/deleted, stale reorder atomic; S+Br |
| LRN4 `CertificationRenew_StaleReminderDenied` | Existing expiry/evidence/pending revision; renew same key concurrent worker | Old version evidence/expiry retained, old job fenced, one new pending reminder logical intent+three attempts; S+clock+local transport |
| LRN5 `WorkLog_UniqueTimeEntry_GrossOverlapLocalDate` | Two requests same TimeEntry, manual minutes+source mix, overnight/DST/overlap | Filtered UQ owner/entry, not double-count; XOR enforced, gross/overlap truth and local date correct; S+C+Br |

Vault crypto canaries/tamper/recovery/package tests design after threat-reviewed crypto contract; no metadata CRUD suite accepted as Vault completion. FX31 suspected flows need exact requirement/current workflow confirmation before concrete defect assertions; RTM records pending source reads explicitly.

## 8. Commands thật có tại baseline, chỉ planned

Chạy từ repo root trừ khi ghi khác. Trước thực thi: rebaseline final branch/tree/dirty hashes, inspect owned manifest and toolchain, pinned lock files, no ambient target. Không tự chạy commands chỉ để tạo evidence plan.

| Command / existing path | Purpose / applicability / target gate |
| --- | --- |
| `dotnet test tests/Nexora.UnitTests/Nexora.UnitTests.csproj -c Release` | xUnit behavior/contract tests; no SQL proof. CI adds TRX/results/coverage flags; use explicit output path |
| `dotnet run --project tests/Nexora.UnitTests/Nexora.UnitTests.csproj -c Release` | Preserved custom executable checks; distinct from xUnit counts |
| `dotnet run --project tests/Nexora.UnitTests/Nexora.UnitTests.csproj -c Release -- --require-os linux` | Actual Linux filesystem-required cases; Windows-only skips cannot imply Windows pass |
| `./scripts/dev/test-filesystem.ps1 -RequireOs windows` | Existing PowerShell forwards required OS and exit code; after WP-WIN actual3tokens setup; disposable Windows only |
| `bash scripts/dev/test-sql.sh` | Existing real SQL/API suite; needs verified `NEXORA_TEST_SQL_CONNECTION` from fresh owned disposable database; never print value |
| `npm run build --prefix web/Nexora.Web` | Existing tsc/Vite build, implementation-error detection only; record existing chunk warning if present, no functional claim |
| `npm run test:unit --prefix web/Nexora.Web -- --maxWorkers=1` | Component state/recovery/ACK/UI contract tests; CI coverage variant `test:unit:coverage` actual existing script |
| `npm run test:e2e --prefix web/Nexora.Web` | Playwright existing suite; requires isolated private API/SQL target+owned credentials; separate fixture vs real-backend projects/cases in report |
| `bash scripts/dev/start-ci-sql.sh` / `stop-ci-sql.sh` | **R only, absent local HEAD**. Script explicitly requires actual GitHub Actions run/attempt/job, trusted GITHUB_ENV, Docker, loopback14339, refuses container reuse. Do not fake GITHUB_ACTIONS locally to bypass; local equivalent needs separately verified disposable target contract |
| `python3 scripts/dev/start-e2e-target.py` / `run-browser` / `sanitize-evidence` and `stop-e2e-target.py` | **R only** existing CI browser harness. Validates owned SQL/target, private ephemeral config, masks secrets, safe sanitizer; cleanup verifies ownership. Local ambient app URL not valid substitute |
| `python3 .ai/scripts/verify-baseline.py` | Required only if future agent-kit changes; none planned here. Structural agent config evidence, no application/independent review claim |

No new custom `npm run lint` command assumed; package.json inspected, it does not declare lint. `python` was unavailable in this sandbox during read-only gzip inventory; PowerShell .NET GZipStream+ConvertFrom-Csv successfully parsed artifacts instead. That is source analysis, no unit/product test. Future actual CI Python scripts run on configured Linux runner; local plan does not certify availability.

## 9. Evidence, rollback và gate closure

For each execution record: requirement/AC/action/test ID; actual source commit+tree/fingerprint+lockfile hashes; environment owner/run/nonce sanitized; exact command+start/end+exit; discovered/passed/failed/skipped and precise skip reason; raw safe TRX/JUnit/assertion+screenshots inspected; GitHub run/job/artifact URL; reviewer identity/fingerprint/disposition. Never upload private config/accounts/passwords/token/captured message/envelope key/raw server logs. Sanitized evidence retains failure meaning and counts, not suppresses failed cases.

Rollback tests: preserve legacy+V2 receipts, crash before commit/no persisted success, after commit/lost response reconcile original; restart stable HMAC config; binary rollback retains compatibility reader; no migration checksum edit. P1 additive migrations rehearse upgrade on verified-owned target with existing data shapes and rollback-compatible application, keep old versions/pins/revocation barriers; stop new writes before any schema recovery decision. No destructive restore into retained target.

UI evidence per screen: initial/loading/filtered-empty/error/denied/read-only/unknown/ACK/conflict; desktop+320px mobile+keyboard Tab/Enter/Escape/Back; dirty/unload guard; nested modal focus trap/restore; skip link and h1 focus; visible validation/status; no page-wide horizontal overflow. Inspect screenshots, not just create PNG. Controlled HTTP cases cannot be relabeled real API acceptance. Keep StrictMode, compare settled baseline read-count rather than expected1.

Final closure sequence: freeze final source → focused tests → affected existing regression → independent sensitive review → fix/retest/re-review any affected delta → semantic RTM update → final evidence snapshot. Required Windows still failed/skipped or independent review pending keeps remediation verdict **INCOMPLETE**, even if SQL/backend tests pass. Không giảm test/filter/skips/ACL/required flag để đạt green.
