using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataConnector.SQL.Utility
{
    public abstract class SqlWrapper : IDisposable, IDbWrapper
    {
        public SqlWrapper(string connString)
        {
            ConnectionString = connString;
        }

        protected string ConnectionString;

        public DataTable RunProcedure(string procedureName, params ProcedureParameter[] parameters)
        {
            var connInfo = GetConnection();

            if (!connInfo.Connection.State.HasFlag(ConnectionState.Open))
            {
                connInfo.Connection.Open();
            }

            SqlCommand command = new SqlCommand(procedureName, connInfo.Connection);
            command.CommandType = CommandType.StoredProcedure;
            command.Parameters.AddParameters(parameters);
            SqlDataAdapter adapter = new SqlDataAdapter(command);
            DataTable table = new DataTable();
            adapter.Fill(table);

            if (!connInfo.Behavior.HasFlag(SqlConnectionBehavior.KeepOpen))
            {
                connInfo.Connection.Close();
            }

            return table;
        }

        public int RunNonQueryProcedure(string procedureName, params ProcedureParameter[] parameters)
        {
            var connInfo = GetConnection();

            if (!connInfo.Connection.State.HasFlag(ConnectionState.Open))
            {
                connInfo.Connection.Open();
            }

            SqlCommand command = new SqlCommand(procedureName, connInfo.Connection);
            command.CommandType = CommandType.StoredProcedure;
            command.Parameters.AddParameters(parameters);
            var res = command.ExecuteNonQuery();

            if (!connInfo.Behavior.HasFlag(SqlConnectionBehavior.KeepOpen))
            {
                connInfo.Connection.Close();
            }

            return res;
        }

        public abstract void Dispose();

        /// <summary>
		/// Returns a <see cref="SqlConnectionInformation"/> containing a <see cref="SqlConnection"/> for use by client code.
		/// It is not defined by this abstract class whether the given connection will be open or closed, but it must be non-null.
		/// All client code is expected to obey the given <see cref="SqlConnectionBehavior"/>.
        /// </summary>
		/// <returns>SqlConnectionInformation</returns>
		public abstract SqlConnectionInformation GetConnection();
    }
}
