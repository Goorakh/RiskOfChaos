using HG;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace RiskOfChaos.Utilities.ParsedValueHolders.ParsedList
{
    public abstract class GenericParsedList<T> : GenericParsedValue<ReadOnlyArray<T>>, IEnumerable<T>
    {
        public int Length => TryGetValue(out ReadOnlyArray<T> array) ? array.Length : 0;

        readonly IComparer<T> _comparer;

        readonly List<ParseFailReason> _itemParseFailReasons = new List<ParseFailReason>();

        public GenericParsedList(IComparer<T> comparer) : base()
        {
            _comparer = comparer;
        }

        public GenericParsedList() : this(null)
        {
        }

        public ref readonly T this[int i]
        {
            get
            {
                if (TryGetValue(out ReadOnlyArray<T> array))
                {
                    return ref array[i];
                }
                else
                {
                    throw new IndexOutOfRangeException();
                }
            }
        }

        public int IndexOf(T item)
        {
            if (!TryGetValue(out ReadOnlyArray<T> array))
                return -1;

            if (_comparer != null)
            {
                return ReadOnlyArray<T>.BinarySearch(array, item, _comparer);
            }
            else
            {
                return ReadOnlyArray<T>.IndexOf(array, item);
            }
        }

        public bool Contains(T item)
        {
            return IndexOf(item) >= 0;
        }

        protected sealed override ReadOnlyArray<T> parseInput(string input)
        {
            _itemParseFailReasons.Clear();
            if (string.IsNullOrWhiteSpace(input))
            {
                return Array.Empty<T>();
            }

            T[] result = parseList(input, _itemParseFailReasons).ToArray();

            if (_comparer != null)
            {
                Array.Sort(result, _comparer);
            }

            return result;
        }

        IEnumerable<T> parseList(string input, List<ParseFailReason> failReasonsList)
        {
            foreach (string item in splitInput(input))
            {
                T value;
                try
                {
                    value = handleParsedInput(item, parseValue);
                }
                catch (ParseException ex)
                {
                    if (_boundToConfig != null)
                    {
                        Log.Warning($"Failed to parse {_boundToConfig.Definition} list item \"{item}\": " + ex.Message);
                    }
                    else
                    {
                        Log.Warning($"Failed to parse list item \"{item}\": " + ex.Message);
                    }

                    failReasonsList?.Add(new ParseFailReason(item, ex));

                    continue;
                }

                yield return value;
            }
        }

        public override IEnumerable<ParseFailReason> GetAllParseFailReasons()
        {
            return base.GetAllParseFailReasons().Concat(_itemParseFailReasons);
        }

        protected abstract IEnumerable<string> splitInput(string input);

        protected abstract T parseValue(string str);

        protected IEnumerable<T> getEnumerable()
        {
            return TryGetValue(out ReadOnlyArray<T> array) ? array : Enumerable.Empty<T>();
        }

        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            return getEnumerable().GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable)getEnumerable()).GetEnumerator();
        }

        public static implicit operator ReadOnlyArray<T>(GenericParsedList<T> list)
        {
            return list.GetValue(Array.Empty<T>());
        }
    }
}
