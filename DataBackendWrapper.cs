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
    public class DataBackendWrapper<TDataObject> : IDataBackend<IDataObject> where TDataObject : IDataObject
    {
        public void Dispose()
        {
            WrappedBackend.Dispose();
        }

        public IEnumerable<TObject> GetAllObjectsOfType<TObject>() where TObject : IDataObject
        {
            return InvokeBackendInstanceMethod<IEnumerable<TObject>>(nameof(WrappedBackend.GetAllObjectsOfType), new Type[] { typeof(TObject) });
        }

        public IEnumerable<TChildObject> GetChildrenOf<TParentObject, TChildObject>(TParentObject parent)
            where TParentObject : IDataObject
            where TChildObject : IDataObject
        {
            EnforceValidity(parent);

            return InvokeBackendInstanceMethod<IEnumerable<TChildObject>>(nameof(WrappedBackend.GetChildrenOf), new Type[] { typeof(TParentObject), typeof(TChildObject) }, parent);
        }

        public IEnumerable<TChildObject> GetChildrenOf<TParentObject, TChildObject>(int parentId)
            where TParentObject : IDataObject
            where TChildObject : IDataObject
        {
            return InvokeBackendInstanceMethod<IEnumerable<TChildObject>>(nameof(WrappedBackend.GetChildrenOf), new Type[] { typeof(TParentObject), typeof(TChildObject) }, parentId);
        }

        public TObject GetObjectByID<TObject>(int id) where TObject : IDataObject
        {
            // Backends are expected to use the type parameter, so we have to use reflection

            if (!typeof(TDataObject).IsAssignableFrom(typeof(TObject)))
            {
                throw new ArgumentException("The given type must be of the type supported by the backend wrapped by this wrapper.");
            }

            return InvokeBackendInstanceMethod<TObject>(nameof(WrappedBackend.GetObjectByID), new Type[] { typeof(TObject) }, id);
        }

        public void SaveObject(IDataObject target)
        {
            EnforceValidity(target);
            WrappedBackend.SaveObject((TDataObject)target);
        }

        protected virtual TReturn InvokeBackendInstanceMethod<TReturn>(string methodName, Type[] genericParameters, params object[] parameters)
        {
            const System.Reflection.BindingFlags bindingFlags = System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.InvokeMethod | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.FlattenHierarchy;

            try
            {                
                var method = WrappedBackend.GetType().GetMethod(methodName, bindingFlags, Type.DefaultBinder, new Type[] { typeof(int) }, null);

                var properMethod = method.MakeGenericMethod(genericParameters);

                return (TReturn)properMethod.Invoke(WrappedBackend, parameters);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("There was an error invoking the real backend's proper method.", ex);
            }
        }

        /// <summary>
        /// Enforce the validity of a data object as a backend-acceptable target.
        /// </summary>
        /// <param name="target"></param>
        protected virtual void EnforceValidity(IDataObject target)
        {
            if (!(target is TDataObject))
            {
                throw new ArgumentException("The target object must be accepted by the wrapped backend.");
            }
        }

        public DataBackendWrapper(IDataBackend<TDataObject> baseBackend)
        {
            WrappedBackend = baseBackend;
        }

        private IDataBackend<TDataObject> _backedBackend;

        public IDataBackend<TDataObject> WrappedBackend
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

    }
}
