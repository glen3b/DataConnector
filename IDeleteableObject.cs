using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataConnector
{
    public interface IDeleteableObject
    {
        bool IsDeleted { get; set; }
    }
}
