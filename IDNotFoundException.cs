using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataConnector
{
    /// <summary>
    /// Represents the exception that is thrown when an ID is not found in a data backend.
    /// </summary>
    public class IDNotFoundException : System.Data.DataException
    {
        public IDNotFoundException(string message, Exception inner) : base(message, inner) { }

        public IDNotFoundException(string message) : this(message, null) { }

        public IDNotFoundException(int id) : this(id, null) { }

        public IDNotFoundException(int id, Exception inner) : base("The record with ID '" + id + "' was not found.", inner) { }
    }
}
