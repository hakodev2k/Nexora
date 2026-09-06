# UX-09 — Notifications & Feedback

Notification Center:
- Unread/All;
- source filter where useful;
- chronological list;
- source deep link;
- mark read/unread;
- mark all read;
- delete where supported;
- keyboard traversal on desktop.

One logical notification may create In-app, Email and Browser Push attempts independently. UI does not claim Delivered when only enqueue/attempt is known.

Browser permission denial and provider failure are truthful delivery states.

Use inline validation for field problems, toast for completed transient actions, persistent banner when action is still required, and safe correlation ID for unexpected errors.