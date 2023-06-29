using System;
using System.Collections.Generic;

namespace RiskOfChaos.Utilities.Extensions
{
    public static class CollectionExtensions
    {
        public static void TryDo<T>(this IEnumerable<T> enumerable, Action<T> action, Converter<T, string> customValueLogStringProvider = null, string customErrorLog = null)
        {
            if (enumerable is null)
                throw new ArgumentNullException(nameof(enumerable));

            if (action is null)
                throw new ArgumentNullException(nameof(action));

            string errorLog = customErrorLog ?? "Failed to perform action for {0}: {1}";

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

                    Log.Error_NoCallerPrefix(string.Format(errorLog, valueLogString, ex));
                }
            }
        }
    }
}
