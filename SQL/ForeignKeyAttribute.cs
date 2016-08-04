using System;

namespace DataConnector.SQL
{
	[AttributeUsage (AttributeTargets.Field, Inherited = false, AllowMultiple = false)]
	public sealed class ForeignKeyAttribute : Attribute
	{
		readonly Type targetTable;

		public Type TargetTable {
			get {
				return targetTable;
			}
		}

		public ForeignKeyAttribute (Type targetTable)
		{
			if (Attribute.GetCustomAttribute (targetTable, typeof(SqlBackedClassAttribute)) == null) {
				throw new ArgumentException ("The foreign table type must have a SqlBackedClassAttribute.");
			}
			if (Attribute.GetCustomAttribute (targetTable, typeof(OneToManyRelationshipAttribute)) == null) {
				// TODO potentially other relationships as needed
				throw new ArgumentException ("The table type must have a OneToManyAttribute.");
			}
			this.targetTable = targetTable;
		}
	}
}

