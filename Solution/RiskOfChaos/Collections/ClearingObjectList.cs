using RiskOfChaos.Components;
using RiskOfChaos.Utilities;
using RiskOfChaos.Utilities.Extensions;
using RoR2;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;

namespace RiskOfChaos.Collections
{
    public sealed class ClearingObjectList<T> : IList<T>, IDisposable where T : UnityEngine.Object
    {
        readonly List<T> _list = [];
        readonly List<OnDestroyEvent> _destroyEvents = [];

        public T this[int index]
        {
            get => _list[index];
            set => _list[index] = value;
        }

        public int Count => _list.Count;

        public int Capacity
        {
            get => _list.Capacity;
            set
            {
                _list.Capacity = value;

                if (!DontUseDestroyEvent)
                {
                    _destroyEvents.Capacity = value;
                }
            }
        }

        public bool IsReadOnly => false;

        public bool DestroyComponentGameObject;

        public bool DontUseDestroyEvent;

        public string ObjectIdentifier { get; set; } = typeof(T).Name;

        bool _hasObjectDestroyedListeners;
        bool _trackedObjectDestroyed;

        float _autoClearTimer;
        float _autoClearInterval = -1f;
        public float AutoClearInterval
        {
            get
            {
                return _autoClearInterval;
            }
            set
            {
                if (_autoClearInterval == value)
                    return;

                _autoClearInterval = value;

                refreshClearListeners();
            }
        }

        bool _clearListenersActive;

        bool _isDisposed = false;

        public void Dispose()
        {
            if (_isDisposed)
                return;

            foreach (OnDestroyEvent destroyEvent in _destroyEvents)
            {
                if (destroyEvent)
                {
                    destroyEvent.OnDestroyed -= onElementObjectDestroyed;
                }
            }

            _destroyEvents.Clear();

            setClearListeners(false);

            _isDisposed = true;
        }

        void refreshClearListeners()
        {
            setClearListeners(_autoClearInterval > 0f || _hasObjectDestroyedListeners);
        }

        void setClearListeners(bool active)
        {
            if (_isDisposed)
                throw new ObjectDisposedException(ObjectIdentifier);

            if (_clearListenersActive == active)
                return;

            _clearListenersActive = active;

            if (_clearListenersActive)
            {
                RoR2Application.onFixedUpdate += fixedUpdate;
            }
            else
            {
                RoR2Application.onFixedUpdate -= fixedUpdate;
            }
        }

        void fixedUpdate()
        {
            bool shouldClear = false;
            if (_autoClearInterval > 0f)
            {
                _autoClearTimer += Time.fixedDeltaTime;
                if (_autoClearTimer >= _autoClearInterval)
                {
                    _autoClearTimer = 0f;
                    shouldClear = true;
                }
            }

            if (_trackedObjectDestroyed)
            {
                _trackedObjectDestroyed = false;
                shouldClear = true;
            }

            if (shouldClear)
            {
                RemoveAllDestroyed();
            }
        }

        public void EnsureCapacity(int capacity)
        {
            if (Capacity < capacity)
            {
                Capacity = capacity;
            }
        }

        bool tryGetGameObject(T item, out GameObject gameObject)
        {
            gameObject = null;
            if (item)
            {
                switch (item)
                {
                    case Component component:
                        gameObject = component.gameObject;
                        break;
                    case GameObject asGameObject:
                        gameObject = asGameObject;
                        break;
                }
            }

            return gameObject;
        }

        void onElementAdd(T item)
        {
            if (item)
            {
                if (!DontUseDestroyEvent && tryGetGameObject(item, out GameObject gameObject))
                {
                    OnDestroyEvent destroyEvent = OnDestroyEvent.Add(gameObject, onElementObjectDestroyed);
                    if (!_destroyEvents.Contains(destroyEvent))
                    {
                        _destroyEvents.Add(destroyEvent);
                        _hasObjectDestroyedListeners = true;
                        refreshClearListeners();
                    }
                }
            }
        }

        void onElementRemove(T item)
        {
            if (item)
            {
                if (!DontUseDestroyEvent && tryGetGameObject(item, out GameObject gameObject))
                {
                    OnDestroyEvent.Remove(gameObject, onElementObjectDestroyed);
                }
            }
        }

        void onElementObjectDestroyed(GameObject obj)
        {
            if (_isDisposed)
            {
                Log.Warning($"({ObjectIdentifier}) Callback triggered on disposed object");
                return;
            }

            _trackedObjectDestroyed = true;
        }

        public void Add(T item)
        {
            _list.Add(item);
            onElementAdd(item);
        }

        public void AddRange(IEnumerable<T> collection)
        {
            _list.AddRange(collection);

            foreach (T item in collection)
            {
                onElementAdd(item);
            }
        }

        public void RemoveAllDestroyed()
        {
            UnityObjectUtils.RemoveAllDestroyed(_destroyEvents);

            int numRemovedObjects = UnityObjectUtils.RemoveAllDestroyed(this);
            if (numRemovedObjects > 0)
            {
                Log.Debug($"({ObjectIdentifier}) Cleared {numRemovedObjects} destroyed elements(s)");
            }
        }

        public void Clear()
        {
            Clear(false);
        }

        public void Clear(bool destroyElements)
        {
            foreach (T element in _list)
            {
                onElementRemove(element);

                if (destroyElements)
                {
                    if (element)
                    {
                        UnityEngine.Object objToDestroy = element;
                        if (DestroyComponentGameObject && tryGetGameObject(element, out GameObject gameObject))
                        {
                            objToDestroy = gameObject;
                        }

                        GameObject.Destroy(objToDestroy);
                    }
                }
            }

            _list.Clear();
        }

        public void ClearAndDispose(bool destroyElements)
        {
            Clear(destroyElements);
            Dispose();
        }

        public bool Contains(T item)
        {
            return _list.Contains(item);
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            _list.CopyTo(array, arrayIndex);
        }

        public int IndexOf(T item)
        {
            return _list.IndexOf(item);
        }

        public void Insert(int index, T item)
        {
            _list.Insert(index, item);
            onElementAdd(item);
        }

        public bool Remove(T item)
        {
            if (_list.Remove(item))
            {
                onElementRemove(item);
                return true;
            }

            return false;
        }

        public void RemoveAt(int index)
        {
            T removedItem = _list.GetAndRemoveAt(index);
            onElementRemove(removedItem);
        }

        public ReadOnlyCollection<T> AsReadOnly()
        {
            return _list.AsReadOnly();
        }

        public IEnumerator<T> GetEnumerator()
        {
            return _list.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
