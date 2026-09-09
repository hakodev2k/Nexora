# DOCX / MD và Calendar liên kết nội bộ

Source DEC-20260907-Q11/Q12; UX/API details Resolved delegated, docs only.

## Documents / Resume formats

R1 basic Document import/export and new Resume file versions: .docx and .md. General Files/attachments retain their separately approved types; this is not a global ban on uploaded PDFs/images. User still explicitly chooses DocumentType and EditorMode on each new page; neither is changed on import/restore. Import is new-page flow with preview and manual Save; it is not autosave/overwrite of existing content. Recommend Block for DOCX and Markdown for MD without automatically selecting immutable choices.

Supported initial subset: paragraphs/headings, basic emphasis, ordered/unordered lists, simple tables, escaped code/plain text, owned clean embedded images where target mode supports them. DOCX↔Markdown conversion previews every omission/flattening before Save. Tracked changes, comments, macros, embedded OLE, external relationships/images, advanced pagination/layout, equations and Word-specific fields are unsupported in this basic contract; report them explicitly, never silently claim exact fidelity. DOCM rejected. No remote document conversion service or Google Docs redirect.

DOCX parser checks ZIP entry names/count/uncompressed size/compression ratios, disables external XML entities/relationships and macro execution; no URL fetch. Existing Files upload/scan limits and canonical body size apply; parser limits versioned/configured and tested, not unbounded. MD stored UTF-8 with literal input kept through supported conversion; raw HTML sanitized/escaped and never executed. External hyperlink text may remain visible as inert text, no navigation outside Nexora.

Export generated from an exact authorized saved version, .docx/.md chosen in dialog; no Reminder/private history/share tokens in file. If edit dirty, ask Save first or export current saved version with explicit label. Async job rechecks source read/export before execution and download. Images use internal approved file refs; pinned Resume uses its exact version. Archive may export owner-readable saved content, never mutate; source in Trash unavailable for ordinary export. Conversion failure keeps source unchanged and offers retry/report.

PDF/HTML user export proposals move Future/out of current baseline. Internal Block JSON/rendered HTML is implementation representation, not an enabled import/export format. Calendar .ics remains approved. Project/Task standalone import/export remains deferred.

## Career → internal Calendar

UX FX39-S04 becomes “Lịch hẹn liên kết” / Linked events. Entry Job detail → Add event: choose Create new or Link existing Personal Event. New opens full Calendar form with Title/Description/Start/End required; no provider meeting URL. Link existing lists own eligible Scheduled Personal Events, never Task projections or another User's Event. Calendar module/create or read capability required accordingly.

Calendar is sole owner of schedule, state and single reminder; Career owns only Job↔Event reference. Create+link idempotent; event+reference commit via declared local transaction/outbox strategy without orphan successful UI. A link operation does not create a second reminder or notify other participants. Notification goes to owner under existing three-channel rule.

Open linked event stays in Nexora Calendar/detail. To reschedule/cancel, use Calendar action/form and its current lifecycle; Completed/Canceled readonly, no reopen. Unlink removes reference, does not cancel/delete Event. Deleting Job does not delete Event; no event causes automatic pipeline stage changes. Calendar ICS handles linked Event as ordinary Personal Event, not a third source type.

No online interview room, conduct/score/feedback workflow, interviewer participant directory, video provider, external join link or invitation sending. Existing Job pipeline status text may still represent the owner's offline process, without adding interview execution. Legacy career.Interview data design/actions are historical proposal, replaced by CalendarLink contract for current scope.
