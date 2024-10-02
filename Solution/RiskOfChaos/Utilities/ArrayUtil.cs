using System;
using System.Collections.Generic;
using System.Linq;

namespace RiskOfChaos.Utilities
{
    public static class ArrayUtil
    {
        public static void AppendRange<T>(ref T[] array, T[] appended)
        {
            if (appended is null || appended.Length == 0)
                return;

            if (array is null || array.Length == 0)
            {
                array = appended;
                return;
            }

            int oldLength = array.Length;
            Array.Resize(ref array, oldLength + appended.Length);
            Array.Copy(appended, 0, array, oldLength, appended.Length);
        }

        public static void AppendRange<T>(ref T[] array, ICollection<T> appended)
        {
            if (appended is null)
                return;

            int appendedCount = appended.Count;
            if (appendedCount <= 0)
                return;

            if (array is null || array.Length == 0)
            {
                array = new T[appendedCount];
                appended.CopyTo(array, 0);
                return;
            }

            int oldLength = array.Length;
            Array.Resize(ref array, oldLength + appendedCount);
            appended.CopyTo(array, oldLength);
        }

        public static bool ElementsEqual<T>(T[] a, T[] b, IEqualityComparer<T> comparer = null)
        {
            comparer ??= EqualityComparer<T>.Default;

            if (a is null || b is null)
                return a is null && b is null;

            if (a.Length != b.Length)
                return false;

            for (int i = 0; i < a.Length; i++)
            {
                if (!comparer.Equals(a[i], b[i]))
                    return false;
            }

            return true;
        }
    }
}
