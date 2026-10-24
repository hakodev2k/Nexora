# Documents saved-version export contract

Technical continuation under existing DEC-20260909-014 and the user's instruction to implement through the remaining slices. Accountable implementation owner: current agent; independent content-security/DB/authorization reviewer: Pending. Branch/revision and full task brief remain in continuation-20261017.md. This contract specifies local code; it does not approve execution, QA, production or provider access.

## Sources and goal binding

NXG-FX20-G01/G02/G03, NXG-FX10-G01/G02, NXG-SYS-03/05/07/08; P03-CNT-002/003/008/009, P03-VER-001; FX20-BR003/005/008 and AC001/004; FX10-BR001/002/003 and AC002. Sources: features20, features10, normative features95 DOCX/MD contract, action20/action10, UX20-S04/S05/S07 and UX10-S03/S04. Current PO amendment supersedes historical future-implementation wording, not product/security rules.

## Bounded vertical slice

Export one exact authorized immutable saved PageVersion to MD or DOCX. New-page import, visual/media pins, Resume export, generic Project/Task export, PDF/HTML product export and system backup remain separate capabilities; they are not removed from approved scope or claimed implemented by this slice. No remote converter, fetch, external relationships or provider execution.

Read-only POST preview and POST request under /api/v1/documents/{pageId}/exports; GET owner job status and private content under /api/v1/document-exports/{jobId}. Request is explicit, UUID-keyed, confirmed and bound to the preview's original page ETag, version, format, codec version, conversion digest, expiry and accepted warning codes. Unknown fields rejected. Source page must currently be owner-readable/nonTrash with active containers; Archived is exportable. Page/version/type/mode/location are never changed. Historical selected versions additionally require page.history; if a once-current version becomes historical before download, history is rechecked then as well.

Preview requires transfer.export.request + documents.format.export + documents.page.read and dynamic history. Status uses transfer.export.read; download transfer.export.download, both composing current source export/read/history and live owner session. SELF only, never public/link/support/emergency. Final account/action/source proofs follow serialization/audit/receipt immediately before commit or returning bytes. Missing/wrong-owner source returns generic unavailable, no private title/count/artifact. No permission granted by stored job, preview token or UI visibility.

## Format and safety

Codec version1. Markdown source to MD preserves exact UTF8 body bytes; canonical Block v1 converts paragraph, H1-3, list/checklist, quote, divider, validated inert link, code and rectangular table. Source block IDs/owner/source IDs are not exported. Markdown to DOCX uses a CommonMark AST with only approved table/tasklist/strikethrough extensions; never HTML execution, scripts, embedded objects, network resolution or a general advanced-extension pipeline. DOCX is a local generated OOXML ZIP with escaped XML, explicit paragraphs/headings/basic emphasis/lists/tables/plain code. No external relationships, macros, credentials, audit/reasons, reminders, share tokens or private history bundle.

Known omissions/flattening are machine-readable fixed warning codes, shown before request; request must accept exactly the current codes. Examples: unsupported images/media, table-cell line breaks, nested-list/quote or advanced constructs flattened, static checklist glyphs, literal HTML/code display, XML-incompatible characters omitted. Unknown structured Block schema is blocked, not guessed/flattened. Legacy nonJSON Block text can export as literal text with an explicit warning. Conversion failure keeps source unchanged and creates no successful job. The codec never claims exact DOCX fidelity for unsupported constructs.

Technical budgets: existing canonical input <=1MiB UTF8 before parse; AST/node/depth budget10000/64; generated output<=5MiB; generated ZIP bounded to owned generated entries. These are versioned parser/serializer limits, not product quotas. No untrusted DOCX parser is introduced by this export slice. Import ZIP/XML budgets and scan flow must be contracted before its ingress is enabled. Markdig dependency is pinned to a verified published version; no external renderer or extension execution.

## Data, replay and recovery

Operations owns a separate typed DocumentExportJob table rather than weakening Calendar-only ExportJob constraints. Owner/job UUID, Page registry identity, selected version/format/codec, original source digest, state Ready, created/expiry instants, actor, rowversion and private bounded artifact bytes. No cached source title/body in metadata/receipt. Artifact bytes are intentional protected output, never an ordinary Files item/public URL. Synchronous generation commits Ready metadata+artifact+audits+opaque receipt atomically; no background worker or fake Queued/Running/Delivered state. Expiry15minutes follows the existing local export boundary. Expired jobs expose Expired status and no bytes. Source deletion/revocation invalidates download, even if bytes remain stored. Job reference retains minimal registry tombstone, not a PageVersion pin; purge must not be blocked solely by an expired export artifact.

Receipt namespace versioned; original replay precedes fresh ETag/lifecycle/preview-expiry validation, verifies actor/owner/page/job/format/status/original actions and returns opaque commit/job identity. It never serializes source fields/output. Current original actions still required; later read grant never upgrades old ACK to body. Fresh request rechecks the current exact source/hash and accepted warnings. Unknown result freezes original request/key for retry or explicit discard; discard never implies rollback. No automatic source GET after ACK. Status/download are explicit user reads, not mutation replay.

UI stays in canonical Documents context. It labels exact saved version and requires explicit acknowledgement when draft differs; it never auto-saves or exports unsaved draft. Explicit format choice, warning/field disclosure, confirmed generation, separate current-authorized status/download, expiry/unavailable messages, same-key uncertainty recovery and modal dirty/unload protection. Download is authenticated blob, safe generated filename and no-store/nosniff/attachment; no source data in URL beyond owned route identity.

Additive schema/permission metadata only, no records/grants/demo files. Rollback disables export ingress and retains jobs/artifacts/receipts until a separately approved retention/cleanup policy; do not drop populated schema or weaken Calendar constraints. No schema application, download/runtime exercise or purge performed in this run.

## Evidence gates

Only API Release compile and frontend TypeScript/Vite build are agent-executed implementation-error detection. Human owner verifies real SQL owner/replay/expiry/current-history/source-revoke races, artifact content/ZIP validity, CommonMark/OOXML fidelity and keyboard/download flows. No tests, artificial fixtures or demo records added/run by agent. All runtime/functional acceptance and independent security/DB/source-boundary review remain NOT RUN/Pending until owner evidence exists.
