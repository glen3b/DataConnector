using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataConnector
{
    /// <summary>
    /// Represents that a property or field that is read only due to its existance only when read from the backend.
    /// </summary>
    [System.AttributeUsage(System.AttributeTargets.Field | System.AttributeTargets.Property, Inherited = true, AllowMultiple = false)]
    public sealed class DataPropertyAttribute : Attribute
    {
        public DataPropertyAttribute()
        {
        }
    }
}
