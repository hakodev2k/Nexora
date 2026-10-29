using Nexora.Application.Documents;
using Nexora.Application.Identity;
using Nexora.Infrastructure.Transfer;

namespace Nexora.Infrastructure.Documents;

public sealed partial class SqlDocumentService
{
    internal bool ImportedCreateAllowedWithin(SqlImportUnit unit,DocumentCreateCommand command)
        =>CurrentAuthority(unit.Connection,unit.Transaction,unit.Actor,"documents.format.import")&&CreateScopeAllowed(unit.Connection,unit.Transaction,unit.Actor,command);
    internal IdentityOperationResult<DocumentDetail> CreateImportedWithin(SqlImportUnit unit,DocumentCreateCommand command,string? trace)
    {
        if(!ImportedCreateAllowedWithin(unit,command))return ModuleUnavailable<DocumentDetail>();
        var result=CreatePageWithin(unit.Connection,unit.Transaction,unit.Actor,command,trace);
        return ImportedCreateAllowedWithin(unit,command)?result:ModuleUnavailable<DocumentDetail>();
    }
}
