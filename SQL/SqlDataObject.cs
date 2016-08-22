using DataConnector.SQL.Utility;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace DataConnector.SQL
{
    [Obsolete("Please implement IDataObject directly.")]
    public abstract class SqlDataObject : IDataObject
    {
        public bool IsStoredData { get { return IsSQLBacked; }
            protected set
            {
                IsSQLBacked = value;
            }
        }

        protected bool IsSQLBacked;
        // All SQL objects should have some form of ID
        public abstract int ID { get; protected set; }
    }
}
