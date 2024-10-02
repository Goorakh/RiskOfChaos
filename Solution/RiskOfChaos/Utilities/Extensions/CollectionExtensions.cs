using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace RiskOfChaos.Utilities.Extensions
{
    public static class CollectionExtensions
    {
        const string DEFAULT_ERROR_LOG_FORMAT = "Failed to perform action for {0}: {1}";

        public static void TryDo<T>(this IEnumerable<T> enumerable, Action<T> action, Converter<T, string> customValueLogStringProvider = null, string errorLogFormat = DEFAULT_ERROR_LOG_FORMAT)
        {
            if (enumerable is null)
                throw new ArgumentNullException(nameof(enumerable));

            if (action is null)
                throw new ArgumentNullException(nameof(action));

            foreach (T value in enumerable)
            {
                try
                {
                    action(value);
                }
                catch (Exception ex)
                {
                    string valueLogString;
                    if (value is null)
                    {
                        valueLogString = "null";
                    }
                    else if (customValueLogStringProvider != null)
                    {
                        valueLogString = customValueLogStringProvider(value);
                    }
                    else
                    {
                        valueLogString = value.ToString();
                    }

                    Log.Error_NoCallerPrefix(string.Format(errorLogFormat, valueLogString, ex));
                }
            }
        }

        public static bool CountGreaterThan<T>(this IEnumerable<T> enumerable, int count)
        {
            count = Math.Max(0, count);

            if (enumerable is ICollection<T> collectionT)
            {
                return collectionT.Count > count;
            }
            else if (enumerable is ICollection collection)
            {
                return collection.Count > count;
            }
            else
            {
                return enumerable.Skip(count).Any();
            }
        }

        public static bool CountGreaterThanOrEqualTo<T>(this IEnumerable<T> collection, int count)
        {
            return collection.CountGreaterThan(count - 1);
        }

        public static bool CountLessThan<T>(this IEnumerable<T> collection, int count)
        {
            return !collection.CountGreaterThanOrEqualTo(count);
        }

        public static bool CountLessThanOrEqualTo<T>(this IEnumerable<T> collection, int count)
        {
            return !collection.CountGreaterThan(count);
        }
    }
}
