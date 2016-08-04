using System;

namespace DataConnector.SQL
{
	[AttributeUsage (AttributeTargets.Class | AttributeTargets.Struct, Inherited = false, AllowMultiple = false)]
	public sealed class OneToManyRelationshipAttribute : Attribute
	{
		readonly Type childType;

		/// <summary>
		/// Gets the type of the data stored in the table with a foreign key relationship to this table.
		/// </summary>
		public Type ChildType{
			get{
				return childType;
			}
		}

		/// <summary>
		/// Gets or sets the name of the SQL procedure to call to retrieve the child elements of this type.
		/// If the procedure name is null it is assumed to not exist.
		/// The procedure is expected to take one parameter: an identifier with the parameter name equivalent to the
		/// name of the column of the foreign key in the child type referring to the type annotated with this object.
		/// This identifier is expected to correspond to an instance of the type which this attribute annotates.
		/// The procedure is expected to return all columns of the table storing the child type, and it is also expected to return all rows
		/// where the foreign key identifier is equal to the identifier passed as the parameter to the procedure.
		/// </summary>
		public string GetChildrenProcedure { get; set; }

		public OneToManyRelationshipAttribute (Type childType)
		{
			this.childType = childType;
		}
	}
}

