using System;
using System.Collections;
using System.Collections.Generic;

namespace RiskOfChaos.Collections.CatalogIndex
{
    public abstract class CatalogIndexCollection<T> : IReadOnlyCollection<T>
    {
        static readonly bool _isComparable = typeof(IComparable).IsAssignableFrom(typeof(T)) || typeof(IComparable<T>).IsAssignableFrom(typeof(T));

        readonly string[] _names;
        readonly T[] _items;

        int _itemsLength;

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

        public int Count => _itemsLength;

        public CatalogIndexCollection(params string[] names)
        {
            _names = names;
            _items = new T[names.Length];
        }

        void trySortItems()
        {
            if (Comparer != null || _isComparable)
            {
                Array.Sort(_items, 0, _itemsLength, Comparer);
            }
        }

        protected abstract T findByName(string name);

        protected abstract bool isValid(T value);

        protected virtual void initialize()
        {
            int length = 0;
            for (int i = 0; i < _names.Length; i++)
            {
                T value = findByName(_names[i]);

                if (isValid(value))
                {
                    _items[length] = value;
                    length++;
                }
            }

            _itemsLength = length;

            trySortItems();

            IsInitialized = true;
        }

        public int IndexOf(T item)
        {
            if (!IsInitialized)
                return -1;

            if (Comparer != null || _isComparable)
            {
                return Array.BinarySearch(_items, 0, _itemsLength, item, Comparer);
            }
            else
            {
                return Array.IndexOf(_items, item, 0, _itemsLength);
            }
        }

        public bool Contains(T item)
        {
            return IndexOf(item) >= 0;
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            Array.Copy(_items, 0, array, arrayIndex, _itemsLength);
        }

        public IEnumerator<T> GetEnumerator()
        {
            return new ArraySegment<T>(_items, 0, _itemsLength).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
