using System.Data;
using Microsoft.Data.SqlClient;
using Nexora.Application.Identity;
using Nexora.Infrastructure.Persistence;

namespace Nexora.Infrastructure.Transfer;

// Explicit local transaction supplied to the source-owned participants; never part of an HTTP/Application DTO.
internal sealed class SqlImportUnit : IDisposable
{
    internal SqlConnection Connection { get; }
    internal SqlTransaction Transaction { get; }
    internal IdentityPrincipal Actor { get; }
    internal DateTime Now { get; }
    internal SqlImportUnit(SqlConnectionFactory connections, IdentityPrincipal actor, DateTime now)
    { Actor=actor;Now=now;Connection=connections.Create();Connection.Open();Transaction=Connection.BeginTransaction(IsolationLevel.Serializable); }
    internal SqlCommand Command(string sql)
    { var command=new SqlCommand(sql,Connection,Transaction);Add(command,"@Owner",SqlDbType.UniqueIdentifier,Actor.OwnerId);Add(command,"@User",SqlDbType.UniqueIdentifier,Actor.UserId);Add(command,"@Now",SqlDbType.DateTime2,Now);return command; }
    internal static void Add(SqlCommand command,string name,SqlDbType type,object? value,int size=0)
    { var parameter=size==0?command.Parameters.Add(name,type):command.Parameters.Add(name,type,size);parameter.Value=value??DBNull.Value; }
    internal static string ETag(byte[] value)=>"\""+Convert.ToBase64String(value)+"\"";
    public void Dispose(){Transaction.Dispose();Connection.Dispose();}
}
