using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace RiskOfChaos.Utilities
{
    public static class VectorUtils
    {
        static Vector3 cumulateVectorFunction(Vector3[] values, Func<Vector3, Vector3, Vector3> func)
        {
            if (values is null)
                throw new ArgumentNullException(nameof(values));

            if (values.Length == 0)
                throw new ArgumentException($"{nameof(values)} must have at least one element", nameof(values));

            if (values.Length == 1)
                return values[0];

            Vector3 result = func(values[0], values[1]);
            for (int i = 2; i < values.Length; i++)
            {
                result = func(result, values[i]);
            }

            return result;
        }

        public static Vector3 Min(params Vector3[] values)
        {
            return cumulateVectorFunction(values, Vector3.Min);
        }

        public static Vector3 Max(params Vector3[] values)
        {
            return cumulateVectorFunction(values, Vector3.Max);
        }
    }
}
