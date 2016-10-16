using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataConnector.SQL
{
    /// <summary>
    /// Represents an immutable parameter to a stored procedure.
    /// </summary>
    public struct ProcedureParameter
    {
        /// <summary>
        /// Gets the name of the parameter in the database.
        /// </summary>
        public string Name { get; private set; }
        /// <summary>
        /// Gets the value of the parameter to be passed.
        /// </summary>
        public object Value { get; private set; }

        /// <summary>
        /// Creates a procedure parameter with a given name and value.
        /// The name may not be null. If the value passed is null, the property will be set to <see cref="DBNull.Value"/>.
        /// </summary>
        /// <param name="name">The name of the parameter.</param>
        /// <param name="value">The value of the parameter.</param>
        public ProcedureParameter(string name, object value)
        {
            if(name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            Name = name;
            Value = value ?? DBNull.Value;
        }
    }
}
