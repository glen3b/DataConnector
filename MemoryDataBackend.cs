using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace DataConnector
{
    public class MemoryDataBackend : IDataBackend
    {
        public void Dispose()
        {
            throw new NotImplementedException();
        }

        internal class LazyDictionary<TKey, TValue> : IDictionary<TKey, TValue>
        {
            private Dictionary<TKey, TValue> _underlying = new Dictionary<TKey, TValue>();

            protected internal Func<TKey, TValue> CreateDefaultValue;

            public TValue this[TKey key]
            {
                get
                {
                    TValue val;
                    if (!_underlying.TryGetValue(key, out val))
                    {
                        _underlying[key] = (val = CreateDefaultValue(key));
                    }

                    return val;
                }

                set
                {
                    _underlying[key] = value;
                }
            }

            public int Count
            {
                get
                {
                    return _underlying.Count;
                }
            }

            public bool IsReadOnly
            {
                get
                {
                    return false;
                }
            }

            public ICollection<TKey> Keys
            {
                get
                {
                    return _underlying.Keys;
                }
            }

            public ICollection<TValue> Values
            {
                get
                {
                    return _underlying.Values;
                }
            }

            public void Add(KeyValuePair<TKey, TValue> item)
            {
                Add(item.Key, item.Value);
            }

            public void Add(TKey key, TValue value)
            {
                _underlying.Add(key, value);
            }

            public void Clear()
            {
                _underlying.Clear();
            }

            public bool Contains(KeyValuePair<TKey, TValue> item)
            {
                return _underlying.ContainsKey(item.Key) && _underlying.ContainsValue(item.Value);
            }

            public bool ContainsKey(TKey key)
            {
                return _underlying.ContainsKey(key);
            }

            public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
            {
                (_underlying as IDictionary<TKey, TValue>).CopyTo(array, arrayIndex);
            }

            public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
            {
                return _underlying.GetEnumerator();
            }

            public bool Remove(KeyValuePair<TKey, TValue> item)
            {
                return (_underlying as IDictionary<TKey, TValue>).Remove(item);
            }

            public bool Remove(TKey key)
            {
                return _underlying.Remove(key);
            }

            public bool TryGetValue(TKey key, out TValue value)
            {
                // Indexer is lazy
                value = this[key];
                return true;
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }
        }

        protected IDictionary<Type, IDictionary<int, IDataObject>> _keyedDataObjectsByType = new
            LazyDictionary<Type, IDictionary<int, IDataObject>>()
        {
            CreateDefaultValue = (k) => new Dictionary<int, IDataObject>()
        };

        public IEnumerable<TObject> GetAllObjectsOfType<TObject>() where TObject : IDataObject
        {
            return _keyedDataObjectsByType[typeof(TObject)].Values.Cast<TObject>();
        }

        public IEnumerable<TChildObject> GetChildrenOf<TParentObject, TChildObject>(int parentId)
            where TParentObject : IDataObject
            where TChildObject : IDataObject
        {

            var foreignKey = typeof(TChildObject)
            .GetMembers().Where(m => m is PropertyInfo || m is FieldInfo)
            .Where(m => m.GetCustomAttribute<StoredDataAttribute>() != null)
            .Where(m => m.GetCustomAttribute<ForeignKeyAttribute>()?.ForeignType == typeof(TParentObject))
            .SingleOrDefault();

            if (foreignKey == null)
            {
                throw new ArgumentException("The given relationship does not exist.");
            }

            Func<TChildObject, int> getRelationId = null;
            if (foreignKey is FieldInfo)
            {
                getRelationId = (ch) => (int)(foreignKey as FieldInfo).GetValue(ch);
            }
            else if (foreignKey is PropertyInfo)
            {
                getRelationId = (ch) => (int)(foreignKey as PropertyInfo).GetValue(ch);
            }

            return _keyedDataObjectsByType[typeof(TChildObject)].Values.Cast<TChildObject>().Where(child => getRelationId(child) == parentId);
        }

        public IEnumerable<TChildObject> GetChildrenOf<TParentObject, TChildObject>(TParentObject parent)
            where TParentObject : IDataObject
            where TChildObject : IDataObject
        {
            return GetChildrenOf<TParentObject, TChildObject>(parent.ID);
        }

        public TObject GetObjectByID<TObject>(int id) where TObject : IDataObject
        {
            return _keyedDataObjectsByType[typeof(TObject)].Cast<TObject>().Single(o => o.ID == id);
        }

        public void SaveObject(IDataObject target)
        {
            if (target.IsStoredData)
            {
                _keyedDataObjectsByType[target.GetType()][target.ID] = target;
            }
            else
            {
                var storedData = _keyedDataObjectsByType[target.GetType()];
                int nId = Enumerable.Range(1, int.MaxValue).First(id => !storedData.Keys.Contains(id));
                storedData[nId] = target;
                var type = target.GetType();
                type.GetProperty(nameof(IDataObject.ID)).SetValue(target, nId);
                type.GetProperty(nameof(IDataObject.IsStoredData)).SetValue(target, true);
            }
        }
    }
}
