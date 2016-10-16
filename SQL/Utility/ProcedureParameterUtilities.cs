using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataConnector.SQL.Utility
{
    /// <summary>
    /// Utilities involving the <see cref="ProcedureParameter"/> structure. 
    /// </summary>
    public static class ProcedureParameterUtilities
    {
        /// <summary>
        /// Converts a <see cref="ProcedureParameter"/> to a structure with the same parameter name and value.
        /// </summary>
        /// <param name="param">The parameter to convert.</param>
        /// <returns>A <see cref="SqlParameter"/> with identical name and value to the given parameter.</returns>
        public static SqlParameter ToSqlParameter(this ProcedureParameter param)
        {
            return new SqlParameter(param.Name, param.Value);
        }

        /// <summary>
        /// Converts a <see cref="System.Data.Common.DbParameter"/> to a <see cref="ProcedureParameter"/> structure with the same parameter name and value.
        /// All other information (such as <see cref="System.Data.Common.DbParameter.DbType"/>) will be lost.
        /// </summary>
        /// <param name="param">The parameter to convert.</param>
        /// <returns>The converted parameter.</returns>
        public static ProcedureParameter FromDbParameter(this System.Data.Common.DbParameter param)
        {
            return new ProcedureParameter(param.ParameterName, param.Value);
        }

        /// <summary>
        /// Adds a set of <see cref="ProcedureParameter"/> instances to a <see cref="System.Data.SqlClient.SqlParameterCollection"/>.
        /// </summary>
        /// <param name="paramSet">The existing set of parameters.</param>
        /// <param name="inParams">The parameters to add to the collection.</param>
        public static void AddParameters(this System.Data.SqlClient.SqlParameterCollection paramSet, IEnumerable<ProcedureParameter> inParams)
        {
            foreach(var procParam in inParams)
            {
                paramSet.AddWithValue(procParam.Name, procParam.Value);
            }
        }
    }
}
