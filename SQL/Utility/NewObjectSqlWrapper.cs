using System;
using DataConnector.SQL.Utility;
using System.Data.SqlClient;

namespace DataConnector
{
	/// <summary>
	/// A SQL wrapper that creates new connections for each call.
	/// Expected to be used in conjunction with connection pooling as documented at https://msdn.microsoft.com/en-us/library/8xx3tyca(v=vs.110).aspx
	/// </summary>
	public class NewObjectSqlWrapper : SqlWrapper, IDisposable
	{
		public NewObjectSqlWrapper (string connectionString) : base (connectionString)
		{
		}

		public override SqlConnectionInformation GetConnection ()
		{
			// Intended to utilize connection pooling
			return new SqlConnectionInformation(new SqlConnection(ConnectionString), SqlConnectionBehavior.Default);
		}

		/// <summary>
		/// Releases all resource used by the <see cref="DataConnector.NewObjectSqlWrapper"/> object.
		/// For this implementation of <seealso cref="DataConnector.SqlWrapper"/>, no resources are held or released.
		/// </summary>
		/// <remarks>Call <see cref="Dispose"/> when you are finished using the <see cref="DataConnector.NewObjectSqlWrapper"/>. The
		/// <see cref="Dispose"/> method may leave the <see cref="DataConnector.NewObjectSqlWrapper"/> in an unusable state.
		/// After calling <see cref="Dispose"/>, you must release all references to the
		/// <see cref="DataConnector.NewObjectSqlWrapper"/> so the garbage collector can reclaim the memory that the
		/// <see cref="DataConnector.NewObjectSqlWrapper"/> was occupying.</remarks>
		public override void Dispose ()
		{

		}
	}
}

