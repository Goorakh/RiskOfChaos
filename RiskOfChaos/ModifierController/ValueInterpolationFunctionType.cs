using System;
using UnityEngine;

namespace RiskOfChaos.ModifierController
{
    public enum ValueInterpolationFunctionType : byte
    {
        Snap,
        Linear,
        EaseInOut
    }

    public static class ValueBlendExtensions
    {
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

        public static uint Interpolate(this ValueInterpolationFunctionType type, uint a, uint b, float t)
        {
            return (uint)Mathf.Round(type.Interpolate((float)a, (float)b, t));
        }

        public static int Interpolate(this ValueInterpolationFunctionType type, int a, int b, float t)
        {
            return (int)Mathf.Round(type.Interpolate((float)a, (float)b, t));
        }

        public static Vector3 Interpolate(this ValueInterpolationFunctionType type, in Vector3 a, in Vector3 b, float t)
        {
            return type switch
            {
                ValueInterpolationFunctionType.Snap => b,
                ValueInterpolationFunctionType.Linear => Vector3.Lerp(a, b, t),
                ValueInterpolationFunctionType.EaseInOut => Vector3.Slerp(a, b, t),
                _ => throw new NotImplementedException($"Blend type {type} not implemented"),
            };
        }

        public static Quaternion Interpolate(this ValueInterpolationFunctionType type, in Quaternion a, in Quaternion b, float t)
        {
            return type switch
            {
                ValueInterpolationFunctionType.Snap => b,
                ValueInterpolationFunctionType.Linear => Quaternion.Lerp(a, b, t),
                ValueInterpolationFunctionType.EaseInOut => Quaternion.Slerp(a, b, t),
                _ => throw new NotImplementedException($"Blend type {type} not implemented")
            };
        }
    }
}
