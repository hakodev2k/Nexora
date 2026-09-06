# UX-03 — Navigation & Wayfinding

Navigation levels: Global → Module → Resource → Contextual.

Breadcrumbs express hierarchy, e.g. `Projects / Nexora / Task` or `Documents / Architecture / API`.

Back behavior:
- browser Back remains meaningful;
- mobile detail returns to previous list with scroll/filter state;
- drawer close preserves selection;
- deep links work without prior navigation history.

Quick Create may expose Task, Personal Event, Document, Bookmark and Time entry only when enabled/permitted. It never bypasses required fields or immutable choices.

External links: validated HTTP(S), safe new tab, never auto-navigate untrusted preview/QR URLs.