# FX-20 — Documents / Notes / Knowledge — UX/UI Specification

> Current specification · reconciled 2026-09-08 · Docs-only. [Previous version](../../history/20260908/snapshot/docs/ux-ui/modules/20-documents.md) is historical evidence, not implementation input.

Review 2026-09-07 · Baseline `b85f0f314da8ca7dcee8dad156e7b538ba52c287` · Documentation only; no schema, migrations or application code executed.

Approved source behavior remains Approved; routine interaction choices below are Resolved delegated under DEC-GOV-001. Major Q-linked behaviors remain Proposed/Blocked. Route strings are navigation proposals, not existing routes or API endpoints.

## 1. Scope

Documents is the only content menu; Document/Note/Knowledge are Page types with manual Save and bounded immutable hierarchy.

## 2. Requirement sources

- [FX-20 feature](../../features/20-documents.md) — business fields, rules and AC remain authoritative.
- [Common behavior](../../features/00-shared-behavior.md) and [source routing](../../features/93-requirement-routing.md).
- [Current requirements](../../requirements/00-product-charter.md), [decision queue](../../features/90-open-decisions.md), [reconciliation](../../design-review/02-reconciliation.md).
- [Normative common screen contracts](../global/15-screen-contracts.md); [database feature coverage](../../design-database/00-overview.md).

Source identifiers reviewed: `DEC-GOV-001`, `FX-20-BR-001`, `FX-20-BR-002`, `FX-20-BR-003`, `FX-20-BR-004`, `FX-20-BR-005`, `FX-20-BR-006`, `FX-20-BR-007`, `FX-20-BR-008`, `FX-20-AC-001`, `FX-20-AC-002`, `FX-20-AC-003`, `FX-20-AC-004`, `FX-20-AC-005`, `DEC-KNW-001`, `DEC-KNW-002`, `DEC-KNW-003`, `DEC-KNW-004`, `DEC-KNW-005`, `DEC-KNW-006`, `DEC-KNW-007`, `DEC-KNW-008`, `DEC-KNW-009`, `DEC-KNW-010`, `DEC-KNW-011`, `DEC-KNW-012`, `DEC-KNW-013`, `DEC-KNW-014`, `DEC-KNW-015`, `DEC-KNW-016`, `DEC-KNW-017`, `DEC-KNW-018`, `DEC-KNW-019`, `DEC-KNW-020`, `DEC-KNW-021`, `DEC-KNW-022`, `DEC-KNW-023`, `DEC-KNW-024`, `DEC-KNW-025`, `DEC-KNW-026`, `DEC-KNW-027`, `DEC-KNW-028`, `DEC-KNW-029`, `DEC-KNW-030`, `DEC-KNW-031`, `DEC-KNW-032`, `DEC-KNW-033`, `DEC-KNW-034`, `DEC-KNW-035`, `DEC-KNW-036`, `DEC-KNW-037`, `DEC-KNW-038`, `DEC-KNW-039`, `DEC-KNW-040`, `DEC-KNW-041`, `DEC-KNW-042`, `DEC-KNW-043`, `P03-CNT-001`, `P03-CNT-002`, `P03-CNT-003`, `P03-CNT-004`, `P03-CNT-005`, `P03-CNT-006`, `P03-CNT-007`, `P03-CNT-008`, `P03-CNT-009`, `P03-DOC-001`, `P03-DOC-002`, `P03-DOC-003`, `P03-DOC-004`, `P03-DOC-005`, `P03-DOC-006`, `P03-DOC-007`, `P03-DOC-008`, `P03-DOC-009`, `P03-DOC-010`, `P03-DOC-011`, `P03-DOC-012`, `P03-DOC-013`, `P03-DOC-014`, `P03-DOC-015`, `P03-DOC-016`, `P03-DOC-017`, `P03-DOC-018`, `P03-DOC-019`, `P03-DOC-020`, `P03-DOC-021`, `P03-DOC-022`, `P03-DOC-023`, `P03-DOC-024`, `P03-DOC-025`, `P03-DOC-026`, `P03-DOC-027`, `P03-DOC-028`, `P03-DOC-029`, `P03-DOC-030`, `P03-DOC-031`, `P03-DOC-032`, `P03-DOC-033`, `P03-DOC-034`, `P03-DOC-035`, `P03-DOC-036`, `P03-DOC-037`, `P03-DOC-038`, `P03-DOC-039`, `P03-DOC-040`, `P03-DOC-041`, `P03-DOC-042`, `P03-DOC-043`, `P03-DOC-044`, `P03-DOC-045`, `P03-DOC-046`, `P03-DOC-047`, `P03-VER-001`, `P03-VER-002`, `P03-VER-003`, `P03-VER-004`, `P03-VER-005`

## 3. Reference products

- [Notion](https://www.notion.com/help/writing-and-editing-basics) — checked2026-09-07. Evidence limit: Official documentation baseline plus current page reference; exact autosave rejection comes from Nexora PO.
- [Google Docs](https://support.google.com/docs/answer/190843?hl=en) — checked2026-09-07. Evidence limit: Official help reference; no document edited.
- [Notion](https://www.notion.com/help/navigate-with-the-sidebar) — checked2026-09-07. Evidence limit: Official help text only.

## 4. Reference behavior analysis

| Product | Observed behavior | Classification | Nexora decision |
| --- | --- | --- | --- |
| [Notion](https://www.notion.com/help/writing-and-editing-basics) | Page content is built from typed editable blocks. | ADAPT | Bounded Block editor or immutable Markdown mode, explicit manual Save; reject autosave/coediting/databases/unlimited tree. |
| [Google Docs](https://support.google.com/docs/answer/190843?hl=en) | Version history lets users inspect earlier document content. | ADAPT | Manual Save produces immutable versions; Restore creates a new one; no history retention borrowed. |
| [Notion](https://www.notion.com/help/navigate-with-the-sidebar) | Sidebar separates navigation and Trash; trashed pages are not editable before restore. | ADAPT | Collapsible module groups and module-local tree; reject workspace/teamspace switch and retention policy. |

No reference grants new scope, sharing rights, provider budget, retention or financial/security semantics. Explicit rejected behavior is listed in the Nexora-decision column; unapproved Q capabilities remain Proposed, not Resolved.

## 5. UX principles for this module

Make the source and current state clear before actions. Keep manual Save, bounded immutable hierarchy and version provenance visible. Use common primitives; no separate module visual language.

## 6. Information architecture

- FX20-S01 — Documents library: BROWSE profile.
- FX20-S02 — Folder view: BROWSE profile.
- FX20-S03 — Create Page: FORM profile.
- FX20-S04 — Block editor: EDITOR profile.
- FX20-S05 — Markdown editor: EDITOR profile.
- FX20-S06 — Page metadata / cover: FORM profile.
- FX20-S07 — Page versions: HISTORY profile.
- FX20-S08 — Archived pages: BROWSE profile.
- FX20-S09 — Archive / Unarchive tree preview: DIALOG profile.
- FX20-S10 — Document Trash and restore: BROWSE profile.

These are screen surfaces, not necessarily separate backend resources; create/edit or views may share a route with distinct state. Module root belongs to [global IA](../global/01-information-architecture.md); no Workspace menu.

## 7. Screen inventory

| Screen ID | Screen | Route proposal | Purpose / data focus | Primary action |
| --- | --- | --- | --- | --- |
| FX20-S01 | Documents library | /documents | Level1Folder navigation and root pages outside Folder; page card/row Title,Type,Tag | Create Page |
| FX20-S02 | Folder view | /documents/folders/:folderId | Direct child Folders and direct root pages; breadcrumb path; page fields same library | Create Page here |
| FX20-S03 | Create Page | /documents/new | Explicit Type selector; explicit EditorMode selector; Title; immutable rootFolder or childParent; optional Tag/visual | Create Page and Save version1 |
| FX20-S04 | Block editor | /documents/pages/:pageId | Page header/status/unsaved badge; Title; bounded block canvas; module-local child sidebar; metadata | Save |
| FX20-S05 | Markdown editor | /documents/pages/:pageId?editor=markdown | Monospace input; sanitized preview; Title/status/manual Save; child sidebar | Save |
| FX20-S06 | Page metadata / cover | /documents/pages/:pageId/metadata | One Tag picker/create; Icon or Cover choice; emoji/builtin picker; file crop/focal preview | Apply to draft then Save Page |
| FX20-S07 | Page versions | /documents/pages/:pageId/history | Version number/time; complete readonly snapshot; differences; sourceVersion | Restore as new version |
| FX20-S08 | Archived pages | /documents/archived | Flat individually listed parent and child pages; original state; parent relation label | Open readonly page |
| FX20-S09 | Archive / Unarchive tree preview | /documents/pages/:pageId/archive | Affected current parent/children; their prior states; excluded independently Archived/Trash pages | Archive / Unarchive |
| FX20-S10 | Document Trash and restore | /trash?module=documents | Folder/page tree deletion batches; original location; independent child deletions | Preview restore |

## 8. Navigation

Entry from global registered module, source link or authorized deep link. Each Screen has explicit Back/Cancel below; in-module return preserves query/view/date/scroll. Public/Support/Emergency routes keep their own access context, never switch silently to owner API. Create/Edit dirty-leave follows UX-05.

## 9. Primary user journeys

**Primary:** Library → Create Page → explicit Type+Editor+Title and immutable location → Save version1 → edit/manual Save → Publish/share or Archive → cohort-aware restore.

**Alternative:** open existing resource from authorized Search/Favorite/notification/source link; resolve current lifecycle before rendering primary action. If this is an operational/auth screen, use its authorized parent navigation rather than inventing a favorite/shareable resource.

**Empty:** no owner data → declared New/Add action only if allowed; no matching filters → clear filters; provider-only screen → explain source/refresh, not fake CRUD.

**Error:** failed query/provider → safe message and retry; failed mutation keeps authorized input/original row; conflict → compare/reload, never blind overwrite.

**Destructive:** select actual permitted operation from section15 → preview affected resources using section16 → revalidate → confirmed result or blocked explanation. A source without destructive action is N/A, not generic Delete.

**Permission/readonly:** recheck actor/module/source; lifecycle banner and source-safe inspect only; expired/revoked mode clears data and exits authorized parent.

## 10. Screen specifications

All screens below inherit every state/layout/keyboard/exit rule in [UX-15A](../global/15-screen-contracts.md). The following rows bind concrete content, commands, controls and exceptions; inheritance is normative, not a placeholder.

### FX20-S01 — Documents library

| Dimension | Specification |
| --- | --- |
| Purpose / profile | BROWSE — Level1Folder navigation and root pages outside Folder; page card/row Title,Type,Tag |
| Entry / proposed route | /documents; module/source navigation or authorized deep link. |
| Header / layout | Screen title: Documents library. Shared browse profile; header → controls → declared content → feedback. |
| Primary action | Create Page; available only when section15/context permits; otherwise explain lifecycle/policy. |
| Secondary actions | New Folder; Grid/Table; Archived. Back/Cancel always has authorized fallback. |
| Content regions / fields | Level1Folder navigation and root pages outside Folder; page card/row Title,Type,Tag |
| Search / filters / sorting / pagination | Local Title/Tag; Type/Tag/CreatedAt; Updated DESC;25/page. Controls not listed here are N/A, not implicit new fields. |
| Interaction / validation overrides | No recursive query. Global Search clearly separate. Archived pages absent here except sidebar child links when reading parent. |
| Loading / empty / error | UX-15A state contract. Empty: declared data absent; no matches: clear listed query controls; fetch error: safe retry. These are distinct, no error-as-empty. |
| Disabled / readonly / Archived / Trash | UX-15A plus exact section15 matrix. No lifecycle in source = N/A. Do not render unauthorized payload behind disabled controls. |
| Conflict / destructive | Current revision and parent guards, section16 dialogs. Readonly surfaces only refresh, never forced write. |
| Desktop / tablet / mobile | UX-10 browse profile. Keep 'Documents library' context and required fields; mobile prioritizes first declared identity and status/time/value, remaining fields detail/expand. Exact approved card/table fields in section12 override generic priority choices. |
| Keyboard / accessibility | UX-11 and profile: visible focus, labeled controls, no drag/hover-only action, status text; dialog focus trap/return; charts/table values reachable. |
| Exit / return | Back/Cancel → actual invoking screen with view/filter/date/scroll restored; direct deep link → module root or safe Home/Admin landing; dirty form guard and revoked-data clear take precedence. |

### FX20-S02 — Folder view

| Dimension | Specification |
| --- | --- |
| Purpose / profile | BROWSE — Direct child Folders and direct root pages; breadcrumb path; page fields same library |
| Entry / proposed route | /documents/folders/:folderId; module/source navigation or authorized deep link. |
| Header / layout | Screen title: Folder view. Shared browse profile; header → controls → declared content → feedback. |
| Primary action | Create Page here; available only when section15/context permits; otherwise explain lifecycle/policy. |
| Secondary actions | Create subfolder only at level1; Rename Folder; Trash tree. Back/Cancel always has authorized fallback. |
| Content regions / fields | Direct child Folders and direct root pages; breadcrumb path; page fields same library |
| Search / filters / sorting / pagination | Same direct-location search/filter; Updated DESC pages; Folder title ASC. Controls not listed here are N/A, not implicit new fields. |
| Interaction / validation overrides | No Folder move/reparent. Breadcrumb/Back retains filter/view; level2 no add-subfolder option. |
| Loading / empty / error | UX-15A state contract. Empty: declared data absent; no matches: clear listed query controls; fetch error: safe retry. These are distinct, no error-as-empty. |
| Disabled / readonly / Archived / Trash | UX-15A plus exact section15 matrix. No lifecycle in source = N/A. Do not render unauthorized payload behind disabled controls. |
| Conflict / destructive | Current revision and parent guards, section16 dialogs. Readonly surfaces only refresh, never forced write. |
| Desktop / tablet / mobile | UX-10 browse profile. Keep 'Folder view' context and required fields; mobile prioritizes first declared identity and status/time/value, remaining fields detail/expand. Exact approved card/table fields in section12 override generic priority choices. |
| Keyboard / accessibility | UX-11 and profile: visible focus, labeled controls, no drag/hover-only action, status text; dialog focus trap/return; charts/table values reachable. |
| Exit / return | Back/Cancel → actual invoking screen with view/filter/date/scroll restored; direct deep link → module root or safe Home/Admin landing; dirty form guard and revoked-data clear take precedence. |

### FX20-S03 — Create Page

| Dimension | Specification |
| --- | --- |
| Purpose / profile | FORM — Explicit Type selector; explicit EditorMode selector; Title; immutable rootFolder or childParent; optional Tag/visual |
| Entry / proposed route | /documents/new; module/source navigation or authorized deep link. |
| Header / layout | Screen title: Create Page. Shared form profile; header → controls → declared content → feedback. |
| Primary action | Create Page and Save version1; available only when section15/context permits; otherwise explain lifecycle/policy. |
| Secondary actions | Cancel. Back/Cancel always has authorized fallback. |
| Content regions / fields | Explicit Type selector; explicit EditorMode selector; Title; immutable rootFolder or childParent; optional Tag/visual |
| Search / filters / sorting / pagination | No remembered/default Type or Mode. Controls not listed here are N/A, not implicit new fields. |
| Interaction / validation overrides | Location summary warns fixed after creation. Child create only from root context; cannot choose both own Folder and Parent. |
| Loading / empty / error | UX-15A state contract. Empty: declared data absent; no matches: clear listed query controls; fetch error: safe retry. These are distinct, no error-as-empty. |
| Disabled / readonly / Archived / Trash | UX-15A plus exact section15 matrix. No lifecycle in source = N/A. Do not render unauthorized payload behind disabled controls. |
| Conflict / destructive | Current revision and parent guards, section16 dialogs. Readonly surfaces only refresh, never forced write. |
| Desktop / tablet / mobile | UX-10 form profile. Keep 'Create Page' context and required fields; mobile prioritizes first declared identity and status/time/value, remaining fields detail/expand. Exact approved card/table fields in section12 override generic priority choices. |
| Keyboard / accessibility | UX-11 and profile: visible focus, labeled controls, no drag/hover-only action, status text; dialog focus trap/return; charts/table values reachable. |
| Exit / return | Back/Cancel → actual invoking screen with view/filter/date/scroll restored; direct deep link → module root or safe Home/Admin landing; dirty form guard and revoked-data clear take precedence. |

### FX20-S04 — Block editor

| Dimension | Specification |
| --- | --- |
| Purpose / profile | EDITOR — Page header/status/unsaved badge; Title; bounded block canvas; module-local child sidebar; metadata |
| Entry / proposed route | /documents/pages/:pageId; module/source navigation or authorized deep link. |
| Header / layout | Screen title: Block editor. Header with current lifecycle + unsaved badge + explicit Save; editor canvas and source navigation. |
| Primary action | Save; available only when section15/context permits; otherwise explain lifecycle/policy. |
| Secondary actions | Publish/Return Draft; Archive; Share eligible; History; Trash. Back/Cancel always has authorized fallback. |
| Content regions / fields | Page header/status/unsaved badge; Title; bounded block canvas; module-local child sidebar; metadata |
| Search / filters / sorting / pagination | Block toolbar/slash menu only approved blocks. Controls not listed here are N/A, not implicit new fields. |
| Interaction / validation overrides | No autosave/comments/coediting/database. Explicit Save remains reachable sticky header; leaving dirty page prompts Save/Discard/Keep editing. Sidebar Archived child labeled readonly. |
| Loading / empty / error | UX-15A state contract. Empty: declared data absent; no matches: clear listed query controls; fetch error: safe retry. These are distinct, no error-as-empty. |
| Disabled / readonly / Archived / Trash | UX-15A plus exact section15 matrix. No lifecycle in source = N/A. Do not render unauthorized payload behind disabled controls. |
| Conflict / destructive | Current revision and parent guards, section16 dialogs. Readonly surfaces only refresh, never forced write. |
| Desktop / tablet / mobile | UX-10 editor profile. Keep 'Block editor' context and required fields; mobile prioritizes first declared identity and status/time/value, remaining fields detail/expand. Exact approved card/table fields in section12 override generic priority choices. |
| Keyboard / accessibility | UX-11 and profile: visible focus, labeled controls, no drag/hover-only action, status text; dialog focus trap/return; charts/table values reachable. |
| Exit / return | Back/Cancel → actual invoking screen with view/filter/date/scroll restored; direct deep link → module root or safe Home/Admin landing; dirty form guard and revoked-data clear take precedence. |

### FX20-S05 — Markdown editor

| Dimension | Specification |
| --- | --- |
| Purpose / profile | EDITOR — Monospace input; sanitized preview; Title/status/manual Save; child sidebar |
| Entry / proposed route | /documents/pages/:pageId?editor=markdown; module/source navigation or authorized deep link. |
| Header / layout | Screen title: Markdown editor. Header with current lifecycle + unsaved badge + explicit Save; editor canvas and source navigation. |
| Primary action | Save; available only when section15/context permits; otherwise explain lifecycle/policy. |
| Secondary actions | Edit/Preview toggle; lifecycle actions; History. Back/Cancel always has authorized fallback. |
| Content regions / fields | Monospace input; sanitized preview; Title/status/manual Save; child sidebar |
| Search / filters / sorting / pagination | Desktop optional split; mobile Edit/Preview tabs. Controls not listed here are N/A, not implicit new fields. |
| Interaction / validation overrides | Query parameter never changes immutable EditorMode; mismatched mode routes canonical editor. Raw HTML escaped; no arbitrary embed/script. |
| Loading / empty / error | UX-15A state contract. Empty: declared data absent; no matches: clear listed query controls; fetch error: safe retry. These are distinct, no error-as-empty. |
| Disabled / readonly / Archived / Trash | UX-15A plus exact section15 matrix. No lifecycle in source = N/A. Do not render unauthorized payload behind disabled controls. |
| Conflict / destructive | Current revision and parent guards, section16 dialogs. Readonly surfaces only refresh, never forced write. |
| Desktop / tablet / mobile | UX-10 editor profile. Keep 'Markdown editor' context and required fields; mobile prioritizes first declared identity and status/time/value, remaining fields detail/expand. Exact approved card/table fields in section12 override generic priority choices. |
| Keyboard / accessibility | UX-11 and profile: visible focus, labeled controls, no drag/hover-only action, status text; dialog focus trap/return; charts/table values reachable. |
| Exit / return | Back/Cancel → actual invoking screen with view/filter/date/scroll restored; direct deep link → module root or safe Home/Admin landing; dirty form guard and revoked-data clear take precedence. |

### FX20-S06 — Page metadata / cover

| Dimension | Specification |
| --- | --- |
| Purpose / profile | FORM — One Tag picker/create; Icon or Cover choice; emoji/builtin picker; file crop/focal preview |
| Entry / proposed route | /documents/pages/:pageId/metadata; module/source navigation or authorized deep link. |
| Header / layout | Screen title: Page metadata / cover. Shared form profile; header → controls → declared content → feedback. |
| Primary action | Apply to draft then Save Page; available only when section15/context permits; otherwise explain lifecycle/policy. |
| Secondary actions | Cancel; remove visual. Back/Cancel always has authorized fallback. |
| Content regions / fields | One Tag picker/create; Icon or Cover choice; emoji/builtin picker; file crop/focal preview |
| Search / filters / sorting / pagination | Tag search; crop keyboard numeric controls. Controls not listed here are N/A, not implicit new fields. |
| Interaction / validation overrides | Tag switch does not auto-save body. Folder/Parent/Type/Editor shown locked; crop preview not overwrite source binary. Tag delete blocked by active/Archived/Trash uses. |
| Loading / empty / error | UX-15A state contract. Empty: declared data absent; no matches: clear listed query controls; fetch error: safe retry. These are distinct, no error-as-empty. |
| Disabled / readonly / Archived / Trash | UX-15A plus exact section15 matrix. No lifecycle in source = N/A. Do not render unauthorized payload behind disabled controls. |
| Conflict / destructive | Current revision and parent guards, section16 dialogs. Readonly surfaces only refresh, never forced write. |
| Desktop / tablet / mobile | UX-10 form profile. Keep 'Page metadata / cover' context and required fields; mobile prioritizes first declared identity and status/time/value, remaining fields detail/expand. Exact approved card/table fields in section12 override generic priority choices. |
| Keyboard / accessibility | UX-11 and profile: visible focus, labeled controls, no drag/hover-only action, status text; dialog focus trap/return; charts/table values reachable. |
| Exit / return | Back/Cancel → actual invoking screen with view/filter/date/scroll restored; direct deep link → module root or safe Home/Admin landing; dirty form guard and revoked-data clear take precedence. |

### FX20-S07 — Page versions

| Dimension | Specification |
| --- | --- |
| Purpose / profile | HISTORY — Version number/time; complete readonly snapshot; differences; sourceVersion |
| Entry / proposed route | /documents/pages/:pageId/history; module/source navigation or authorized deep link. |
| Header / layout | Screen title: Page versions. Shared history profile; header → controls → declared content → feedback. |
| Primary action | Restore as new version; available only when section15/context permits; otherwise explain lifecycle/policy. |
| Secondary actions | Preview; Back. Back/Cancel always has authorized fallback. |
| Content regions / fields | Version number/time; complete readonly snapshot; differences; sourceVersion |
| Search / filters / sorting / pagination | Version newest25/page. Controls not listed here are N/A, not implicit new fields. |
| Interaction / validation overrides | Restore editable Draft/Published only; preserve current immutable topology and full history; tag/media rebind preview, no resurrection of purged files. |
| Loading / empty / error | UX-15A state contract. Empty: declared data absent; no matches: clear listed query controls; fetch error: safe retry. These are distinct, no error-as-empty. |
| Disabled / readonly / Archived / Trash | UX-15A plus exact section15 matrix. No lifecycle in source = N/A. Do not render unauthorized payload behind disabled controls. |
| Conflict / destructive | Current revision and parent guards, section16 dialogs. Readonly surfaces only refresh, never forced write. |
| Desktop / tablet / mobile | UX-10 history profile. Keep 'Page versions' context and required fields; mobile prioritizes first declared identity and status/time/value, remaining fields detail/expand. Exact approved card/table fields in section12 override generic priority choices. |
| Keyboard / accessibility | UX-11 and profile: visible focus, labeled controls, no drag/hover-only action, status text; dialog focus trap/return; charts/table values reachable. |
| Exit / return | Back/Cancel → actual invoking screen with view/filter/date/scroll restored; direct deep link → module root or safe Home/Admin landing; dirty form guard and revoked-data clear take precedence. |

### FX20-S08 — Archived pages

| Dimension | Specification |
| --- | --- |
| Purpose / profile | BROWSE — Flat individually listed parent and child pages; original state; parent relation label |
| Entry / proposed route | /documents/archived; module/source navigation or authorized deep link. |
| Header / layout | Screen title: Archived pages. Shared browse profile; header → controls → declared content → feedback. |
| Primary action | Open readonly page; available only when section15/context permits; otherwise explain lifecycle/policy. |
| Secondary actions | Unarchive; Move to Trash. Back/Cancel always has authorized fallback. |
| Content regions / fields | Flat individually listed parent and child pages; original state; parent relation label |
| Search / filters / sorting / pagination | Title/Tag/Type/CreatedAt scoped Archived list; Updated DESC;25/page. Controls not listed here are N/A, not implicit new fields. |
| Interaction / validation overrides | Parent Unarchive restores only children archived in same operation; already-independent Archived children stay. Child Unarchive blocked while parent Archived. |
| Loading / empty / error | UX-15A state contract. Empty: declared data absent; no matches: clear listed query controls; fetch error: safe retry. These are distinct, no error-as-empty. |
| Disabled / readonly / Archived / Trash | UX-15A plus exact section15 matrix. No lifecycle in source = N/A. Do not render unauthorized payload behind disabled controls. |
| Conflict / destructive | Current revision and parent guards, section16 dialogs. Readonly surfaces only refresh, never forced write. |
| Desktop / tablet / mobile | UX-10 browse profile. Keep 'Archived pages' context and required fields; mobile prioritizes first declared identity and status/time/value, remaining fields detail/expand. Exact approved card/table fields in section12 override generic priority choices. |
| Keyboard / accessibility | UX-11 and profile: visible focus, labeled controls, no drag/hover-only action, status text; dialog focus trap/return; charts/table values reachable. |
| Exit / return | Back/Cancel → actual invoking screen with view/filter/date/scroll restored; direct deep link → module root or safe Home/Admin landing; dirty form guard and revoked-data clear take precedence. |

### FX20-S09 — Archive / Unarchive tree preview

| Dimension | Specification |
| --- | --- |
| Purpose / profile | DIALOG — Affected current parent/children; their prior states; excluded independently Archived/Trash pages |
| Entry / proposed route | /documents/pages/:pageId/archive; module/source navigation or authorized deep link. |
| Header / layout | Screen title: Archive / Unarchive tree preview. Named modal with preview/reason and footer actions. |
| Primary action | Archive / Unarchive; available only when section15/context permits; otherwise explain lifecycle/policy. |
| Secondary actions | Cancel. Back/Cancel always has authorized fallback. |
| Content regions / fields | Affected current parent/children; their prior states; excluded independently Archived/Trash pages |
| Search / filters / sorting / pagination | Dependency list scroll, no hidden children. Controls not listed here are N/A, not implicit new fields. |
| Interaction / validation overrides | Archive root atomically includes active children; existing Archived child excluded. Unarchive never restores Trash automatically. |
| Loading / empty / error | UX-15A state contract. Empty: declared data absent; no matches: clear listed query controls; fetch error: safe retry. These are distinct, no error-as-empty. |
| Disabled / readonly / Archived / Trash | UX-15A plus exact section15 matrix. No lifecycle in source = N/A. Do not render unauthorized payload behind disabled controls. |
| Conflict / destructive | Current revision and parent guards, section16 dialogs. Readonly surfaces only refresh, never forced write. |
| Desktop / tablet / mobile | UX-10 dialog profile. Keep 'Archive / Unarchive tree preview' context and required fields; mobile prioritizes first declared identity and status/time/value, remaining fields detail/expand. Exact approved card/table fields in section12 override generic priority choices. |
| Keyboard / accessibility | UX-11 and profile: visible focus, labeled controls, no drag/hover-only action, status text; dialog focus trap/return; charts/table values reachable. |
| Exit / return | Back/Cancel → actual invoking screen with view/filter/date/scroll restored; direct deep link → module root or safe Home/Admin landing; dirty form guard and revoked-data clear take precedence. |

### FX20-S10 — Document Trash and restore

| Dimension | Specification |
| --- | --- |
| Purpose / profile | BROWSE — Folder/page tree deletion batches; original location; independent child deletions |
| Entry / proposed route | /trash?module=documents; module/source navigation or authorized deep link. |
| Header / layout | Screen title: Document Trash and restore. Shared browse profile; header → controls → declared content → feedback. |
| Primary action | Preview restore; available only when section15/context permits; otherwise explain lifecycle/policy. |
| Secondary actions | Delete permanently. Back/Cancel always has authorized fallback. |
| Content regions / fields | Folder/page tree deletion batches; original location; independent child deletions |
| Search / filters / sorting / pagination | Common Trash filters/counts. Controls not listed here are N/A, not implicit new fields. |
| Interaction / validation overrides | Delete Folder/root includes current tree; separately deleted child keeps own batch. Restore parent before child; immutable parent/folder never repaired by moving. |
| Loading / empty / error | UX-15A state contract. Empty: declared data absent; no matches: clear listed query controls; fetch error: safe retry. These are distinct, no error-as-empty. |
| Disabled / readonly / Archived / Trash | UX-15A plus exact section15 matrix. No lifecycle in source = N/A. Do not render unauthorized payload behind disabled controls. |
| Conflict / destructive | Current revision and parent guards, section16 dialogs. Readonly surfaces only refresh, never forced write. |
| Desktop / tablet / mobile | UX-10 browse profile. Keep 'Document Trash and restore' context and required fields; mobile prioritizes first declared identity and status/time/value, remaining fields detail/expand. Exact approved card/table fields in section12 override generic priority choices. |
| Keyboard / accessibility | UX-11 and profile: visible focus, labeled controls, no drag/hover-only action, status text; dialog focus trap/return; charts/table values reachable. |
| Exit / return | Back/Cancel → actual invoking screen with view/filter/date/scroll restored; direct deep link → module root or safe Home/Admin landing; dirty form guard and revoked-data clear take precedence. |

## 11. Forms and validation

Every create explicitly requires DocumentType(Document/Note/Knowledge), EditorMode(Block/Markdown) and Title1..200; body optional<=1MiB. Type/Mode/Parent/Folder immutable including none. Root0..1Folder; child one root parent, follows folder. Optional exactly one Tag; Emoji/builtin Icon OR uploaded cover JPG/PNG/WebP<=5MiB/25MP cropped nondestructively. Title editable Draft/Published, duplicates anywhere allowed.

[Common forms](../global/05-forms-and-validation.md) define field error timing, Save, cancellation, stale session and conflict; DB required technical columns are server-owned, never rendered as form fields.

## 12. Lists / Grid / Table / Kanban behavior

Library default Grid/Table both fields Title,DocumentType,Tag only. Initially level1Folders plus root pages outside Folder; Folder view direct child folders/root pages only. Search Title/Tag; filters Type,Tag,CreatedAt range ONLY direct location; Updated DESC then Id. Folder rows navigation, no fabricated type/tag. Archived flat pages parent+child separately.

Use [UX-06](../global/06-lists-grids-tables-kanban.md). Only views declared above exist; no Kanban simply because resource has a status. Page Grid/Table both Title/DocumentType/Tag only. Folders are navigation entries, not fake pages; sidebar shows archived children readonly.

## 13. Search / Filter / Sort

Library default Grid/Table both fields Title,DocumentType,Tag only. Initially level1Folders plus root pages outside Folder; Folder view direct child folders/root pages only. Search Title/Tag; filters Type,Tag,CreatedAt range ONLY direct location; Updated DESC then Id. Folder rows navigation, no fabricated type/tag. Archived flat pages parent+child separately.

Search/filter are owner and location scoped; reset cursor after query change, preserve draft separately. Date filters overlap unless explicit Calendar ICS fully-contained export. Status vocabulary is module-specific under shared semantic tokens, not all Completed states identical.

## 14. Lifecycle UX

Each row below is source-defined state/context, not a client-only flag. Parent gates and current server capability override otherwise available actions. Archive, terminal, Trash and purge remain distinct. Archive and Trash cohorts are separate; restore never changes immutable location.

## 15. Action matrix

| State / context | Available actions | Denied / UX explanation |
| --- | --- | --- |
| Draft | Manual Save; Publish; Archive; Trash; versions; create child if root | No share creation/resolve; existing link temporarily blocked |
| Published | Manual Save; return Draft; Share; Archive; Trash; versions | Private until valid explicit link, no auto-publication |
| Archived | Readonly; history; Unarchive if parent not Archived; Trash; existing eligible share | No edits/new share; parent Unarchive only same cohort |
| Trash | Restore original tree only if parent/location valid; purge after pins | No edit/share; child restore requires parent restored and not Archived |

**Context intersection:** Owner Self requires active verified account, installed/system/user module gates and action+resource permission. Admin/SuperAdmin own data uses Self, not global data access. Support/Emergency only explicitly registered approved safe readonly projection for the granted module; otherwise unavailable. Secret reveal/export/mutation denied in those modes. Share viewer only if this source declares an approved readonly share projection and current link qualifies; operational screens/auth/Calendar Events/pure tools do not acquire sharing from common UI.

## 16. Dialogs

| Dialog title / ID | Explanation and affected resources | Primary / cancel | Retry/error and boundary |
| --- | --- | --- | --- |
| Archive / Unarchive tree preview | Affected current parent/children; their prior states; excluded independently Archived/Trash pages | Archive / Unarchive / Cancel | Archive root atomically includes active children; existing Archived child excluded. Unarchive never restores Trash automatically. |
| D-TRASH / D-RESTORE / D-PURGE | Selected source + exact cohort/reference/pin preview | Move to Trash / Restore / Delete permanently; Cancel | Only permitted source actions; irreversible purge warning, revalidate parent/revision; no generic restore bypass. |
| D-ARCHIVE / D-UNARCHIVE | Source + previous state and affected references | Archive / Unarchive; Cancel | Only features with archive lifecycle; preserve source-specific prior state/cohort. |
| D-UNSAVED / D-CONFLICT | Authorized dirty source/current revision, no secrets in diagnostics | Save/Discard/Keep editing or Reload/Reapply/Cancel | No silent discard/overwrite; revoked access clears protected data. N/A on pure readonly screens. |

Risk style/focus/retry defaults: [UX-08 dialog contracts](../global/08-lifecycle-destructive-actions.md). Reason dialogs focus mandatory reason; irreversible confirm initially focuses Cancel. Pending response not successful action; retries use same safe idempotency key.

## 17. Loading / Empty / Error / Degraded

Each screen inherits explicit UX-15A states: initial skeleton, empty owner data, no filtered matches, fetch failure, stale authorized data, module unavailable, permission denied/revoked and conflict. This module does not need a fabricated provider-specific error surface for its ordinary local data; its registered cross-module sources may still be unavailable and must be labeled.

## 18. Permissions / Read-only / Sensitive contexts

Owner isolation applies equally to list/count/picker/history/source links. Operational authority does not supply personal-data permission. 

Use [security UX](../global/12-security-sensitive-ux.md) and [explicit modes](../global/13-admin-support-emergency.md). Readonly banner is contextual and server enforced, not merely a disabled Save over full private DTO.

## 19. Responsive behavior

[UX-10](../global/10-responsive-design.md) plus per-screen overrides is mandatory: desktop appropriate split/table, tablet drawer, mobile stacked route/sheet with Back, all fields available. Do not reset approved default view/source state on resize. Mobile Markdown Edit/Preview tabs and module-local child drawer; manual Save remains visible.

## 20. Accessibility

Keyboard-only primary/alternate/error/destructive flows, explicit labels and visible focus; semantics for tables/forms/statuses; chart/table equivalent; no drag-only; modal focus trap/return; touch/zoom/reduced-motion contracts [UX-11](../global/11-accessibility.md). Per-screen text is not certification; later assistive-tech tests must execute these paths.

## 21. Cross-module integration

Use registered source/command/projection contracts from [UX-14](../global/14-cross-module-interactions.md); links preserve owner/access mode and do not transfer ownership or mutate unrelated source.

Data design trace:

| Table / provider data | Purpose / dependency status |
| --- | --- |
| [files.FileReference](../../design-database/04-files-jobs-notifications.md#files-filereference) | Reference-aware binary retention; Technical decision |
| [documents.Folder](../../design-database/06-documents-knowledge-discovery.md#documents-folder) | Bounded immutable-location Document folder; Technical decision |
| [documents.Tag](../../design-database/06-documents-knowledge-discovery.md#documents-tag) | Single-tag Document vocabulary; Technical decision |
| [documents.Page](../../design-database/06-documents-knowledge-discovery.md#documents-page) | Single content aggregate for Document, Note and Knowledge; Technical decision |
| [documents.PageVersion](../../design-database/06-documents-knowledge-discovery.md#documents-pageversion) | Immutable canonical content and editable metadata per explicit Save; Technical decision |
| [documents.ArchiveBatch](../../design-database/06-documents-knowledge-discovery.md#documents-archivebatch) | Archive operation cohort distinct from Trash; Technical decision |
| [documents.ArchiveMember](../../design-database/06-documents-knowledge-discovery.md#documents-archivemember) | Per-page previous status within one archive cohort; Technical decision |

No direct table access from frontend/another module. [Architecture command/query contract](../../architecture/02-module-boundaries.md) and [transaction boundaries](../../design-database/11-relations-and-transactions.md) govern source mutations.

## 22. UX decisions made by delegated authority

Screen grouping/routes, shared profile selection, action placement, empty/error wording, explicit keyboard alternatives, focus return, sensible column priority and preview anatomy are **Resolved delegated**. Existing feature defaults remain, not newly PO-approved. Reference-specific scope/cost/permissions/privacy/lifecycle/financial changes are not delegated. See [normalized decision register](../decisions/ux-decisions.md).

## 23. Major open questions


## 24. Acceptance checklist

- [ ] Execute primary journey for every Screen ID and authorized deep link; Back restores context.
- [ ] Required/optional/immutable fields match feature and data dictionary; no reference-product scope added.
- [ ] Every state/action row enforced both UI and server, including parent lifecycle and permission revoke race.
- [ ] Initial empty, filtered empty, loading, failed fetch, degraded/stale, disabled/read-only and conflict are distinct.
- [ ] Each destructive/reason dialog previews exact resources, cancels without mutation and handles stale revision.
- [ ] Keyboard-only and mobile flow reaches every action, detail field and safe exit; no drag/hover-only control.
- [ ] No secret or unauthorized payload in previews, error, URL, notification, logs or persistent browser state.
- [ ] Source BR/AC IDs and Q dependencies traced; Q-gated actions not treated as Approved.

**Five-question review:** User goal and simplest IA are sections1/6/9; mature reference evidence and adaptations/rejections sections3/4; important remaining choices are explicitly Q-gated in section23, not left for frontend to invent. This checklist is specification for later execution, not tests marked passed in a docs-only task.

## Canonical action binding — catalog v1

[FX-20 action catalog](../../action-catalog/modules/20-documents.md) and [screen bindings](../../action-catalog/06-screen-bindings.md) define exact keys, grantable contexts and Q gates. Descriptive verbs above are not permission names. Admin Self also needs explicit allowed action; User Self uses enabled-module owner baseline. SuperAdmin alone changes role/module/action grants. Local UI visibility does not replace server authorization.
