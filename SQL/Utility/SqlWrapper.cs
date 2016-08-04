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
			var connInfo = GetConnection ();

			if (connInfo.Connection.State != ConnectionState.Open && connInfo.Connection.State != ConnectionState.Connecting) {
				connInfo.Connection.Open ();
			}

			SqlCommand command = new SqlCommand(procedureName, connInfo.Connection);
            command.CommandType = CommandType.StoredProcedure;
            command.Parameters.AddRange(parameters);
            SqlDataAdapter adapter = new SqlDataAdapter(command);
            DataTable table = new DataTable();
            adapter.Fill(table);
            
			if (!connInfo.Behavior.HasFlag (SqlConnectionBehavior.KeepOpen)) {
				connInfo.Connection.Close ();
			}

			return table;
        }

        public int RunNonQueryProcedure(string procedureName, params SqlParameter[] parameters)
        {
			var connInfo = GetConnection ();

			if (connInfo.Connection.State != ConnectionState.Open && connInfo.Connection.State != ConnectionState.Connecting) {
				connInfo.Connection.Open ();
			}

			SqlCommand command = new SqlCommand(procedureName, connInfo.Connection);
            command.CommandType = CommandType.StoredProcedure;
            command.Parameters.AddRange(parameters);
			var res = command.ExecuteNonQuery();

			if (!connInfo.Behavior.HasFlag (SqlConnectionBehavior.KeepOpen)) {
				connInfo.Connection.Close ();
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
