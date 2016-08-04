using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataConnector.SQL
{
	[AttributeUsage (AttributeTargets.Field, Inherited = false, AllowMultiple = false)]
	public sealed class SqlFieldAttribute : Attribute
	{
		// See the attribute guidelines at
		//  http://go.microsoft.com/fwlink/?LinkId=85236

		// MUST be the same in both the SQL parameter
		readonly string sqlFieldName;

		// This is a positional argument
		public SqlFieldAttribute (string sqlFieldName)
		{
			this.sqlFieldName = sqlFieldName;
		}

		public string SQLFieldName {
			get { return sqlFieldName; }
		}

		public DbType DataType { get; set; }
	}
}
