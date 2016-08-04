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

        public override SqlConnection GetConnection()
        {
            if (_connection == null)
            {
                _connection = new SqlConnection(ConnectionString);
                _connection.Open();
            }

            return _connection;
        }
    }
}
