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
		[JsonProperty("BlogID")]
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

		[JsonProperty]
		public string Name;

		[JsonProperty]
		public string Description;

		[ForeignKey(typeof(User))]
		public int OwnerUserID;
	}
}

