# Rules: Completion Claim Policy

- Every high-confidence completion claim **MUST** map to recorded, fresh, relevant tool evidence.
- The agent **MUST NOT** claim a command ran unless execution is present in the evidence ledger.
- The agent **MUST NOT** claim tests/builds passed when the recorded execution failed, targeted the wrong scope, or became stale after later edits.
- `verified` **MUST** require evidence covering the task acceptance criteria, not merely a green pre-existing test suite.
- Canonical repository verification commands **SHOULD** be registered explicitly; if unknown, discovery and uncertainty **MUST** be stated.
- Relevant edits after verification **MUST** invalidate affected evidence.
- Missing evidence **MUST** downgrade status rather than be filled by inference.
- Failed evidence **MUST NOT** be removed from the ledger to obtain a green result.
- High-impact changes **MUST** receive verification by an agent/reviewer other than the implementer when policy requires it.
- Retry loops **MUST** be bounded; after two failures of the same hypothesis, the workflow **MUST** re-diagnose.
- User-facing status **MUST** distinguish Implemented, Measured, and Verified.
- In Nexora, follow the current DEC-014 execution amendment and `.ai/verification.md`:
  implementation completion, automated QA verification, runtime/security evidence
  and human UI acceptance are separate. Only explicit human confirmation may set
  HUMAN UI REVIEW ACCEPTED; pending mandatory human/independent/D09 gates prohibit
  full RELEASE READY. A policy update never retroactively supplies test evidence.
- The system **SHOULD** retain non-sensitive command, scope, result, timestamp, and state fingerprint needed to audit claims.
- Hidden chain-of-thought **MUST NOT** be requested or stored as verification evidence.
