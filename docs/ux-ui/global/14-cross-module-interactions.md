# UX-14 — Cross-module Interaction Patterns

Projects → Tasks → Calendar:
- Project owns Task container lifecycle.
- Task owns Calendar projection.
- Calendar does not directly edit Task source.
- terminal Project locks Task mutation.
- Task Trash hides projection.

Documents → Files/Sharing/Search:
- Files owns attachment/cover objects.
- Sharing exposes approved read-only projection.
- local Documents search remains narrower than Global Search.

Finance → Files/Shopping/Assets:
- receipts attach through Files;
- links never auto-create finance transactions;
- Finance owns ledger.

Vault → integrations/digital assets:
- other modules keep VaultRef, not plaintext.

Career → Calendar is Proposed/Q-12.

Automation uses trusted registered actions and rechecks current authority before side effects.