# UX-09 — Notifications and feedback

Review 2026-09-07 · Baseline `b85f0f314da8ca7dcee8dad156e7b538ba52c287` · Documentation only; no schema, migrations or application code executed.

**MUST:** every approved Task/Calendar Reminder, Support/Emergency, Security/Account and ordinary Module/System notification creates **all three In-app, Email, BrowserPush attempts concurrently and independently**. This replaces earlier wording may create. No mute/quiet hours/channel selection settings. Enqueue does not guarantee delivery or human receipt; permission/provider failure truthful.

Logical notification states unread/read/deleted belong to Inbox; channel states queued/sending/accepted/failed/unavailable belong to dispatch. BrowserDenied/NoSubscription is unavailable, not delayed successful delivery. Email acceptance may still not mean inbox receipt. Support/Emergency immediate means durable intent/dispatch initiation at access event, not guaranteed zero network latency.

Feedback hierarchy: inline field validation for correction; inline alert for source/module/provider state; persistent access/lifecycle banner; toast for confirmed short success only. Save toast includes operation/version, never secret/body/token. Long operation uses queued/progress/report, not success immediately after202. Dangerous action result distinguishes moved to Trash from deleted permanently.

Inbox defaults All with Unread tab, newest first; open source/current detail; read/unread; mark all current notifications read cutoff; delete one/bulk preview. Retain until User deletes. Security Audit retention independent. Source unavailable message remains safe, no broken deep link trap. Browser permission prompt follows explicit Enable action with explanation and declined instructions. Screen readers use polite live regions for normal status, assertive only urgent blocking error; avoid repeating timer/notification counters on every repaint.
