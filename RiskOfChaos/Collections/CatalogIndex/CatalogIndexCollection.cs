using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace RiskOfChaos.Collections.CatalogIndex
{
    public abstract class CatalogIndexCollection<T> : IReadOnlyList<T>
    {
        static readonly bool _isComparable = typeof(IComparable).IsAssignableFrom(typeof(T));

        readonly string[] _names;
        T[] _items;

        public bool IsInitialized { get; private set; }

        IComparer<T> _comparer;
        public IComparer<T> Comparer
        {
            get
            {
                return _comparer;
            }
            set
            {
                _comparer = value;

                if (IsInitialized)
                {
                    trySortItems();
                }
            }
        }

        public int Count => _items.Length;

        public bool IsReadOnly => true;

        public T this[int i] => _items[i];

        public CatalogIndexCollection(params string[] names)
        {
            _names = names;
            _items = new T[names.Length];
        }

        void trySortItems()
        {
            if (Comparer != null)
            {
                Array.Sort(_items, Comparer);
            }
            else if (_isComparable)
            {
                Array.Sort(_items);
            }
        }

        protected abstract bool isValid(T value);

        protected virtual void initialize()
        {
            bool anyInvalid = false;

            for (int i = 0; i < _names.Length; i++)
            {
                T value = findByName(_names[i]);
                _items[i] = value;

                if (!isValid(value))
                    anyInvalid = true;
            }

            if (anyInvalid)
            {
                _items = _items.Where(isValid).ToArray();
            }

            trySortItems();

            IsInitialized = true;
        }

        protected abstract T findByName(string name);

        public int IndexOf(T item)
        {
            if (!IsInitialized)
                return -1;

            if (Comparer != null)
            {
                return Array.BinarySearch(_items, item, Comparer);
            }
            else if (_isComparable)
            {
                return Array.BinarySearch(_items, item);
            }
            else
            {
                return Array.IndexOf(_items, item);
            }
        }

        public bool Contains(T item)
        {
            return IndexOf(item) >= 0;
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            _items.CopyTo(array, arrayIndex);
        }

        public IEnumerator<T> GetEnumerator()
        {
            return ((IEnumerable<T>)_items).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _items.GetEnumerator();
        }
    }
}
