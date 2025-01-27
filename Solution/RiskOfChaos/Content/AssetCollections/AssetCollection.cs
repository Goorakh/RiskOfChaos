﻿using RoR2.ContentManagement;
using System;
using System.Collections;
using System.Collections.Generic;

namespace RiskOfChaos.Content.AssetCollections
{
    internal abstract class AssetCollection<T> : IList<T>
    {
        readonly List<T> _assets = [];

        bool _flushed;

        public T this[int index]
        {
            get => _assets[index];
            set => _assets[index] = value;
        }

        public int Count => _assets.Count;

        public int Capacity
        {
            get => _assets.Capacity;
            set => _assets.Capacity = value;
        }

        public bool IsReadOnly => false;

        void checkFlushedBeforeAdd(T item)
        {
            if (_flushed)
            {
                Log.Error($"Attempting to add asset '{item}' to {this}, but asset collection has already been flushed! The asset will not be included in the content pack!");
            }
        }

        public void Add(T item)
        {
            checkFlushedBeforeAdd(item);
            _assets.Add(item);
        }

        public void FlushAssets(NamedAssetCollection<T> destination)
        {
            if (Count > 0)
            {
                destination.Add(ToArray());
                _assets.Clear();
            }

            _flushed = true;
        }

        void ICollection<T>.Clear()
        {
            throw new NotSupportedException("Removing elements is not supported");
        }

        public bool Contains(T item)
        {
            return _assets.Contains(item);
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            _assets.CopyTo(array, arrayIndex);
        }

        public T[] ToArray()
        {
            return _assets.ToArray();
        }

        public void EnsureCapacity(int capacity)
        {
            if (Capacity < capacity)
            {
                Capacity = capacity;
            }
        }

        public IEnumerator<T> GetEnumerator()
        {
            return _assets.GetEnumerator();
        }

        public int IndexOf(T item)
        {
            return _assets.IndexOf(item);
        }

        public void Insert(int index, T item)
        {
            checkFlushedBeforeAdd(item);
            _assets.Insert(index, item);
        }

        bool ICollection<T>.Remove(T item)
        {
            throw new NotSupportedException("Removing elements is not supported");
        }

        void IList<T>.RemoveAt(int index)
        {
            throw new NotSupportedException("Removing elements is not supported");
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
