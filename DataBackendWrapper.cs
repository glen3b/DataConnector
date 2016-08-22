using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataConnector
{
    /// <summary>
    /// A wrapper around a more specific data backend that uses runtime exceptions to allow or disallow potentially more generic requests.
    /// A hack.
    /// </summary>
    [Obsolete("This hack is not needed with the new IDataBackend.")]
    public class DataBackendWrapper<TDataObject> : IDataBackend
    {
        public DataBackendWrapper(IDataBackend baseBackend)
        {
            WrappedBackend = baseBackend;
        }

        private IDataBackend _backedBackend;

        public IDataBackend WrappedBackend
        {
            get { return _backedBackend; }
            set
            {
                if (value == null)
                {
                    throw new ArgumentException("The wrapped backend cannot be null.");
                }

                _backedBackend = value;
            }
        }

        public void SaveObject(IDataObject target)
        {
            WrappedBackend.SaveObject(target);
        }

        public TObject GetObjectByID<TObject>(int id) where TObject : IDataObject
        {
            return WrappedBackend.GetObjectByID<TObject>(id);
        }

        public IEnumerable<TObject> GetAllObjectsOfType<TObject>() where TObject : IDataObject
        {
            return WrappedBackend.GetAllObjectsOfType<TObject>();
        }

        public IEnumerable<TChildObject> GetChildrenOf<TParentObject, TChildObject>(TParentObject parent)
            where TParentObject : IDataObject
            where TChildObject : IDataObject
        {
            return WrappedBackend.GetChildrenOf<TParentObject, TChildObject>(parent);
        }

        public void Dispose()
        {
            WrappedBackend.Dispose();
        }
    }
}
