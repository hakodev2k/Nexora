# Independent FX32 catalog/native navigation review

2026-10-24. **R24-TN-01 REMEDIATED_IN_SOURCE / CLOSED after focused re-review;
see superseding addendum below.** Original finding retained as historical evidence.
No confirmed security/navigation-authority defect found in the other assigned
paths. Static review only; no runtime or release acceptance.

## Task brief and authority

- Reviewer: this session, independent from root's implementation. User authorizes
  bounded review of current App diff and
  `docs/implementation/toolbox-catalog-navigation-contract-20261024.md`.
  Only this new report may be written. Earlier closed reports are untouched.
- Branch `impl/m01-s00-scaffold`; HEAD/base
  `8eb5aaff54c493d27ae1a0baa7209bdadecda948`, plus dirty App/new contract.
  Accountable implementation/remediation owner: root Technical Lead.
- Current DEC014 and D01–D09 apply; local code-only approval, no provider/production
  activation. No tests, runtime, fixtures, product edits, agents, commit or push.
- Kepler's URL/header helpers, C# seam and0092 are outside scope until explicit
  final-wiring notification. Their presence in finite route-code names is not
  reviewed as completed execution support. No Plan or other-module review here.
- Startup read: AGENTS; nexora-engineering skill; implementation profile; TL
  README/core rules. Previously loaded routing/verification/frontend/security
  guidance continues; browser-security/forms/accessibility guidance refreshed.
  Current docs/delivery scope, D01–D09, FX32 goals/UX and the new contract read.
- Plan: trace route parser/serializer → finite safe login return → native remount;
  trace catalog state/filter/open/return → draft guard/deferred callback; inspect
  stale async/privacy and report concrete source defects. No API/schema changes
  or migration/rollback execution are part of this assignment.

| Goals | Source / action | Reviewed source | Evidence status |
|---|---|---|---|
| NXG-SYS-03/08; NXG-FX32-G03 | P06-TBX-004/008; FX-32-BR-001; `toolbox.catalog.read`, existing selected run action | routeFromPath/pathForLocation/safeReturnPath; remount and existing authority boundary | Static inspection only |
| NXG-SYS-13/14; NXG-FX32-G01/G03 | FX32-S01/S02; P06-TBX-001/003/007; shared UX-05/15 | catalog query/category/no-result, selectTool, deferred navigation/leave dialog | One source finding below; runtime NOT RUN |

Rollback impact: revert UI/navigation diff, preserve all user/server data; do not
persist workbench content or alter action grants to solve a navigation problem.

## R24-TN-01 — Catalog filters are lost on native tool open and return

Priority P2 / severity Medium / confidence High / OPEN. This is a concrete view
continuity defect introduced with the new catalog state, not a generic test gap.

Exact evidence in `web/Nexora.Web/src/App.tsx`:

- `DeveloperToolsScreen`5421 owns `catalogQuery` and `catalogCategory` in local
  state, both initialized to empty strings. There is no parent view-state input
  or callback for restoring these values.
- Shell1502 keys this entire component by owner/profile/modules **and toolCode**.
  Opening a tool through `selectTool`5596 → deferred callback5454–5457 →
  `navigate('tools', code)` therefore destroys the catalog state and creates a
  new component with empty filters.
- The explicit “Về danh mục” action5618 calls `onSelectTool()`; the resulting
  undefined toolCode changes the key again. Browser Back follows the same route
  state/remount path. Neither route carries/restores the prior catalog filter.

Source-derived reproduction, **NOT executed**: open `/developer/tools`, search
`XML`, choose category `Data`, open the returned XML tool, then use Back or
“Về danh mục”. Expected: the prior catalog search/category context is restored.
Source-implied actual result: search and category are empty, and the full catalog
is shown. Filters already reset upon opening the tool, before the return action.

Requirement: FX32 UX section8 says in-module return preserves query/view/date/
scroll; FX32-S01 and S02 Exit/return explicitly require invoking view/filter
restoration. The new bounded contract sources FX32-S01/S02 and shared UX-05/15;
it does not waive the existing return-context rule. Workbench privacy requires
clearing input/output/options; catalog filter metadata can be retained separately.

Requested root fix: retain only catalog query/category/view metadata in an
owner-scoped parent/navigation state, restore it on tool open/return, and reset
on owner/authority change. Keep the tool-specific remount that clears private
input/output/options. Do not retain sensitive workbench drafts in URL/storage or
weaken the guard to preserve filters. Direct tool deep links may start with the
declared default catalog view when no prior view exists.

Owner acceptance still NOT RUN: search+category survive open/Back/explicit return;
clear-filters updates retained state; no-results survives a round trip; another
owner cannot inherit a prior catalog view; workbench drafts remain cleared on
confirmed switch/context change. Request focused source re-review after remediation.

## Other inspected paths — no additional confirmed finding

- LocationState243 adds a public toolCode only. Parser346–348 accepts bounded
  lowercase code syntax; path serializer428 encodes it into a native path.
  Unknown syntactically valid codes stay selected and produce an unavailable
  workbench: initialToolCode prevents catalog fallback5483, and selectedTool
  absence suppresses workbench5623 onward. No automatic fallback tool executes.
- `safeReturnPath`251–265 checks length, root-relative same origin, no credentials,
  query/hash/backslash, then finite known codes249/260. Direct unknown tool codes
  can show unavailable while authenticated but are intentionally not trusted login
  return targets. No tool input/options/output is added to route state.
- Anonymous redirect7176–7179 and expired-session clearSession7201–7209 preserve
  only the safe tool path. `authenticate`7185–7195 restores toolCode through
  navigate7100–7101. This is a native destination, not an external redirect.
- Shell1502 remounts by owner/profile/module/tool context. Existing catalog API
  and selected run API remain authoritative; a route or visible entry is no grant.
  Metadata is rendered as React text, no raw HTML sink is added. Catalog name/
  description/category are searched locally, category exact-filtered, name-sorted;
  clear-filters and explicit no-match status are present. No pagination/history/
  favorite persistence is invented for this small current catalog.
- Confirmed `selectTool`5591–5596 aborts/invalidates old work, clears both inputs,
  pattern/output/options and resets ordinary controls before queuing the route.
  The callback runs in an effect5454–5457 after busy/draft settle. Its guard
  effect is declared earlier, so its new clean closure is installed before that
  effect invokes normal navigation. The closed ResourceFormDialog deletes its
  child/guard on unmount; it does not leave a busy/dirty modal blocking the route.
  This avoids synchronously invoking Shell against the old dirty guard.
- Native catalog Back5618 uses normal guarded navigation; browser Back uses the
  existing Shell registry. Stay preserves drafts; explicit Discard5652 clears
  and invokes the stored continuation. Flight ref blocks exits during Run;
  beforeunload remains present. No bypass/force-navigation behavior was added.
- Existing epoch checks, known-denial cleanup and owner remount remain. Recoverable
  parser failures preserve drafts. New catalog filter state contains public
  metadata selection only and is not a server permission proof.
- Module-root entry still supports the existing local workbench when no native
  callback is passed; this review does not claim URL/header tools are wired.

These observations do not close all FX32 AC or broader existing catalog/history/
favorite requirements. Missing runtime evidence is recorded separately from the
single concrete view-state finding.

## Evidence / snapshots / remaining sign-offs

Executed read-only: Get-Content, rg, Get-FileHash, Get-Date, Git revision/branch/
status/scoped diff against8eb5aaff. Initial Git calls use the already-established
command-local safe.directory due to sandbox identity; no Git config changes.
Wrote only this report with apply_patch. No tests/builds/typechecks/lint/runtime/
SQL/browser/providers/fixtures/agents/commits/pushes. No author build treated as
independent runtime verification. All acceptance scenarios above are NOT RUN.

App hash read repeatedly and unchanged during this pass:

| File | SHA256 |
|---|---|
| web/Nexora.Web/src/App.tsx | `0D8EAA4AD56CB248EB5F14157752F1286FFC51ABAA88B3750229D8C7543A7FD8` |
| docs/implementation/toolbox-catalog-navigation-contract-20261024.md | `E2297DCA3F89254FCAAC3547E4DEF29ADC7C675DE94085EBE3F22B9FF7DBA7AB` |
| web/Nexora.Web/src/ResourceFormDialog.tsx | `B0F6DD3530CEBFE121C7EF480B079692B394007EBDB6FE86E671E738CE5C7883` |
| web/Nexora.Web/src/navigationGuard.ts | `28B2D3A375900230EA322CE47F1956407E92133935ED742EE1E9806A9ED2D651` |

Remaining: root remediation of R24-TN-01 and focused re-review; human browser/
keyboard/mobile/login/history/privacy acceptance; final-source build/current-head
CI; separate notification/review of URL/header final wiring. No merge, activation
or whole-module/R1 acceptance is supplied here.

## Superseding focused re-review — 2026-10-24 23:08 +07:00

User resumed after quota interruption and requested actual source re-review of
the implemented catalog-view fix. Branch/HEAD remain
`impl/m01-s00-scaffold` / `8eb5aaff54c493d27ae1a0baa7209bdadecda948` with shared
dirty changes. Authorization, source requirements and goal bindings above apply;
only this report may be edited. Previously reviewed startup/engineering rules
remain applicable and startup/core instructions were refreshed.

**Superseding disposition: R24-TN-01 REMEDIATED_IN_SOURCE / CLOSED. No remaining
confirmed blocker in this focused catalog query/category restoration correction.**
This supersedes the original OPEN/CHANGES REQUESTED status, not the separate
owner browser/scroll/keyboard/login/SQL/CI acceptance gates.

Actual latest-source evidence in `web/Nexora.Web/src/App.tsx`:

- Shell1346–1348 stores `ToolCatalogView` in a ref scoped by profile ID,
  PersonalSpace, role and module projection. Scope mismatch replaces the ref
  with empty query/category before rendering the child. No prior owner's view
  is supplied under a changed context.
- Type5421 contains only `{query, category}`. Child initialization5426 restores
  those values from `catalogView`; `updateCatalog`5427 updates both local state
  and parent callback. Shell1506 passes its current retained view and writes
  callback values back under the same scope.
- Search/category/clear callbacks5625–5627 all use `updateCatalog`. Therefore a
  query/category edit or explicit Clear reaches the ref synchronously before
  a later tool navigation/remount; no dependency on a delayed parent effect.
- Shell1506 retains the toolCode-dependent child key. Opening another tool,
  browser Back, or explicit catalog return remounts private workbench state
  while receiving the current catalog query/category. Thus the original XML/Data
  open/return source failure is closed without retaining private editor state.
- `clearToolAuthority`5473–5479 clears private fields, tools, pending modal and
  queued route, and calls `updateCatalog` with empty view. Known catalog/run
  authority-denial paths still call that cleanup. Reset affects both child and
  parent view, so later remount cannot recover a denied catalog filter.
- The parent ref has no input, second input, output, pattern, tool options,
  token, API result or pending intent. These remain child-local and are cleared
  by confirmed switch/remount/authority cleanup. No URL, local/session storage
  or server persistence was introduced for restoration.
- Shell routeKey1351 now includes toolCode; its existing route-change effect
  closes mobile navigation and focuses main1353–1358 on native tool changes.
  This is source evidence of the trigger, not browser focus/scroll verification.
- Existing clean-render deferred route callback5460–5463 and draft/flight guard
  remain present. Restoration does not bypass them or preserve workbench drafts.

Latest App SHA256, read twice unchanged in this resumed pass:
`9D69367EDA7F53160897E4B44CE6F39DE09945B22C64426FE77F0C1E20DF6682`.
Navigation contract SHA256 remains
`E2297DCA3F89254FCAAC3547E4DEF29ADC7C675DE94085EBE3F22B9FF7DBA7AB`.
These supersede the earlier App snapshot for this correction. Later relevant
source edits require renewed review of the affected paths.

URL/header wiring is present in the shared App but remains outside this focused
assignment; user assigns that execution-flow review to Einstein. This closure
does not certify those helpers, their payload/permission seam or0092. It also
does not claim restoration of catalog scroll position, which remains owner QA.

Evidence: read-only source/revision/branch/hash inspection and report-only patch.
`rg` was unavailable in the resumed shell; used native PowerShell Select-String
and Get-Content instead. No quota failure occurred in this resumed pass. No
product edits, tests, builds, runtime/API/browser/SQL/provider execution, fixtures,
agents, commit or push. Full browser/scroll/mobile/keyboard acceptance and
current-source CI remain owner gates, NOT RUN by this reviewer. No publication,
merge or activation approval is supplied by this bounded source closure.
