using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataConnector.SQL.Utility
{
    public class ReferenceTrackingSqlWrapper : SqlWrapper
    {
        public ReferenceTrackingSqlWrapper(string connString) : base(connString) { }

        protected ICollection<WeakReference<SqlConnection>> OpenConnections = new List<WeakReference<SqlConnection>>();

        public override void Dispose()
        {
            foreach (var connection in OpenConnections)
            {
                SqlConnection realConn;
                if(connection.TryGetTarget(out realConn))
                {
                    try
                    {
                        realConn.Close();
                        realConn.Dispose();
                    }
                    catch { }
                }
            }
        }

        public override SqlConnection GetConnection()
        {
            SqlConnection newConn = new SqlConnection(ConnectionString);
            newConn.Open();
            WeakReference<SqlConnection> connRef = new WeakReference<SqlConnection>(newConn);
            OpenConnections.Add(connRef);

            return newConn;
        }
    }
}
