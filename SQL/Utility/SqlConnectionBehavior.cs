using System;

namespace DataConnector
{
	[Flags]
	public enum SqlConnectionBehavior : uint
	{
		/// <summary>
		/// Specifies default behavior as documented in the other values in this enumeration.
		/// </summary>
		Default = 0,
		/// <summary>
		/// Specifies that the connection should be kept open after its usage.
		/// By default, connections are assumed to be allocated per function call.
		/// </summary>
		KeepOpen = 1
	}
}

