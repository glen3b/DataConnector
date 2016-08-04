using System;
using System.Data;
using System.Data.SqlClient;

namespace DataConnector.SQL.Utility
{
    public interface ISqlWrapper : IDisposable
    {
        int RunNonQueryProcedure(string procedureName, params SqlParameter[] parameters);
        DataTable RunProcedure(string procedureName, params SqlParameter[] parameters);
    }
}