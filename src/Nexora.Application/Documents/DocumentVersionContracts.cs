using Nexora.Application.Identity;

namespace Nexora.Application.Documents;

public sealed record DocumentVersionComparison(DocumentVersionPreview Before, DocumentVersionPreview After);
public sealed record DocumentRestoreVersionCommand(long VersionNumber, DocumentTagChange? Tag = null, string? ChangeNote = null);

public interface IDocumentVersionService
{
    IdentityOperationResult<DocumentVersionComparison> Compare(IdentityPrincipal actor, Guid pageId, long before, long after);
    IdentityOperationResult<DocumentDetail> RestoreVersion(IdentityPrincipal actor, Guid pageId, string? ifMatch,
        DocumentRestoreVersionCommand command, string? idempotencyKey = null, string? traceId = null);
}
