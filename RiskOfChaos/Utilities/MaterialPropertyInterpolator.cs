using System;
using System.Collections.Generic;
using UnityEngine;

namespace RiskOfChaos.Utilities
{
    public class MaterialPropertyInterpolator
    {
        delegate object InterpolationDelegate(float fraction);
        delegate void SetMaterialValueDelegate(Material material, object value);

        readonly record struct PropertyInterpolationInfo(int PropertyID, object Start, object End, InterpolationDelegate InterpolationFunc, SetMaterialValueDelegate SetMaterialValueFunc);

        readonly Dictionary<int, PropertyInterpolationInfo> _propertyInterpolations = [];

        public void SetPropertiesInterpolation(float fraction, Material targetMaterial)
        {
            foreach (PropertyInterpolationInfo property in _propertyInterpolations.Values)
            {
                property.SetMaterialValueFunc(targetMaterial, property.InterpolationFunc(fraction));
            }
        }

        void registerInterpolation(PropertyInterpolationInfo target)
        {
            _propertyInterpolations[target.PropertyID] = target;
        }

        public void SetColor(int propertyID, Color startValue, Color endValue)
        {
            registerInterpolation(new PropertyInterpolationInfo(propertyID, startValue, endValue, (fraction) =>
            {
                return Color.Lerp(startValue, endValue, fraction);
            }, (material, value) =>
            {
                material.SetColor(propertyID, (Color)value);
            }));
        }

        public void SetColorArray(int propertyID, Color[] startValue, Color[] endValue)
        {
            if (startValue is null)
                throw new ArgumentNullException(nameof(startValue));

            if (endValue is null)
                throw new ArgumentNullException(nameof(endValue));

            if (startValue.Length != endValue.Length)
                throw new ArgumentException("start and end lengths do not match");

            registerInterpolation(new PropertyInterpolationInfo(propertyID, startValue, endValue, (fraction) =>
            {
                return interpolateArray(startValue, endValue, fraction, Color.Lerp);
            }, (material, value) =>
            {
                material.SetColorArray(propertyID, (Color[])value);
            }));
        }

        public void SetFloat(int propertyID, float startValue, float endValue)
        {
            registerInterpolation(new PropertyInterpolationInfo(propertyID, startValue, endValue, (fraction) =>
            {
                return Mathf.Lerp(startValue, endValue, fraction);
            }, (material, value) =>
            {
                material.SetFloat(propertyID, (float)value);
            }));
        }

        public void SetFloatArray(int propertyID, float[] startValue, float[] endValue)
        {
            if (startValue is null)
                throw new ArgumentNullException(nameof(startValue));

            if (endValue is null)
                throw new ArgumentNullException(nameof(endValue));

            if (startValue.Length != endValue.Length)
                throw new ArgumentException("start and end lengths do not match");

            registerInterpolation(new PropertyInterpolationInfo(propertyID, startValue, endValue, (fraction) =>
            {
                return interpolateArray(startValue, endValue, fraction, Mathf.Lerp);
            }, (material, value) =>
            {
                material.SetFloatArray(propertyID, (float[])value);
            }));
        }

        public void SetInt(int propertyID, int startValue, int endValue)
        {
            registerInterpolation(new PropertyInterpolationInfo(propertyID, startValue, endValue, (fraction) =>
            {
                return Mathf.RoundToInt(Mathf.Lerp(startValue, endValue, fraction));
            }, (material, value) =>
            {
                material.SetInt(propertyID, (int)value);
            }));
        }

        public void SetTextureOffset(int propertyID, Vector2 startValue, Vector2 endValue)
        {
            registerInterpolation(new PropertyInterpolationInfo(propertyID, startValue, endValue, (fraction) =>
            {
                return Vector2.Lerp(startValue, endValue, fraction);
            }, (material, value) =>
            {
                material.SetTextureOffset(propertyID, (Vector2)value);
            }));
        }

        public void SetTextureScale(int propertyID, Vector2 startValue, Vector2 endValue)
        {
            registerInterpolation(new PropertyInterpolationInfo(propertyID, startValue, endValue, (fraction) =>
            {
                return Vector2.Lerp(startValue, endValue, fraction);
            }, (material, value) =>
            {
                material.SetTextureScale(propertyID, (Vector2)value);
            }));
        }

        public void SetVector(int propertyID, Vector4 startValue, Vector4 endValue)
        {
            registerInterpolation(new PropertyInterpolationInfo(propertyID, startValue, endValue, (fraction) =>
            {
                return Vector4.Lerp(startValue, endValue, fraction);
            }, (material, value) =>
            {
                material.SetVector(propertyID, (Vector4)value);
            }));
        }

        public void SetVectorArray(int propertyID, Vector4[] startValue, Vector4[] endValue)
        {
            if (startValue is null)
                throw new ArgumentNullException(nameof(startValue));

            if (endValue is null)
                throw new ArgumentNullException(nameof(endValue));

            if (startValue.Length != endValue.Length)
                throw new ArgumentException("start and end lengths do not match");

            registerInterpolation(new PropertyInterpolationInfo(propertyID, startValue, endValue, (fraction) =>
            {
                return interpolateArray(startValue, endValue, fraction, Vector4.Lerp);
            }, (material, value) =>
            {
                material.SetVectorArray(propertyID, (Vector4[])value);
            }));
        }

        static T[] interpolateArray<T>(T[] start, T[] end, float fraction, Func<T, T, float, T> interpolationFunc)
        {
            if (fraction <= 0f)
            {
                return start;
            }
            else if (fraction >= 1f)
            {
                return end;
            }
            else
            {
                T[] result = new T[start.Length];

                for (int i = 0; i < result.Length; i++)
                {
                    result[i] = interpolationFunc(start[i], end[i], fraction);
                }

                return result;
            }
        }
    }
}
