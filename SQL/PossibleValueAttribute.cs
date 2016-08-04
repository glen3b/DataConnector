using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataConnector.SQL
{
    /// <summary>
    /// An attribute denoting a possible value of a type. Intended as a hardcoded alternative to a SqlSelectAllAttribute.
    /// </summary>
    [System.AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, Inherited = false, AllowMultiple = true)]
    public sealed class PossibleValueAttribute : Attribute
    {
        // See the attribute guidelines at 
        //  http://go.microsoft.com/fwlink/?LinkId=85236
        readonly int id;

        // This is a positional argument
        public PossibleValueAttribute(int id)
        {
            this.id = id;
        }

        public int ID
        {
            get { return id; }
        }
    }
}
