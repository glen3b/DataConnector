using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataConnector
{
    public interface IDataBackend<in TBaseObject> : IDisposable where TBaseObject : IDataObject  
    {
        /// <summary>
		/// Saves the given object into the data backend, setting identifying information in the object if needed.
        /// This will occur most often in inserts.
		/// </summary>
		/// <param name="target">The object to save.</param>
        /// <exception cref="System.Data.ReadOnlyException">Thrown if the target object type is read only.</exception>
        void SaveObject(TBaseObject target);
        
		/// <summary>
		/// Retrieves an object by its unique database identifier.
		/// </summary>
		/// <returns>The object of the given type with the given identifier.</returns>
		/// <param name="id">The unique identifier of the object.</param>
		/// <typeparam name="TObject">The type of the object to retrieve.</typeparam>
		/// <exception cref="System.Data.RowNotInTableException">Thrown if the given ID does not correspond to a record.</exception>
		/// <exception cref="System.Data.DuplicateNameException">Thrown if the given ID is ambiguous.</exception>
		TObject GetObjectByID<TObject>(int id) where TObject : TBaseObject;

		/// <summary>
		/// Retrieves all objects of a given type in the database.
		/// </summary>
		/// <returns>All objects of the given type in the data backend.</returns>
		/// <typeparam name="TObject">The type of the objects to retrieve.</typeparam>
        IEnumerable<TObject> GetAllObjectsOfType<TObject>() where TObject : TBaseObject;

        /// <summary>
		/// Gets the children of a given object. Expected to be used in one-to-many relationships.
		/// </summary>
		/// <returns>The children of the given parent object.</returns>
		/// <param name="parentId">The ID of the parent object, for which children will be retrieved.</param>
		/// <typeparam name="TParentObject">The type of the parent.</typeparam>
		/// <typeparam name="TChildObject">The type of the children.</typeparam>
		/// <exception cref="System.NotSupportedException">Thrown if the given pair of types does not have a one-to-many relationship.</exception>
		IEnumerable<TChildObject> GetChildrenOf<TParentObject, TChildObject>(int parentId)
            where TParentObject : TBaseObject
            where TChildObject : TBaseObject;

        /// <summary>
        /// Gets the children of a given object. Expected to be used in one-to-many relationships.
        /// </summary>
        /// <returns>The children of the given parent object.</returns>
        /// <param name="parent">The parent object, for which children will be retrieved.</param>
        /// <typeparam name="TParentObject">The type of the parent.</typeparam>
        /// <typeparam name="TChildObject">The type of the children.</typeparam>
        /// <exception cref="System.NotSupportedException">Thrown if the given pair of types does not have a one-to-many relationship.</exception>
        IEnumerable<TChildObject> GetChildrenOf<TParentObject, TChildObject>(TParentObject parent)
			where TParentObject : TBaseObject
			where TChildObject : TBaseObject;
    }
}
