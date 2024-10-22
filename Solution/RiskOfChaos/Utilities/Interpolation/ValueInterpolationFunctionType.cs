using System;
using UnityEngine;

namespace RiskOfChaos.Utilities.Interpolation
{
    [Obsolete]
    public enum ValueInterpolationFunctionType : byte
    {
        Snap,
        Linear,
        EaseInOut
    }

    [Obsolete]
    public static class ValueBlendExtensions
    {
        [Obsolete]
        public static float Interpolate(this ValueInterpolationFunctionType type, float a, float b, float t)
        {
            return type switch
            {
                ValueInterpolationFunctionType.Snap => b,
                ValueInterpolationFunctionType.Linear => Mathf.Lerp(a, b, t),
                ValueInterpolationFunctionType.EaseInOut => Mathf.SmoothStep(a, b, t),
                _ => throw new NotImplementedException($"Blend type {type} not implemented"),
            };
        }
    }
}
