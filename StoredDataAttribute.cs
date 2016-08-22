using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataConnector
{
    /// <summary>
    /// Represents that a property or field is stored data.
    /// </summary>
    [System.AttributeUsage(System.AttributeTargets.Field | System.AttributeTargets.Property, Inherited = true, AllowMultiple = false)]
    public sealed class StoredDataAttribute : Attribute
    {
        /// <summary>
        /// Gets or sets the name of the field or column to use in the backend.
        /// If this is unset or null, the literal name of the member will be used.
        /// </summary>
        public string StoredName { get; set; }
        
        /// <summary>
        /// Gets or sets the data type to use to store the value in the backend.
        /// If this is unset or null, defaults will be used.
        /// Handling of this value is backend-dependent.
        /// </summary>
        public System.Data.DbType? DataType { get; set; }

        public StoredDataAttribute()
        {
        }
    }
}
