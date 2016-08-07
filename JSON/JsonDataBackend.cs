using System;
using Newtonsoft.Json;

namespace DataConnector.JSON
{
	/// <summary>
	/// Uses Json.NET as a data storage backend. Target objects are expected to have the appropriate attributes.
	/// </summary>
	public class JsonDataBackend : IDataBackend<IDataObject>
	{
		/// <summary>
		/// Gets the path of the backend file.
		/// </summary>
		/// <value>The backend file path.</value>
		public string FilePath {
			get;
			private set;
		}

		public JsonDataBackend (string filePath)
		{
			if (filePath == null) {
				throw new ArgumentNullException ("filePath");
			}

			FilePath = filePath;
		}
	}
}

