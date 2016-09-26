using System;

namespace DataConnector
{
	[AttributeUsage (AttributeTargets.Field | AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
	public sealed class ForeignKeyAttribute : Attribute
	{
		readonly Type targetTable;

		public Type ForeignType {
			get {
				return targetTable;
			}
		}

		public ForeignKeyAttribute (Type foreignType)
		{
			if (!typeof(IDataObject).IsAssignableFrom (foreignType)) {
				throw new ArgumentException ("The foreign type does not implement IDataObject.");
			}

			this.targetTable = foreignType;
		}
	}
}

