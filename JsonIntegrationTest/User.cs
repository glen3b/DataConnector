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
        [StoredData(StoredName = "UserID")]
        public int ID { get; protected set; }

		public Boolean IsStoredData { get; protected set; }

		[StoredData]
		public string Name;

		[StoredData]
		public string Username;

		[StoredData]
		public string PasswordHash;

		[StoredData]
		public DateTime BirthDate;
	}
}

