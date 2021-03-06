﻿using System;
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
    public class JsonDataBackend : IDataBackend
    {
        /// <summary>
        /// A delegate type that creates streams on demand. The streams should support both reading and writing if possible.
        /// </summary>
        /// <returns>A new stream.</returns>
        public delegate Stream StreamCreator();

        /// <summary>
        /// Gets the path of the backend file.
        /// </summary>
        /// <value>The backend file path.</value>
        [Obsolete("Please use the StreamCreator property instead.")]
        public string FilePath
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the delegate which is used to initialize streams for reading from and writing to the backend.
        /// If the returned streams do not support writing, it will be assumed that the backend is read only.
        /// </summary>
        public StreamCreator StreamInitializer { get; private set; }

        /// <summary>
        /// Gets a collection of assemblies from which loading of user types should be attempted.
        /// </summary>
        public ICollection<Assembly> AssembliesToLoad { get; private set; }

        protected readonly JsonSerializer Serializer;

        public JsonDataBackend(StreamCreator streamCreator)
        {
            if (streamCreator == null)
            {
                throw new ArgumentNullException(nameof(streamCreator));
            }

            StreamInitializer = streamCreator;

            Serializer = new JsonSerializer();
            Serializer.ContractResolver = new JsonDataObjectContractResolver();
            AssembliesToLoad = new HashSet<Assembly>(new Assembly[] {
                typeof(IDataObject).Assembly,
                typeof(JsonDataBackend).Assembly
            });
            // Serializer.Converters.Add (new DataObjectJsonConverter (this));
        }
        
        /// <summary>
        /// Creates a JSON data backend backed by a file.
        /// </summary>
        /// <param name="filePath">The path to the backend file.</param>
        public JsonDataBackend(string filePath) : this(() => File.Open(filePath, FileMode.OpenOrCreate))
        {
            if (filePath == null)
            {
                throw new ArgumentNullException(nameof(filePath));
            }

            // Still set the file path while the property exists - just remove this line when we kill the property
#pragma warning disable CS0618
            FilePath = filePath;
#pragma warning restore CS0618
        }

        //		class DataObjectJsonConverter : JsonConverter
        //		{
        //
        //			protected IDataBackend<IDataObject> Backend;
        //
        //			public DataObjectJsonConverter (IDataBackend<IDataObject> backend)
        //			{
        //				Backend = backend;
        //			}
        //
        //			public override bool CanConvert (Type objectType)
        //			{
        //				return typeof(IDataObject).IsAssignableFrom (objectType);
        //			}
        //
        //			public override object ReadJson (JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        //			{
        //				if (!typeof(IDataObject).IsAssignableFrom (objectType)) {
        //					throw new ArgumentException ("The given object is not an IDataObject");
        //				}
        //
        //				// We wrote an ID, allow an exception if something's not right
        //				int id = reader.ReadAsInt32 ().Value;
        //
        //				// TODO strongly type the name
        //				var method = Backend.GetType ().GetMethod ("GetObjectByID").MakeGenericMethod (objectType);
        //				return method.Invoke (Backend, new Object[]{ id });
        //			}
        //
        //			public override void WriteJson (JsonWriter writer, object value, JsonSerializer serializer)
        //			{
        //				if (value is IDataObject) {
        //					// Just write an ID
        //					writer.WriteValue (((IDataObject)value).ID);
        //				} else {
        //					throw new ArgumentException ("The given object is not an IDataObject");
        //				}
        //			}
        //		}

        /// <summary>
        /// The dictionary containing type-separated dictionaries of IDs to objects.
        /// This collection contains all objects.
        /// The string key is the type full name.
        /// </summary>
        protected IDictionary<string, IDictionary<int, IDataObject>> AllObjects = new Dictionary<string, IDictionary<int, IDataObject>>();

        public void SaveObject(IDataObject target)
        {
            IDictionary<int, IDataObject> objectTypeCollection;
            if (!AllObjects.TryGetValue(target.GetType().FullName, out objectTypeCollection))
            {
                objectTypeCollection = new Dictionary<int, IDataObject>();
                AllObjects[target.GetType().FullName] = objectTypeCollection;
            }

            try
            {
                AssembliesToLoad.Add(target.GetType().Assembly);
            }
            catch
            {
            }

            if (!target.IsStoredData)
            {
                // TODO document these requirements
                int newId = 1;
                // TODO a bit of a hack
                while (objectTypeCollection.ContainsKey(newId))
                {
                    newId++;
                }

                // TODO these requirements should be documented
                target.GetType().GetProperty("ID", BindingFlags.Public | BindingFlags.SetProperty | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.FlattenHierarchy).SetValue(target, newId);
                target.GetType().GetProperty("IsStoredData", BindingFlags.Public | BindingFlags.SetProperty | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.FlattenHierarchy).SetValue(target, true);
            }

            objectTypeCollection[target.ID] = target;

            SaveToBackend();
        }

        protected void SaveToBackend()
        {
            using (Stream stream = StreamInitializer())
            {
                if (!stream.CanWrite)
                {
                    throw new ReadOnlyException("This backend does not support writing.");
                }

                using (StreamWriter writer = new StreamWriter(stream))
                {
                    Serializer.Serialize(writer, AllObjects);
                }
            }
        }

        protected void LoadBackend()
        {
            using (Stream stream = StreamInitializer())
            {
                using (StreamReader srdr = new StreamReader(stream))
                {
                    using (JsonReader reader = new JsonTextReader(srdr))
                    {
                        var deserialized = Serializer.Deserialize<IDictionary<string, IDictionary<int, JObject>>>(reader);
                        var newAllDictionary = new Dictionary<string, IDictionary<int, IDataObject>>();
                        foreach (var typeToDictionaryPair in deserialized)
                        {
                            Type realType = null;
                            List<Exception> inner = new List<Exception>();

                            foreach (var assembly in AssembliesToLoad)
                            {
                                try
                                {
                                    if (realType == null)
                                    {
                                        realType = assembly.GetType(typeToDictionaryPair.Key, true);
                                    }
                                }
                                catch (Exception ex)
                                {
                                    inner.Add(ex);
                                }
                            }

                            if (realType == null)
                            {
                                throw new AggregateException("Could not find the type " + typeToDictionaryPair.Key, inner);
                            }

                            var idToObjectDict = new Dictionary<int, IDataObject>();
                            foreach (var idToObjectPair in typeToDictionaryPair.Value)
                            {
                                var obj = (IDataObject)idToObjectPair.Value.ToObject(realType);
                                obj.GetType().GetProperty(nameof(obj.IsStoredData), BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.SetProperty | BindingFlags.Instance | BindingFlags.FlattenHierarchy).SetValue(obj, true);
                                obj.GetType().GetProperty(nameof(obj.ID), BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.SetProperty | BindingFlags.Instance | BindingFlags.FlattenHierarchy).SetValue(obj, idToObjectPair.Key);
                                idToObjectDict.Add(idToObjectPair.Key, obj);
                            }

                            newAllDictionary.Add(typeToDictionaryPair.Key, idToObjectDict);
                        }
                        AllObjects = newAllDictionary;
                    }
                }
            }
        }

        public TObject GetObjectByID<TObject>(int id) where TObject : IDataObject
        {
            IDataObject result = null;
            AssembliesToLoad.Add(typeof(TObject).Assembly);
            try
            {
                result = AllObjects[typeof(TObject).FullName][id];
            }
            catch
            {
                // TODO fancy foreign keys (those that have a composite type as opposed to an ID) may cause a StackOverflowException here
                // TODO may return out of date objects if backend is concurrent (however we set the dictionary before we save)
                LoadBackend();

                try
                {
                    result = AllObjects[typeof(TObject).FullName][id];
                }
                catch (Exception ex)
                {
                    throw new IDNotFoundException(id, ex);
                }
            }

            return (TObject)result;
        }

        public IEnumerable<TObject> GetAllObjectsOfType<TObject>() where TObject : IDataObject
        {
            AssembliesToLoad.Add(typeof(TObject).Assembly);
            LoadBackend();
            if (!AllObjects.ContainsKey(typeof(TObject).FullName))
            {
                return Enumerable.Empty<TObject>();
            }
            return AllObjects[typeof(TObject).FullName].Values.Cast<TObject>();
        }

        public IEnumerable<TChildObject> GetChildrenOf<TParentObject, TChildObject>(int parentId)
            where TParentObject : IDataObject
            where TChildObject : IDataObject
        {
            AssembliesToLoad.Add(typeof(TParentObject).Assembly);
            AssembliesToLoad.Add(typeof(TChildObject).Assembly);
            // TODO this is intended to be able to use more efficient implementations
            // This is just a hack
            return GetAllObjectsOfType<TChildObject>().Where(c =>
            {
                foreach (var mem in c.GetType().GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
                {
                    ForeignKeyAttribute fkey;
                    if ((fkey = ((ForeignKeyAttribute)Attribute.GetCustomAttribute(mem, typeof(ForeignKeyAttribute)))) != null)
                    {
                        if (fkey.ForeignType == typeof(TParentObject))
                        {
                            var fkeyValue = mem.GetValue(c);
                            // Match foreign key relations
                            return parentId.Equals(fkeyValue);
                        }
                    }
                }

                // TODO don't duplicate this loop
                foreach (var mem in c.GetType().GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
                {
                    ForeignKeyAttribute fkey;
                    if ((fkey = ((ForeignKeyAttribute)Attribute.GetCustomAttribute(mem, typeof(ForeignKeyAttribute)))) != null)
                    {
                        if (fkey.ForeignType == typeof(TParentObject))
                        {
                            var fkeyValue = mem.GetValue(c);
                            // Match foreign key relations
                            return parentId.Equals(fkeyValue);
                        }
                    }
                }

                // No foreign key relation found
                // Do not include in return
                return false;
            });
        }

        public IEnumerable<TChildObject> GetChildrenOf<TParentObject, TChildObject>(TParentObject parent)
            where TParentObject : IDataObject
            where TChildObject : IDataObject
        {
            return GetChildrenOf<TParentObject, TChildObject>(parent.ID);
        }

        public void Dispose()
        {
            // TODO fix
            // this is a no-op
        }
    }
}

