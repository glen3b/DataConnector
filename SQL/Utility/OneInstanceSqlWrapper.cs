using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataConnector.SQL.Utility
{
    public sealed class OneInstanceSqlWrapper : SqlWrapper
    {
        public OneInstanceSqlWrapper(string connString) : base(connString) { }

        public override void Dispose()
        {
            try
            {
                if (_connection != null)
                {
                    if (_connection.State.HasFlag(ConnectionState.Open))
                    {
                        _connection.Close();
                    }
                    _connection.Dispose();
                }
            }
            catch (Exception ex)
            {
                throw new ObjectDisposedException("Error disposing the connection of a " + nameof(OneInstanceSqlWrapper) + ".", ex);
            }
        }

        private SqlConnection _connection;
        private SqlConnectionInformation _info;

        public override SqlConnectionInformation GetConnection()
        {
            if (_connection == null)
            {
                _connection = new SqlConnection(ConnectionString);
                _info = new SqlConnectionInformation(_connection, SqlConnectionBehavior.KeepOpen);
            }

            return _info;
        }
    }
}
