# UI/UX execution report — partial coverage, not full acceptance

Actual current browser execution uses Chromium headless-shell153.0.8010.12,
loopback Vite and controlled synthetic HTTP. Vite and browser must share the same
exec network namespace; an earlier separate-session attempt was connection-refused
before UI assertions and is not a product UI failure. No real SQL-backed writes
are certified by these controlled tests.

## Actual interactions and visual judgment

| Surface | Actions actually exercised | Outcome / evidence |
|---|---|---|
| Public account screens | Login/register/forgot/reset/verify render, skip-link keyboard focus, invalid email retained with alert, API503 retry | Existing controlled browser smoke4pass across desktop/mobile; sampled axe only |
| Authenticated shell | Native navigation, module-disabled FX35, ten named route render/reflow samples, account disclosure Escape/focus |4case smoke plus width probes; not every route state |
| Home / Dashboard | One QuickCreate, settled layout, no whole-page overflow320/768/1440 |3width probes pass; home-*.png |
| Finance category dialog | Open, one heading, native form-associated footer, type dirty draft, Cancel→keep editing retains draft, discard |3width probes; finance-category-*.png |
| Finance record dialog | Open, one heading, explicitly blank Currency, correct form/footer, reflow |3width probes; finance-record-*.png; no write certification |
| Bookmark create / ACK | Open dirty form, Back/keep-editing/Escape/discard/focus restoration; opaque ACK clears private rows without automatic read | Existing controlled smoke; not full bookmark lifecycle |
| Notifications ACK | Mutation clears private rows without implicit protected GET; explicit reload obtains current ETags | Component and controlled browser; SQL source effects separate |
| Document diff/import | Exact source reconstruction, bounds/fallback, inert rendering, stale selectors/mode response defenses |18component/pure tests; no current browser visual acceptance for every Document popup |
| Focus/Time | Dirty form recovery, in-flight locks, revoked data clear, late responses and nonoverlap polling |10component tests; browser and real SQL matrix pending |

Nine current PNGs, case JSON, JUnit and logs are under evidence/frontend.
Root inspected Home320 and FinanceCategory320 screenshots; frontend worker
inspected Home1440/320 and both Finance mobile dialogs. Finance now has one
readable title and coherent actions. Home still presents a very long list,
technical provider/layout labels and dense configuration; professional product
UX is PARTIAL and needs further design work. No numerical visual score invented.
Final selector grid CSS was followed by a fresh3width probe; no hidden-root
overflow trick or disabled security control is used as acceptance.

## Every source popup retains a disposition

The existing source register enumerates60shared JSX occurrences plus29specialized
components. Its individual state/capability prerequisites remain required:
[popup-source-register](../pr4-uiux-audit-20261019/popup-source-register.md).
The generated popup-disposition.csv preserves every enumerated row distinctly.
Each remains NOT_EXERCISED for its full required persona/lifecycle/error/race
matrix until exact scenario evidence exists. The demonstrated Finance/Bookmark
dialogs above do not certify all similarly named invocation occurrences.

Read-only open/close samples do not verify Save, conflict, dirty draft, source
revocation, loading/empty/populated and denied behavior for every popup.
Actual SQL fixtures/rights for each source and viewport are the prerequisite.

## Remaining page coverage

All36historical route rows remain linked in
[route-evidence-register](../pr4-uiux-audit-20261019/route-evidence-register.md).
Historical screenshots are not current full-page acceptance. Module-unavailable
cards and generic ModuleEntry are unimplemented/gated states, not completed UX.
Full page/pop-up/per-persona SQL-backed review, screen-reader checks, and full
keyboard/error/conflict/mobile matrices remain IN_PROGRESS.
