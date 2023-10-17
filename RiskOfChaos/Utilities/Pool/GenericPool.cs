using System;
using System.Collections.Generic;

namespace RiskOfChaos.Utilities.Pool
{
    public abstract class GenericPool<T>
    {
        int _numCreatedObjects = 0;

        readonly Stack<T> _pooledObjects = new Stack<T>();

        public void WarmUp(int count)
        {
            while (_pooledObjects.Count < count)
            {
                _pooledObjects.Push(generateNew());
            }
        }

        protected virtual T createNew(int creationID)
        {
            return Activator.CreateInstance<T>();
        }

        T generateNew()
        {
            return createNew(_numCreatedObjects++);
        }

        public void Return(T value)
        {
            if (value is IPooledObject pooledObject)
            {
                pooledObject.ResetValues();
            }

            _pooledObjects.Push(value);
        }

        public T GetOrCreateNew()
        {
            if (_pooledObjects.Count > 0)
            {
                return _pooledObjects.Pop();
            }

            return generateNew();
        }
    }
}
