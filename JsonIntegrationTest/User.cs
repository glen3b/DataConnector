using System;
using DataConnector;
using Newtonsoft.Json;

namespace JsonIntegrationTest
{
	public class User : IDataObject
	{
		public User ()
		{
			IsStoredData = false;
		}

		[PrimaryKey]
		[JsonProperty ("UserID")]
		private int _id;

		public int ID {
			get {
				return _id;
			}
			protected set {
				_id = value;
			}
		}

		public Boolean IsStoredData { get; protected set; }

		[JsonProperty]
		public string Name;

		[JsonProperty]
		public string Username;

		[JsonProperty]
		public string PasswordHash;

		[JsonProperty]
		public DateTime BirthDate;
	}
}

