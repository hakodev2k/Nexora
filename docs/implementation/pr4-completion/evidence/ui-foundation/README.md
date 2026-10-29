# Curated shared UI evidence — final rendering checkpoint2026-10-26

Exactly nine PNGs copied byte-for-byte from actual isolated full-application run `ui-foundation-frozen`; all nine personally opened before curation. Synthetic UserA and ordinary owned Bookmark metadata only, account menu closed. No credentials/private browser storage, image editing, runtime CSS injection or fabricated business responses. This set replaces the earlier pre-scroll-lock captures; earlier run limits remain in the worker report.

Source: PR4 `impl/m01-s00-scaffold`, base `a0215f7b3e85d33503cff9077b41cc065240cb81` plus coordinated worktree; runtime marker `a0215f7b3e85d33503cff9077b41cc065240cb81-existing-feature-worktree`. Final source/test SHA256 fingerprints below bind this checkpoint. This is not immutable delivered-commit/CI or human acceptance; reconcile to delivery and invalidate evidence for later relevant edits.

Command: `.tmp/run-nexora-browser.ps1 -Suite uiux -Grep 'shared foundation: Bookmark create/edit visual and rendered control contrast'`, worker `ui-foundation-frozen`: **3cases PASS**,0failure/skip/errors,5.9s. Each actual viewport creates its own uniquely named synthetic Bookmark through ordinary UI Save, checks its persisted populated result and opens create/edit forms. Nine screenshots and18field observations are not nine/18independent feature acceptance cases.

Actual computed URL/title/description boundaries in both modes at all widths: `rgb(129,144,165)` onwhite, contrast3.2477316577:1. Checks include exact token match, single dialog heading and Escape close. This proves selected light-mode rendering/control boundaries, not full WCAG, screen-reader, all keyboard/permission paths or whole-product usability. Mobile screenshot pixel ratio differs from CSS width390x844. [Worker report](../../workers/design-system-20261025.md) records iterations and remaining gates.

Actual local evidence: `web/Nexora.Web/artifacts/uiux-audit/ui-foundation-frozen/playwright-junit.xml` and three `*-foundation-controls.json` files; no private runtime files copied. Product Owner UI/product acceptance and independent current-head engineering/security/release gates remain required.

| CSS width | State | Screenshot | SHA256 |
|---|---|---|---|
| 1440 | populated | [UserA-1440-bookmarks-foundation-populated.png](UserA-1440-bookmarks-foundation-populated.png) | `630C6AE422A5BC0F309869E2DA12E70C961030A6BA73C6BCC3B6898AFDEECBB9` |
| 1440 | create | [UserA-1440-bookmarks-foundation-create.png](UserA-1440-bookmarks-foundation-create.png) | `645850287F9125468E02AD44C531B6D0901B04418BB900F9F0F7F3E7C1309BFB` |
| 1440 | edit | [UserA-1440-bookmarks-foundation-edit.png](UserA-1440-bookmarks-foundation-edit.png) | `BB2BA7BAFBC3FC88EFFBDBEE4DEAADF280FE74148B9C891551D3763AA692B278` |
| 768 | populated | [UserA-768-bookmarks-foundation-populated.png](UserA-768-bookmarks-foundation-populated.png) | `3F28B91308E00A5284BDE9A6826AFF52B8EFAC29C060DFBDC42D2F06A2DD7BCB` |
| 768 | create | [UserA-768-bookmarks-foundation-create.png](UserA-768-bookmarks-foundation-create.png) | `BB5212DA13530EBE6109F567FE79C7C420496CBA34048A8775F712EF0F6A78FE` |
| 768 | edit | [UserA-768-bookmarks-foundation-edit.png](UserA-768-bookmarks-foundation-edit.png) | `6BFBF4D979A9E98894EE6EEDBD7EEC4821CC387657F5F2CA446ADDF428FD026D` |
| 390 | populated | [UserA-390-bookmarks-foundation-populated.png](UserA-390-bookmarks-foundation-populated.png) | `58972730C713DE8D6C713C4013FD15550327019A53CBFF209816B77051AD0D20` |
| 390 | create | [UserA-390-bookmarks-foundation-create.png](UserA-390-bookmarks-foundation-create.png) | `F55B15F4C2BCD281E0EAEA87FF7DE7C9A738B9722F1260EB2BD6E18240485CDC` |
| 390 | edit | [UserA-390-bookmarks-foundation-edit.png](UserA-390-bookmarks-foundation-edit.png) | `C37F18B1EBBE9D8C9208B27788C733727A20F7E905F7B4CC45BD1455B7EF5FB9` |

## Final source/test fingerprints

- `web/Nexora.Web/src/App.tsx`: `2F51F975BC550B29FD597AD6C47A0BA711F2F303136126CF8953709242B6466C`
- `web/Nexora.Web/src/styles.css`: `EB47CA838980876EC9FE70687D27B415543A6A019DD27A32F2E9A0AE9B14E384`
- `web/Nexora.Web/src/modalIsolation.ts`: `ECE837490D35F7A74587516E85B3743750323C0EEAFBADAAAB13D4A136562099`
- `web/Nexora.Web/src/ui/primitives.tsx`: `CCC63FBD9F920E8C061437006331D8441E90395D04C55E9F91E745E7BC8CE0BE`
- `web/Nexora.Web/src/ResourceFormDialog.tsx`: `006A6220944A2D0C0C3E000C0195E29943BCF29880D2EEE91DBCCE5CA5ECF58A`
- `web/Nexora.Web/e2e/pr4-uiux-audit.spec.ts`: `AF9EFCE3061EA330A2E69E37AF4D6C0625F9016BEAC2AF3459657FAAAB1F8FA6`
- `web/Nexora.Web/playwright.ux-audit.config.ts`: `0B59E1A3D79BFDBBAEF7BC3AE25E3F3BEE987266CFED05880BC8882E681F7AF8`
- `web/Nexora.Web/package-lock.json`: `88D2C95D5D2649CF486DABFD432081CE4B9B68A01E63B8BEEE1586CA938CE8B0`
- `web/Nexora.Web/artifacts/uiux-audit/ui-foundation-frozen/playwright-junit.xml`: `0DAE6A6B4B967A5823EDED9416E9D59C0BF23BB73F28C40B1FCA639DE5B3FE7F`
