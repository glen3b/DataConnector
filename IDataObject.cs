namespace DataConnector
{
	public interface IDataObject
    {
        /// <summary>
        /// Gets the unique identifier of this object in the data backend.
		/// This property must have a setter, however it does not have to be publicly visible.
        /// </summary>
        int ID { get; }

        /// <summary>
        /// Gets a value indicating whether this value has been committed to the backend, or in other words,
        /// returns true if and only if an identifier has been assigned to this object from the backend.
        /// Note that this may return true if unsaved changes have been made, but will not return true if the ID is not assigned.
		/// This property must have a setter, however it does not have to be publicly visible.
        /// </summary>
		bool IsStoredData { get; }
        
        // IDataBackend<IDataObject> DataBackend { get; }
    }
}