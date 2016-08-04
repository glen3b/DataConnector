using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataConnector.SQL.Utility
{
    public abstract class SqlWrapper : IDisposable, ISqlWrapper
    {
        public SqlWrapper(string connString)
        {
            ConnectionString = connString;
        }
        
        protected string ConnectionString;

        public DataTable RunProcedure(string procedureName, params SqlParameter[] parameters)
        {
            SqlCommand command = new SqlCommand(procedureName, GetConnection());
            command.CommandType = CommandType.StoredProcedure;
            command.Parameters.AddRange(parameters);
            SqlDataAdapter adapter = new SqlDataAdapter(command);
            DataTable table = new DataTable();
            adapter.Fill(table);
            return table;
        }

        public int RunNonQueryProcedure(string procedureName, params SqlParameter[] parameters)
        {
            SqlCommand command = new SqlCommand(procedureName, GetConnection());
            command.CommandType = CommandType.StoredProcedure;
            command.Parameters.AddRange(parameters);
            return command.ExecuteNonQuery();
        }

        public abstract void Dispose();

        /// <summary>
        /// Returns an open SqlConnection for use by client code.
        /// </summary>
        /// <returns></returns>
        public abstract SqlConnection GetConnection();
    }
}
