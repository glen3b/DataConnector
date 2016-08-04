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
    public class SqlDataBackend : IDataBackend<SqlDataObject>
    {
        private ISqlWrapper _database;

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
		public SqlDataBackend(string connectionString) : this(new NewObjectSqlWrapper(connectionString)) {

		}


        public IEnumerable<TChildObject> GetChildrenOf<TParentObject, TChildObject>(TParentObject parent)
            where TParentObject : SqlDataObject
            where TChildObject : SqlDataObject
        {
            if (parent == null)
            {
                throw new ArgumentNullException("parent");
            }

            OneToManyRelationshipAttribute parentAttribute = Attribute.GetCustomAttribute(typeof(TParentObject), typeof(OneToManyRelationshipAttribute)) as OneToManyRelationshipAttribute;
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
            foreach (var field in typeof(TChildObject).GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.FlattenHierarchy))
            {
                ForeignKeyAttribute fkeyattr = null;
                if ((fkeyattr = ((ForeignKeyAttribute)Attribute.GetCustomAttribute(field, typeof(ForeignKeyAttribute)))) == null)
                {
                    // Continue
                    continue;
                }

                if (fkeyattr.TargetTable != typeof(TParentObject))
                {
                    continue;
                }

                // Found the foreign key for our parent type
                foreignKeyName = ((SqlFieldAttribute)Attribute.GetCustomAttribute(field, typeof(SqlFieldAttribute)))?.SQLFieldName;
                break;
            }

            if (foreignKeyName == null)
            {
                throw new NotSupportedException("The correct foreign key was not found in the child type.");
            }

            foreach (DataRow row in _database.RunProcedure(parentAttribute.GetChildrenProcedure, new SqlParameter(foreignKeyName, parent.ID)).Rows)
            {
                TChildObject child = CreateObject<TChildObject>();
                InitializeData(child, row);
                yield return child;
            }
        }

        protected IEnumerable<TObject> EnumeratePossibleValueAttributes<TObject>() where TObject : SqlDataObject
        {
            foreach (PossibleValueAttribute attr in Attribute.GetCustomAttributes(typeof(TObject), typeof(PossibleValueAttribute)))
            {
                yield return GetObjectByID<TObject>(attr.ID);
            }
        }

        public IEnumerable<TObject> GetAllObjectsOfType<TObject>() where TObject : SqlDataObject
        {
            if (Attribute.GetCustomAttribute(typeof(TObject), typeof(SqlSelectAllAttribute)) != null)
            {
                return GenericSelectAll(CreateObject<TObject>);
            }
            else
            {
                return EnumeratePossibleValueAttributes<TObject>();
            }
        }

        public TObject GetObjectByID<TObject>(int id) where TObject : SqlDataObject
        {
            // TODO this is a wee bit of a hack
            TObject instance = CreateObject<TObject>();
            SetObjectInternals(instance, id, true);
            GenericRead(instance);

            return instance;
        }

        protected TObject CreateObject<TObject>() where TObject : SqlDataObject
        {
            // TODO less hacky
            // TODO fix this method

            ConstructorInfo constructor = null;
            MethodInfo createBlank = null;

            try
            {
                constructor = typeof(TObject).GetConstructor(new Type[0]);
            }
            catch
            {
            }

            try
            {
                createBlank = typeof(TObject).GetMethod("Create", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static, Type.DefaultBinder, Type.EmptyTypes, null);
            }
            catch
            {
            }

            TObject instance = null;

            if (constructor != null)
            {
                try
                {
                    instance = constructor.Invoke(new object[0]) as TObject;
                }
                catch
                {
                }
            }

            if (createBlank != null && instance == null)
            {
                try
                {
                    instance = createBlank.Invoke(null, new object[0]) as TObject;
                }
                catch
                {
                }
            }

            if (instance == null)
            {
                throw new ArgumentException("The specified type does not have a constructor or create method.");
            }

            return instance;
        }

        protected void SetObjectInternals(SqlDataObject instance, int id, bool isDataBacked)
        {
            var idProp = instance.GetType().GetProperty("ID", System.Reflection.BindingFlags.FlattenHierarchy | System.Reflection.BindingFlags.SetProperty | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            idProp.SetValue(instance, id);

            var dataBacked = instance.GetType().GetField("IsSQLBacked", System.Reflection.BindingFlags.FlattenHierarchy | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            dataBacked.SetValue(instance, isDataBacked);

        }

        public void LoadObject(SqlDataObject target)
        {
            if (!(target is SqlDataObject))
            {
                throw new ArgumentException("The SqlDataBackend cannot load objects unless they extend SqlDataObject.");
            }

            GenericRead(target as SqlDataObject);
        }

        public void SaveObject(SqlDataObject target)
        {
            if (!(target is SqlDataObject))
            {
                throw new ArgumentException("The SqlDataBackend cannot save objects unless they extend SqlDataObject.");
            }

            SqlBackedClassAttribute dataAttr = Attribute.GetCustomAttribute(target.GetType(), typeof(SqlBackedClassAttribute)) as SqlBackedClassAttribute;
                        
            if(dataAttr == null)
            {
                throw new InvalidOperationException("SqlBackedClassAttribute not found on the type of the given object.");
            }

            if(dataAttr.InsertProcedureName == null && dataAttr.UpdateProcedureName == null)
            {
                throw new ReadOnlyException("The given object type is read only.");
            }

            if (target.IsStoredData)
            {
                GenericUpdate(target as SqlDataObject);
            }
            else
            {
                GenericInsert(target as SqlDataObject);
            }
        }

        protected IEnumerable<TObject> GenericSelectAll<TObject>(Func<TObject> createBlank) where TObject : SqlDataObject
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
        protected void GenericUpdate(SqlDataObject targetObject)
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

            foreach (FieldInfo field in targetObject.GetType().GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.FlattenHierarchy))
            {
                SqlFieldAttribute sqlFieldAttribute = (SqlFieldAttribute)Attribute.GetCustomAttribute(field, typeof(SqlFieldAttribute));
                if (sqlFieldAttribute == null)
                {
                    continue;
                }
                SqlParameter param = new SqlParameter(sqlFieldAttribute.SQLFieldName, field.GetValue(targetObject));
                if (sqlFieldAttribute.DataType != default(DbType))
                {
                    // If the data type is manually specified
                    param.DbType = sqlFieldAttribute.DataType;
                }
                parameters.Add(param);
            }

            // Run the actual update
            _database.RunNonQueryProcedure(dataManagementAttribute.UpdateProcedureName, parameters.ToArray());
        }


        /// <summary>
        /// Modifies the current object to match the data given in the specified DataRow.
        /// </summary>
        protected void InitializeData(SqlDataObject targetObject, DataRow data)
        {
            SqlBackedClassAttribute dataManagementAttribute = (SqlBackedClassAttribute)Attribute.GetCustomAttribute(targetObject.GetType(), typeof(SqlBackedClassAttribute));
            if (dataManagementAttribute == null)
            {
                throw new ArgumentException("The given object does not have the required SqlBackedClassAttribute.");
            }

            foreach (FieldInfo field in targetObject.GetType().GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.FlattenHierarchy))
            {
                SqlFieldAttribute sqlFieldAttribute = (SqlFieldAttribute)Attribute.GetCustomAttribute(field, typeof(SqlFieldAttribute));
                if (sqlFieldAttribute == null)
                {
                    continue;
                }

                // Found a SQL column
                object dataInstance = data[sqlFieldAttribute.SQLFieldName];
                field.SetValue(targetObject, dataInstance is DBNull ? null : dataInstance);
            }

            SetObjectInternals(targetObject, targetObject.ID, true);
        }

        // Uses annotations to provide a generic insert function to all proper DataObjects
        protected void GenericInsert(SqlDataObject targetObject)
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

            foreach (FieldInfo field in targetObject.GetType().GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.FlattenHierarchy))
            {
                SqlFieldAttribute sqlFieldAttribute = (SqlFieldAttribute)Attribute.GetCustomAttribute(field, typeof(SqlFieldAttribute));
                if (sqlFieldAttribute == null)
                {
                    continue;
                }
                if (Attribute.GetCustomAttribute(field, typeof(PrimaryKeyAttribute)) != null)
                {
                    idColumnName = sqlFieldAttribute.SQLFieldName;
                    continue;
                }

                // Found a non-ID column
                SqlParameter param = new SqlParameter(sqlFieldAttribute.SQLFieldName, field.GetValue(targetObject));
                if (sqlFieldAttribute.DataType != default(DbType))
                {
                    // If the data type is manually specified
                    param.DbType = sqlFieldAttribute.DataType;
                }
                parameters.Add(param);
            }

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
        protected void GenericRead(SqlDataObject targetObject)
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

            foreach (FieldInfo field in targetObject.GetType().GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.FlattenHierarchy))
            {
                SqlFieldAttribute sqlFieldAttribute = (SqlFieldAttribute)Attribute.GetCustomAttribute(field, typeof(SqlFieldAttribute));
                if (sqlFieldAttribute == null || Attribute.GetCustomAttribute(field, typeof(PrimaryKeyAttribute)) == null)
                {
                    continue;
                }

                // Found the ID column
                idParameter = new SqlParameter(sqlFieldAttribute.SQLFieldName, targetObject.ID);
                break; // No need to search further
            }

            // Run the actual update
            DataTable results = _database.RunProcedure(dataManagementAttribute.GetByIdProcedureName, idParameter);

            if (results.Rows.Count != 1)
            {
                throw new ArgumentException("The given ID is either ambiguous or does not correspond to a record.");
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
