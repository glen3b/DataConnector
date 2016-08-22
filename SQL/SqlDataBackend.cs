using DataConnector.SQL.Utility;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace DataConnector.SQL
{
    public class SqlDataBackend : IDataBackend
    {
        private ISqlWrapper _database;

        /// <summary>
        /// Gets the database wrapper in use by this data backend.
        /// </summary>
        public ISqlWrapper Database
        {
            get
            {
                return _database;
            }
        }

        /// <summary>
        /// The binding flags which are used to find data storage members.
        /// </summary>
        public static readonly BindingFlags SearchBindingFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.FlattenHierarchy;

        public SqlDataBackend(ISqlWrapper databaseWrapper)
        {
            if (databaseWrapper == null)
            {
                throw new ArgumentNullException("databaseWrapper");
            }
            _database = databaseWrapper;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DataConnector.SQL.SqlDataBackend"/> class, using a backend wrapper of type <see cref="DataConnector.SQL.Utility.NewObjectSqlWrapper"/>.
        /// </summary>
        /// <param name="connectionString">The connection string to initialize the backend wrapper with.</param>
        public SqlDataBackend(string connectionString) : this(new NewObjectSqlWrapper(connectionString))
        {

        }


        public IEnumerable<TChildObject> GetChildrenOf<TParentObject, TChildObject>(TParentObject parent)
            where TParentObject : IDataObject
            where TChildObject : IDataObject
        {
            if (parent == null)
            {
                throw new ArgumentNullException("parent");
            }

            SqlRelationshipOneToManyAttribute parentAttribute = Attribute.GetCustomAttribute(typeof(TParentObject), typeof(SqlRelationshipOneToManyAttribute)) as SqlRelationshipOneToManyAttribute;
            if (parentAttribute == null)
            {
                // Per interface spec
                throw new NotSupportedException("The given parent type does not have a child type.");
            }

            if (parentAttribute.GetChildrenProcedure == null)
            {
                throw new NotSupportedException("The given parent type does not support explicitly retrieving children.");
            }

            string foreignKeyName = null;
            GetFieldsAndProperties(typeof(TChildObject), (field, getter) =>
            {
                if (foreignKeyName != null)
                {
                    // Already found it
                    return;
                }

                ForeignKeyAttribute fkeyattr = null;
                if ((fkeyattr = ((ForeignKeyAttribute)Attribute.GetCustomAttribute(field, typeof(ForeignKeyAttribute)))) == null)
                {
                    // Continue
                    return;
                }

                if (fkeyattr.ForeignType != typeof(TParentObject))
                {
                    return;
                }

                // Found the foreign key for our parent type
                foreignKeyName = ((StoredDataAttribute)Attribute.GetCustomAttribute(field, typeof(StoredDataAttribute)))?.StoredName;

                if (foreignKeyName == null)
                {
                    foreignKeyName = field.Name;
                }
            });

            if (foreignKeyName == null)
            {
                throw new NotSupportedException("The correct foreign key was not found in the child type.");
            }

            foreach (DataRow row in _database.RunProcedure(parentAttribute.GetChildrenProcedure, new SqlParameter(foreignKeyName, parent.ID)).Rows)
            {
                TChildObject child = Activator.CreateInstance<TChildObject>();
                InitializeData(child, row);
                yield return child;
            }
        }

        protected IEnumerable<TObject> EnumeratePossibleValueAttributes<TObject>() where TObject : IDataObject
        {
            foreach (PossibleValueAttribute attr in Attribute.GetCustomAttributes(typeof(TObject), typeof(PossibleValueAttribute)))
            {
                yield return GetObjectByID<TObject>(attr.ID);
            }
        }

        public IEnumerable<TObject> GetAllObjectsOfType<TObject>() where TObject : IDataObject
        {
            if (Attribute.GetCustomAttribute(typeof(TObject), typeof(SqlSelectAllAttribute)) != null)
            {
                return GenericSelectAll(Activator.CreateInstance<TObject>);
            }
            else
            {
                return EnumeratePossibleValueAttributes<TObject>();
            }
        }

        public TObject GetObjectByID<TObject>(int id) where TObject : IDataObject
        {
            // TODO this is a wee bit of a hack
            TObject instance = Activator.CreateInstance<TObject>();
            SetObjectInternals(instance, id, true);
            GenericRead(instance);

            return instance;
        }

        protected static void SetObjectInternals(IDataObject instance, int id, bool isDataBacked)
        {

            var idProp = instance.GetType().GetProperty(nameof(instance.ID), BindingFlags.SetProperty | BindingFlags.FlattenHierarchy | System.Reflection.BindingFlags.SetProperty | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            idProp.SetValue(instance, id);

            var dataBacked = instance.GetType().GetProperty(nameof(instance.IsStoredData), BindingFlags.SetProperty | BindingFlags.FlattenHierarchy | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            dataBacked.SetValue(instance, isDataBacked);

        }

        public void SaveObject(IDataObject target)
        {
            SqlBackedClassAttribute dataAttr = Attribute.GetCustomAttribute(target.GetType(), typeof(SqlBackedClassAttribute)) as SqlBackedClassAttribute;

            if (dataAttr == null)
            {
                throw new InvalidOperationException("SqlBackedClassAttribute not found on the type of the given object.");
            }

            if (dataAttr.InsertProcedureName == null && dataAttr.UpdateProcedureName == null)
            {
                throw new ReadOnlyException("The given object type is read only.");
            }

            if (target.IsStoredData)
            {
                GenericUpdate(target);
            }
            else
            {
                GenericInsert(target);
            }
        }

        protected IEnumerable<TObject> GenericSelectAll<TObject>(Func<TObject> createBlank) where TObject : IDataObject
        {
            SqlSelectAllAttribute dataManagementAttribute = (SqlSelectAllAttribute)Attribute.GetCustomAttribute(typeof(TObject), typeof(SqlSelectAllAttribute));
            if (dataManagementAttribute == null)
            {
                throw new ArgumentException("The given type does not have the required SqlSelectAllAttribute.");
            }

            DataTable results = _database.RunProcedure(dataManagementAttribute.SelectAllProcedureName);
            foreach (DataRow result in results.Rows)
            {
                TObject newObject = createBlank();
                InitializeData(newObject, result);
                yield return newObject;
            }
        }

        // Uses annotations to provide a generic update function to all proper DataObjects
        protected void GenericUpdate(IDataObject targetObject)
        {
            if (targetObject == null)
            {
                throw new ArgumentNullException("targetObject");
            }

            if (!targetObject.IsStoredData)
            {
                // Object does not already exist in SQL
                throw new ArgumentException("The given object is not SQL backed; that is, it does not exist in SQL yet.");
            }

            SqlBackedClassAttribute dataManagementAttribute = (SqlBackedClassAttribute)Attribute.GetCustomAttribute(targetObject.GetType(), typeof(SqlBackedClassAttribute));
            if (dataManagementAttribute == null)
            {
                throw new ArgumentException("The given object does not have the required SqlBackedClassAttribute.");
            }

            List<SqlParameter> parameters = new List<SqlParameter>();

            GetFieldsAndProperties(targetObject.GetType(), (field, getter) =>
            {
                StoredDataAttribute sqlFieldAttribute = (StoredDataAttribute)Attribute.GetCustomAttribute(field, typeof(StoredDataAttribute));
                if (sqlFieldAttribute == null)
                {
                    // Continue
                    return;
                }
                SqlParameter param = new SqlParameter(sqlFieldAttribute.StoredName ?? field.Name, getter(targetObject));
                if (sqlFieldAttribute.DataType.HasValue)
                {
                    // If the data type is manually specified
                    param.DbType = sqlFieldAttribute.DataType.Value;
                }
                parameters.Add(param);
            });

            // Run the actual update
            _database.RunNonQueryProcedure(dataManagementAttribute.UpdateProcedureName, parameters.ToArray());
        }

        /// <summary>
        /// Sets fields and properties on a type. Uses <see cref="SearchBindingFlags"/> and <see cref="BindingFlags.SetProperty"/>.
        /// </summary>
        /// <param name="type">The type to process.</param>
        /// <param name="processor">The member processor. Takes a member and a setter delegate. The setter delegate takes an object and a value in that order.</param>
        protected static void SetFieldsAndProperties(Type type, Action<MemberInfo, Action<object, object>> processor)
        {
            foreach (FieldInfo field in type.GetFields(SearchBindingFlags))
            {
                processor(field, (obj, val) => field.SetValue(obj, val));
            }

            foreach (PropertyInfo prop in type.GetProperties(SearchBindingFlags | BindingFlags.SetProperty))
            {
                processor(prop, (obj, val) => prop.SetValue(obj, val));
            }
        }

        /// <summary>
        /// Gets fields and properties on a type. Uses <see cref="SearchBindingFlags"/> and <see cref="BindingFlags.GetProperty"/>.
        /// </summary>
        /// <param name="type">The type to process.</param>
        /// <param name="processor">The member processor. Takes a member and a getter delegate. The getter delegate takes an object.</param>
        protected static void GetFieldsAndProperties(Type type, Action<MemberInfo, Func<object, object>> processor)
        {
            foreach (FieldInfo field in type.GetFields(SearchBindingFlags))
            {
                processor(field, (obj) => field.GetValue(obj));
            }

            foreach (PropertyInfo prop in type.GetProperties(SearchBindingFlags | BindingFlags.GetProperty))
            {
                processor(prop, (obj) => prop.GetValue(obj));
            }
        }

        protected static Type GetMemberType(MemberInfo memInfo)
        {
            switch (memInfo.MemberType)
            {
                case MemberTypes.Property:
                    return (memInfo as PropertyInfo).PropertyType;
                case MemberTypes.Method:
                    return (memInfo as MethodInfo).ReturnType;
                case MemberTypes.Field:
                    return (memInfo as FieldInfo).FieldType;
                case MemberTypes.Event:
                    return (memInfo as EventInfo).EventHandlerType;
                default:
                    throw new ArgumentException("The given MemberInfo is not supported.");
            }
        }

        /// <summary>
        /// Modifies the given object to match the data given in the specified DataRow.
        /// </summary>
        public static void InitializeData(IDataObject targetObject, DataRow data)
        {
            SqlBackedClassAttribute dataManagementAttribute = (SqlBackedClassAttribute)Attribute.GetCustomAttribute(targetObject.GetType(), typeof(SqlBackedClassAttribute));
            if (dataManagementAttribute == null)
            {
                throw new ArgumentException("The given object does not have the required SqlBackedClassAttribute.");
            }

            SetFieldsAndProperties(targetObject.GetType(), (field, setter) =>
            {
                StoredDataAttribute sqlFieldAttribute = null;
                if ((sqlFieldAttribute = Attribute.GetCustomAttribute(field, typeof(StoredDataAttribute)) as StoredDataAttribute) == null)
                {
                    return;
                }


                // Found a SQL column
                object dataInstance = data[sqlFieldAttribute.StoredName ?? field.Name];

                ForeignKeyAttribute fkey = null;
                if ((fkey = Attribute.GetCustomAttribute(field, typeof(ForeignKeyAttribute)) as ForeignKeyAttribute) != null)
                {
                    if (fkey.ForeignType == GetMemberType(field))
                    {
                        // Get the object by ID and use that to set the field
                        // We have to use reflection to invoke a generic method with a type only known at runtime
                        // TODO strongly type the method names

                        throw new NotSupportedException("The static InitializeData method does not support object-type foreign keys.");
                        //field.SetValue(targetObject, this.GetType().GetMethod("GetObjectByID").MakeGenericMethod(fkey.ForeignType).Invoke(this, new object[]{dataInstance}));
                    }
                }
                else
                {
                    // Set the field directly
                    setter(targetObject, dataInstance is DBNull ? null : dataInstance);
                }
            });

            SetObjectInternals(targetObject, targetObject.ID, true);
        }

        // Uses annotations to provide a generic insert function to all proper DataObjects
        protected void GenericInsert(IDataObject targetObject)
        {
            if (targetObject == null)
            {
                throw new ArgumentNullException("targetObject");
            }

            SqlBackedClassAttribute dataManagementAttribute = (SqlBackedClassAttribute)Attribute.GetCustomAttribute(targetObject.GetType(), typeof(SqlBackedClassAttribute));
            if (dataManagementAttribute == null)
            {
                throw new ArgumentException("The given object does not have the required SqlBackedClassAttribute.");
            }

            List<SqlParameter> parameters = new List<SqlParameter>();

            string idColumnName = null;

            GetFieldsAndProperties(targetObject.GetType(), (field, getter) =>
            {
                StoredDataAttribute sqlFieldAttribute = (StoredDataAttribute)Attribute.GetCustomAttribute(field, typeof(StoredDataAttribute));
                if (sqlFieldAttribute == null)
                {
                    // Continue
                    return;
                }
                if (Attribute.GetCustomAttribute(field, typeof(PrimaryKeyAttribute)) != null)
                {
                    // Set ID and handle specially
                    idColumnName = sqlFieldAttribute.StoredName ?? field.Name;
                    return;
                }

                // Found a non-ID column
                SqlParameter param = new SqlParameter(sqlFieldAttribute.StoredName ?? field.Name, getter(targetObject));
                if (sqlFieldAttribute.DataType.HasValue)
                {
                    // If the data type is manually specified
                    param.DbType = sqlFieldAttribute.DataType.Value;
                }
                parameters.Add(param);
            });

            // Run the actual insert, returns the inserted record
            DataTable insertedRecords = _database.RunProcedure(dataManagementAttribute.InsertProcedureName, parameters.ToArray());

            if (insertedRecords.Rows.Count != 1)
            {
                throw new DataException(string.Format("{0} rows were inserted when only one should have been.", insertedRecords.Rows.Count));
            }

            DataRow insertedRecord = insertedRecords.Rows[0];

            if (idColumnName == null)
            {
                throw new ArgumentException("The specified object does not have an explicitly specified ID column.");
            }

            SetObjectInternals(targetObject, (int)insertedRecord[idColumnName], true);
        }

        // Uses annotations to provide a generic read function to all proper DataObjects
        protected void GenericRead(IDataObject targetObject)
        {
            if (targetObject == null)
            {
                throw new ArgumentNullException("targetObject");
            }

            SqlBackedClassAttribute dataManagementAttribute = (SqlBackedClassAttribute)Attribute.GetCustomAttribute(targetObject.GetType(), typeof(SqlBackedClassAttribute));
            if (dataManagementAttribute == null)
            {
                throw new ArgumentException("The given object does not have the required SqlBackedClassAttribute.");
            }

            SqlParameter idParameter = null;

            GetFieldsAndProperties(targetObject.GetType(), (field, getter) =>
            {
                if (idParameter != null)
                {
                    // No need to search
                    return;
                }

                StoredDataAttribute sqlFieldAttribute = (StoredDataAttribute)Attribute.GetCustomAttribute(field, typeof(StoredDataAttribute));
                if (sqlFieldAttribute == null || Attribute.GetCustomAttribute(field, typeof(PrimaryKeyAttribute)) == null)
                {
                    return;
                }

                // Found the ID column
                idParameter = new SqlParameter(sqlFieldAttribute.StoredName ?? field.Name, targetObject.ID);
            });



            // Run the actual update
            DataTable results = _database.RunProcedure(dataManagementAttribute.GetByIdProcedureName, idParameter);

            if (results.Rows.Count == 0)
            {
                throw new System.Data.RowNotInTableException("The given ID does not correspond to a record.");
            }
            else if (results.Rows.Count > 1)
            {
                throw new System.Data.DuplicateNameException("The given ID is ambiguous.");
            }

            // Update the data in the object
            InitializeData(targetObject, results.Rows[0]);
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // dispose managed state
                    _database.Dispose();
                }

                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.

                disposedValue = true;
            }
        }

        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        // ~SqlDataBackend() {
        //   // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
        //   Dispose(false);
        // }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            // TODO: uncomment the following line if the finalizer is overridden above.
            // GC.SuppressFinalize(this);
        }
        #endregion
    }
}
