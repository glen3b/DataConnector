using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataConnector
{
	[AttributeUsage (AttributeTargets.Field, Inherited = false, AllowMultiple = false)]
	public sealed class PrimaryKeyAttribute : Attribute
	{
		public PrimaryKeyAttribute ()
		{
		}
	}
}
