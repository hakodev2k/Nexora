# UX-07 — Search, Favorites & Command Palette

`Ctrl/Cmd+K` is the global search/command entry.

Results are grouped by module/type and show only safe metadata. Vault payload never appears in preview.

Module-local search remains visibly scoped, e.g. “Search in Documents”; it does not silently widen to global.

Command palette may expose navigation and safe creation. Destructive commands still open normal confirmation/preview.

Favorites are navigation shortcuts only; they do not copy, move, share or change ownership.