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
		private string sqlFieldName;

        // This is a positional argument
        public SqlFieldAttribute()
        {
            this.sqlFieldName = null;
        }

        [Obsolete("SQLFieldName is now an optional parameter")]
        public SqlFieldAttribute (string sqlFieldName)
		{
			this.sqlFieldName = sqlFieldName;
		}

		public string SQLFieldName {
			get { return sqlFieldName; }
            set { sqlFieldName = value; }
		}

		public DbType DataType { get; set; }
	}
}
