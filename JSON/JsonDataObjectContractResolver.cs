using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace DataConnector.JSON
{


    /// <summary>
    /// A contract resolver that selects fields in an opt-in mode using the backend-abstract <see cref="DataConnector.StoredDataAttribute"/> attribute.
    /// </summary>
    public class JsonDataObjectContractResolver : DefaultContractResolver
    {
        public JsonDataObjectContractResolver()
        {
        }

        protected override IList<JsonProperty> CreateProperties(Type type, MemberSerialization memberSerialization)
        {
            if (!typeof(IDataObject).IsAssignableFrom(type))
            {
                return base.CreateProperties(type, memberSerialization);
            }

            // TODO this is a bit hacky

            const BindingFlags searchFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.FlattenHierarchy;

            var props = (from MemberInfo mem in type.GetFields(searchFlags) select mem)
                .Union(from MemberInfo mem in type.GetProperties(searchFlags) select mem)
                .Where(mem => Attribute.GetCustomAttribute(mem, typeof(StoredDataAttribute)) != null)
                .Select(mem =>
                {
                    JsonProperty prop = CreateProperty(mem, memberSerialization);
                    string custName = (Attribute.GetCustomAttribute(mem, typeof(StoredDataAttribute)) as StoredDataAttribute)?.StoredName;
                    if (custName != null)
                    {
                        prop.PropertyName = custName;
                    }

                    prop.Readable = true;
                    prop.Writable = true;

                    return prop;
                }).ToList();

            return props;
        }
    }
}
