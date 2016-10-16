using System;
using System.Data;

namespace DataConnector.SQL.Utility
{
    public interface IDbWrapper : IDisposable
    {
        int RunNonQueryProcedure(string procedureName, params ProcedureParameter[] parameters);
        DataTable RunProcedure(string procedureName, params ProcedureParameter[] parameters);
    }
}