using Microsoft.Data.SqlClient;

namespace Nexora.Infrastructure.Persistence;

// Explicit infrastructure transaction contract; participants query only their owned retention ledgers.
public interface IFileRetentionParticipant
{
    bool IsRetained(SqlConnection connection,SqlTransaction transaction,Guid ownerId,Guid fileId);
}
