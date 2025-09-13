using System;
using System.Collections;
using System.Collections.Generic;

namespace RiskOfChaos.Collections
{
    public class MaxCapacityQueue<T> : IReadOnlyCollection<T>, ICollection
    {
        readonly Queue<T> _queue;

        public int Count
        {
            get
            {
                return _queue.Count;
            }
        }

        int _maxCapacity;
        public int MaxCapacity
        {
            get
            {
                return _maxCapacity;
            }
            set
            {
                if (_maxCapacity == value)
                    return;

                _maxCapacity = value;
                checkCapacity();
            }
        }

        public bool IsSynchronized => ((ICollection)_queue).IsSynchronized;

        public object SyncRoot => ((ICollection)_queue).SyncRoot;

        public MaxCapacityQueue(int maxCapacity)
        {
            if (maxCapacity <= 0)
                throw new ArgumentOutOfRangeException(nameof(maxCapacity), "Max capacity must be greater than zero.");

            _queue = new Queue<T>(maxCapacity);
            MaxCapacity = maxCapacity;
        }

        public void CopyTo(Array array, int index)
        {
            ((ICollection)_queue).CopyTo(array, index);
        }

        public IEnumerator<T> GetEnumerator()
        {
            return _queue.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _queue.GetEnumerator();
        }

        void checkCapacity()
        {
            while (_queue.Count > _maxCapacity)
            {
                _queue.Dequeue();
            }
        }

        public void Enqueue(T item)
        {
            _queue.Enqueue(item);

            if (_queue.Count > _maxCapacity)
            {
                _queue.Dequeue();
            }
        }

        public T Dequeue()
        {
            return _queue.Dequeue();
        }

        public T Peek()
        {
            return _queue.Peek();
        }

        public bool Contains(T item)
        {
            return _queue.Contains(item);
        }

        public T[] ToArray()
        {
            return [.. _queue];
        }

        public void Clear()
        {
            _queue.Clear();
        }
    }
}
