using System;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System.Collections.Generic;
using System.Reflection;
using Newtonsoft.Json.Linq;
using System.IO;
using System.Data;

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

		protected readonly JsonSerializer Serializer;

		public JsonDataBackend (string filePath)
		{
			if (filePath == null) {
				throw new ArgumentNullException ("filePath");
			}

			FilePath = filePath;

			Serializer = new JsonSerializer ();
			Serializer.ContractResolver = new JsonDataObjectContractResolver (this);
			Serializer.Converters.Add (new DataObjectJsonConverter (this));
		}

		/// <summary>
		/// A contract resolver that selects fields in an opt-in mode.
		/// </summary>
		class JsonDataObjectContractResolver : DefaultContractResolver
		{
			public JsonDataObjectContractResolver (IDataBackend<IDataObject> backend)
			{
				Backend = backend;
			}

			IDataBackend<IDataObject> Backend;

			protected override IList<JsonProperty> CreateProperties (Type type, MemberSerialization memberSerialization)
			{
				// TODO this is a bit hacky

				var props = base.CreateProperties (type, MemberSerialization.Fields);

				foreach (var p in props) {
					p.Writable = true;
					p.Readable = true;

					if (typeof(IDataObject).IsAssignableFrom (p.PropertyType)) {
						// TODO ensure this doesn't call excessively recursively
						p.Converter = new DataObjectJsonConverter (Backend);
					}
				}

				return props;
			}
		}

		class DataObjectJsonConverter : JsonConverter
		{

			protected IDataBackend<IDataObject> Backend;

			public DataObjectJsonConverter (IDataBackend<IDataObject> backend)
			{
				Backend = backend;
			}

			public override bool CanConvert (Type objectType)
			{
				return typeof(IDataObject).IsAssignableFrom (objectType);
			}

			public override object ReadJson (JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
			{
				if (!typeof(IDataObject).IsAssignableFrom (objectType)) {
					throw new ArgumentException ("The given object is not an IDataObject");
				}

				// We wrote an ID, allow an exception if something's not right
				int id = reader.ReadAsInt32 ().Value;

				// TODO strongly type the name
				var method = Backend.GetType ().GetMethod ("GetObjectByID").MakeGenericMethod (objectType);
				return method.Invoke (Backend, new Object[]{ id });
			}

			public override void WriteJson (JsonWriter writer, object value, JsonSerializer serializer)
			{
				if (value is IDataObject) {
					// Just write an ID
					writer.WriteValue (((IDataObject)value).ID);
				} else {
					throw new ArgumentException ("The given object is not an IDataObject");
				}
			}
		}

		/// <summary>
		/// The dictionary containing type-separated dictionaries of IDs to objects.
		/// This collection contains all objects.
		/// The string key is the type full name.
		/// </summary>
		protected IDictionary<string, IDictionary<int, IDataObject>> AllObjects = new Dictionary<string, IDictionary<int, IDataObject>> ();

		public void SaveObject (IDataObject target)
		{
			IDictionary<int, IDataObject> objectTypeCollection;
			if (!AllObjects.TryGetValue (target.GetType ().FullName, out objectTypeCollection)) {
				objectTypeCollection = new Dictionary<int, IDataObject> ();
				AllObjects [target.GetType ().FullName] = objectTypeCollection;
			}

			if (!target.IsStoredData) {
				// TODO document these requirements
				int newId = 1;
				// TODO a bit of a hack
				while (objectTypeCollection.ContainsKey (newId)) {
					newId++;
				}

				// TODO these requirements should be documented
				target.GetType ().GetProperty ("ID", BindingFlags.Public | BindingFlags.SetProperty | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.FlattenHierarchy).SetValue (target, newId);
				target.GetType ().GetProperty ("IsStoredData", BindingFlags.Public | BindingFlags.SetProperty | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.FlattenHierarchy).SetValue (target, true);
			}

			objectTypeCollection [target.ID] = target;

			SaveToBackend ();
		}

		protected void SaveToBackend ()
		{
			using (FileStream stream = File.Open (FilePath, FileMode.Create)) {
				using (StreamWriter writer = new StreamWriter (stream)) {
					Serializer.Serialize (writer, AllObjects);
				}
			}
		}

		protected void LoadBackend ()
		{
			using (FileStream stream = File.Open (FilePath, FileMode.Open)) {
				using (StreamReader srdr = new StreamReader (stream)) {
					using (JsonReader reader = new JsonTextReader (srdr)) {
						AllObjects = Serializer.Deserialize<IDictionary<string, IDictionary<int, IDataObject>>> (reader);
					}
				}
			}
		}

		public TObject GetObjectByID<TObject> (int id) where TObject : IDataObject
		{
			IDataObject result = null;
			try {
				result = AllObjects [typeof(TObject).FullName] [id];
			} catch {
				// TODO fancy foreign keys (those that have a composite type as opposed to an ID) may cause a StackOverflowException here
				// TODO may return out of date objects if backend is concurrent (however we set the dictionary before we save)
				LoadBackend ();

				try {
					result = AllObjects [typeof(TObject).FullName] [id];
				} catch (Exception ex){
					throw new RowNotInTableException ("The given ID does not correspond to a record.", ex);
				}
			}

			return (TObject)result;
		}

		public IEnumerable<TObject> GetAllObjectsOfType<TObject> () where TObject : IDataObject
		{
			return AllObjects [typeof(TObject).FullName].Values.Cast<TObject> ();
		}

		public IEnumerable<TChildObject> GetChildrenOf<TParentObject, TChildObject> (TParentObject parent)
			where TParentObject : IDataObject
			where TChildObject : IDataObject
		{
			// TODO this is intended to be able to use more efficient implementations
			// This is just a hack
			return GetAllObjectsOfType<TChildObject> ().Where (c => {
				foreach (var mem in c.GetType().GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)) {
					// TODO move this to a different namespace
					DataConnector.SQL.ForeignKeyAttribute fkey;
					if ((fkey = ((DataConnector.SQL.ForeignKeyAttribute)Attribute.GetCustomAttribute (mem, typeof(DataConnector.SQL.ForeignKeyAttribute)))) != null) {
						if (fkey.TargetTable == typeof(TParentObject)) {
							var fkeyValue = mem.GetValue (c);
							// Match foreign key relations
							return parent.Equals (fkeyValue) || parent.ID.Equals (fkeyValue);
						}
					}
				}

				// No foreign key relation found
				// Do not include in return
				return false;
			});
		}

		public void Dispose ()
		{
			// TODO fix
			// this is a no-op
		}
	}
}

