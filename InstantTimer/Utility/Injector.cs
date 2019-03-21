using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InstantTimer.Utility
{
    static class Injector
    {
        private static readonly object _syncRoot = new object();
        private static Dictionary<Type, object> _dict = new Dictionary<Type, object>();

        public static void Put<TInterface>(TInterface implementation)
        {
            if (implementation == null) throw new ArgumentNullException(nameof(implementation));
            lock (_syncRoot)
            {
                if (_dict.ContainsKey(typeof(TInterface))) throw new InvalidOperationException($"The type \"{typeof(TInterface).AssemblyQualifiedName}\" is already registered with an object of type \"{_dict[typeof(TInterface)].GetType().AssemblyQualifiedName}\".");
                _dict.Add(typeof(TInterface), implementation);
            }
        }

        public static TInterface Get<TInterface>()
        {
            lock(_syncRoot)
            {
                if (!_dict.ContainsKey(typeof(TInterface))) throw new InvalidOperationException($"The type \"{typeof(TInterface).AssemblyQualifiedName}\" doesn't have an implementation.");
                return (TInterface)_dict[typeof(TInterface)];
            }
        }

        public static void DisposeContent()
        {
            lock (_syncRoot)
            {
                foreach (KeyValuePair<Type, object> kvp in _dict)
                {
                    if (kvp.Value is IDisposable disposable)
                    {
                        disposable.Dispose();
                    }
                }
                _dict.Clear();
            }
        }
    }
}
