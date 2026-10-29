# Independent Shopping/Certification UI delta review — 2026-10-25

Reviewer: shared-UI worker, independent of Shopping/Certification author. Read-only review of current source and component-test delta on base a0215f7 plus worktree. Reviewed sourceAppliedQuery paging guard, chooseOrder response epochs/success-only line reset, Certification history success/error/empty states and their regression tests. No Shopping/Certification tests executed by reviewer; author-reported30PASS is separate evidence.

Finding SHOP-UI-R01 Medium: Shopping persist handler omitted sourceBusy, while the button alone disabled submit during source lookup. Pending order-reference selection can leave the prior orderLineId until the read succeeds; the handler should reject any submitted form while that reference request is pending. Sent author for handler guard and regression test. No authority widening or direct SQL risk inferred; backend remains authoritative. Re-review the fix separately.

Other bounded delta: edited-query paging cannot append the next page of a different search; query snapshot matches response/applied-query state, epoch guards discard obsolete results. Successful order lookup resets an incompatible line, failed lookup preserves current draft. Certification empty-history notice is rendered only after successful empty response, not on failure; failed history remains recoverable. No other blocking source finding in this delta. This is not whole-module or runtime acceptance.

Reviewed source fingerprints before SHOP-UI-R01 author remediation:
- `web/Nexora.Web/src/ShoppingRecordsScreen.tsx`: `51DFDAA2684CFE2319AC2EE6E027DBE19F12668A838D1304494BB72C06C1128E`
- `web/Nexora.Web/src/ShoppingRecordsScreen.test.tsx`: `8B523C4E90AC3223404678BFE2059307CD635B3464D255633887460CB7080ED6`
- `web/Nexora.Web/src/CertificationScreen.tsx`: `A3F321AC0AC895DD6FC86C587E6D37E8AEF30281E9500C811098E4C03D4710DA`
- `web/Nexora.Web/src/CertificationScreen.test.tsx`: `90DF6ABD30E16B790D268C7BFB600D46E71FEA68D2DF142B3707A7E4489A5CB8`

## SHOP-UI-R01 remediation re-review
Author source now clears prior sourceOrder and line immediately on selecting a new order, keeps other draft fields, guards chooseOrder during mutation flight and guards persist with sourceBusy. Read the failed503 switch / preserve other fields / explicit rechoose-and-save regression source. Source finding FIXED and independently re-reviewed; reviewer did not execute tests. Current Shopping source SHA256: `51DFDAA2684CFE2319AC2EE6E027DBE19F12668A838D1304494BB72C06C1128E`. Author reports31PASS separately; do not promote that to reviewer runtime evidence.
