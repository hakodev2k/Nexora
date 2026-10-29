# Reviewable proposal — Read Later Admin eligibility and capability query

Status: APPLIED AFTER EXPLICIT USER APPROVAL. The user answered “Cho phép đúng đề xuất này” to this exact published proposal on continuation. The earlier two automatic approval rejections remain historical evidence; they blocked application until that reply. The six-key eligibility, boolean endpoint and UI are now implemented. No grants or module/provider activation occurred.

Baseline d2e4eac / main8782f46. Main FX23 catalog lists these six SELF actions Admin checkbox Yes,gated/Resolved delegated; existing migration0011 registers their permission metadata. Proposed eligibility does not write Allow/Deny grants or enable modules. Current SQL owner/session/module/role/Deny/absent controls and SuperAdmin grant-assignment controls remain.

Exact eligible keys:

- reading.queue.read
- reading.item.save
- reading.item.remove
- reading.item.read
- reading.item.unread
- reading.item.position

Proposed production edits (no Support/News/Archive/provider/other-owner keys):

1. ActionGrantPolicy: add the closed set below to both CanGrantAllow eligibility checks and the IsApprovedForLocalAction / IsAdminGrantable predicates, exactly as existing Bookmark/Snippet sets. No permission/grant SQL mutation.

```csharp
private static readonly HashSet<string> ApprovedReadingActions = new(StringComparer.Ordinal)
{ "reading.queue.read", "reading.item.save", "reading.item.remove", "reading.item.read", "reading.item.unread", "reading.item.position" };
// Both deny predicates gain: && !ApprovedReadingActions.Contains(actionKey)
// Both positive predicates gain: || ApprovedReadingActions.Contains(actionKey)
```

2. IReadingService adds Capabilities(IdentityPrincipal) returning only IReadOnlyDictionary<string,bool>. SqlReadingService implements this read-only query:

```csharp
public IdentityOperationResult<IReadOnlyDictionary<string, bool>> Capabilities(IdentityPrincipal actor)
{
    using var connection = _connections.Create(); connection.Open();
    using var transaction = connection.BeginTransaction(IsolationLevel.Serializable);
    if (!SqlCurrentActor.IsLive(connection, transaction, actor))
        return Failure<IReadOnlyDictionary<string, bool>>("PermissionDenied", 403, "Reading capabilities are unavailable.");
    string[] actions = ["reading.queue.read", "reading.item.save", "reading.item.remove", "reading.item.read", "reading.item.unread", "reading.item.position"];
    var result = actions.ToDictionary(action => action, action => action == "reading.queue.read"
        ? _capabilities.IsAllowed(connection, transaction, actor, "FX23", action)
        : CurrentMutationAuthority(connection, transaction, actor, action), StringComparer.Ordinal);
    if (!SqlCurrentActor.IsLive(connection, transaction, actor))
        return Failure<IReadOnlyDictionary<string, bool>>("PermissionDenied", 403, "Reading capabilities are unavailable.");
    transaction.Commit();
    return IdentityOperationResult<IReadOnlyDictionary<string, bool>>.Success(result);
}
```

3. Authenticated GET /api/v1/read-later/capabilities maps the current cookie principal through this query, no owner/resource parameters or personal records. Client/UI consume six booleans before private queries; Save-only context can use authorized Bookmark picker while skipping queue read. Buttons intersect current capability, lifecycle and source availability. Every operation keeps server-side checks.

CurrentMutationAuthority keeps Bookmark-read prerequisite for save/read/unread/position; Remove checks only queue authority and owner ETag per existing main FX23-S03 / UX15 unavailable-source row and performs no source query. Actual progress changes also require position authority through trusted before/after diff and receipt flag.

Independent security_review checked the six-row source alignment, unchanged central SqlSelfCapability fingerprint and already-applied transaction/response fixes. This is source evidence, not runtime acceptance or host approval. If approved, apply this exact local scope on PR4, compile/review; no tests/fixtures/runtime in the current code-only run. No deployment, merge, production/provider or real-user grants.
