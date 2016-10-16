using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataConnector.SQL
{
    // TODO have this attribute just specify a table name (if direct-to-table) and use different annotations for CRU(D)
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, Inherited = false, AllowMultiple = false)]
    public sealed class SqlBackedClassAttribute : Attribute
    {
        // See the attribute guidelines at 
        //  http://go.microsoft.com/fwlink/?LinkId=85236
        readonly string updateProcedureName;
        readonly string getByIDProcedureName;
        readonly string insertProcedureName;

        // This is a positional argument
        public SqlBackedClassAttribute(string updateProcedureName, string getByIDProcedureName, string insertProcedureName)
        {
            this.updateProcedureName = updateProcedureName;
            this.getByIDProcedureName = getByIDProcedureName;
            this.insertProcedureName = insertProcedureName;
        }

        public string UpdateProcedureName
        {
            get { return updateProcedureName; }
        }

        public string GetByIdProcedureName
        {
            get
            {
                return getByIDProcedureName;
            }
        }

        public string InsertProcedureName { get { return insertProcedureName; } }
    }
}
