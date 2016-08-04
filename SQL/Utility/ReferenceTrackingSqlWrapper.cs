using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataConnector.SQL.Utility
{
	[Obsolete("MSDN documentation states that a SqlConnection that has been garbage collected will not automatically close, and in addition changes to the API render this class useless as client code closes its own connections.")]
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

		public override SqlConnectionInformation GetConnection()
        {
            SqlConnection newConn = new SqlConnection(ConnectionString);
            WeakReference<SqlConnection> connRef = new WeakReference<SqlConnection>(newConn);
            OpenConnections.Add(connRef);

			return new SqlConnectionInformation(newConn, SqlConnectionBehavior.Default);
        }
    }
}
