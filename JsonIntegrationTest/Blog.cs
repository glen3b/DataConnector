using System;
using DataConnector;
using Newtonsoft.Json;

namespace JsonIntegrationTest
{
	public class Blog : IDataObject
	{
		public Blog ()
		{
			IsStoredData = false;
		}

		[PrimaryKey]
		[StoredData(StoredName = "BlogID")]
		private int _id;

		public int ID {
			get{
				return _id;
			}
			protected set{
				_id = value;
			}
		}

		public bool IsStoredData { get; protected set; }

		[StoredData]
        public string Name { get; set; }

		[StoredData]
		public string Description;

        [StoredData]
		[ForeignKey(typeof(User))]
		public int OwnerUserID;
	}
}

