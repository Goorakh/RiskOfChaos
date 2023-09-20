using System;

namespace RiskOfChaos.Utilities
{
    public static class ArrayUtil
    {
        public static void AppendRange<T>(ref T[] array, T[] appended)
        {
            if (array is null)
                throw new ArgumentNullException(nameof(array));

            if (appended is null)
                throw new ArgumentNullException(nameof(appended));

            if (array.Length == 0)
            {
                array = appended;
                return;
            }

            if (appended.Length == 0)
                return;

            int oldLength = array.Length;
            Array.Resize(ref array, oldLength + appended.Length);
            Array.Copy(appended, 0, array, oldLength, appended.Length);
        }
    }
}
