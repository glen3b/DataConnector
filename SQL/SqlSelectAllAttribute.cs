using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataConnector.SQL
{
    [System.AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, Inherited = false, AllowMultiple = false)]
    sealed class SqlSelectAllAttribute : Attribute
    {
        // See the attribute guidelines at 
        //  http://go.microsoft.com/fwlink/?LinkId=85236
        readonly string selectAllProcedure;

        // This is a positional argument
        public SqlSelectAllAttribute(string selectAllProcedure)
        {
            this.selectAllProcedure = selectAllProcedure;
        }

        public string SelectAllProcedureName
        {
            get { return selectAllProcedure; }
        }
    }
}
