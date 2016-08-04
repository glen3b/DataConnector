using System;
using System.Collections.Generic;
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
            if (_connection != null)
            {
                //_connection.Close();
                _connection.Dispose();
            }
        }

        private SqlConnection _connection;
		private SqlConnectionInformation _info;

		public override SqlConnectionInformation GetConnection()
        {
            if (_connection == null)
            {
                _connection = new SqlConnection(ConnectionString);
				_info = new SqlConnectionInformation (_connection, SqlConnectionBehavior.KeepOpen);
            }

			return _info;
        }
    }
}
