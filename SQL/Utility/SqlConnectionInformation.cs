using System;
using System.Data.SqlClient;

namespace DataConnector
{
	/// <summary>
	/// A structure containing a SqlConnection and information about the proper behavior of client code in usage of that connection.
	/// </summary>
	public struct SqlConnectionInformation
	{
		/// <summary>
		/// Gets the appropriate behavior to use when dealing with this <see cref="System.Data.SqlClient.SqlConnection"/>.
		/// </summary>
		/// <value>The behavior to use in respect to the SqlConnection.</value>
		public SqlConnectionBehavior Behavior  {
			get;
			private set;
		}

		/// <summary>
		/// Gets the <see cref="System.Data.SqlClient.SqlConnection"/> referred to by this information.
		/// </summary>
		/// <value>The connection about which information is being passed.</value>
		public SqlConnection Connection  {
			get;
			private set;
		}

		public SqlConnectionInformation(SqlConnection connection, SqlConnectionBehavior behavior){
			if (connection == null) {
				throw new ArgumentNullException ("connection");
			}

			Connection = connection;
			Behavior = behavior;
		}
	}
}

