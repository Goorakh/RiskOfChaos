using RiskOfChaos.Utilities;
using System;
using UnityEngine;
using UnityEngine.Networking;

namespace RiskOfChaos.Components.MaterialInterpolation
{
    public sealed class NetworkedMaterialPropertyInterpolators : NetworkBehaviour, IMaterialPropertyInterpolator
    {
        public readonly SyncListMaterialPropertyInterpolationData PropertyInterpolations = [];

        public void SetValues(Material material, float interpolationFraction)
        {
            foreach (MaterialPropertyInterpolationData property in PropertyInterpolations)
            {
                int interpolateInt(in MaterialPropertyInterpolationData.ValuePair<int> pair)
                {
                    if (interpolationFraction <= 0f)
                        return pair.Start;

                    if (interpolationFraction >= 1f)
                        return pair.End;

                    return Mathf.RoundToInt(Mathf.Lerp(pair.Start, pair.End, interpolationFraction));
                }

                float interpolateFloat(in MaterialPropertyInterpolationData.ValuePair<float> pair)
                {
                    if (interpolationFraction <= 0f)
                        return pair.Start;

                    if (interpolationFraction >= 1f)
                        return pair.End;

                    return Mathf.Lerp(pair.Start, pair.End, Ease.InOutQuad(interpolationFraction));
                }

                Color interpolateColor(in MaterialPropertyInterpolationData.ValuePair<Color> pair)
                {
                    if (interpolationFraction <= 0f)
                        return pair.Start;

                    if (interpolationFraction >= 1f)
                        return pair.End;

                    Color.RGBToHSV(pair.Start, out float startH, out float startS, out float startV);
                    Color.RGBToHSV(pair.End, out float endH, out float endS, out float endV);

                    float t = Ease.InOutQuad(interpolationFraction);

                    float h = Mathf.LerpAngle(startH, endH, t);
                    float s = Mathf.Lerp(startS, endS, t);
                    float v = Mathf.Lerp(startV, endV, t);

                    return Color.HSVToRGB(h, s, v);
                }

                Vector4 interpolateVector(in MaterialPropertyInterpolationData.ValuePair<Vector4> pair)
                {
                    if (interpolationFraction <= 0f)
                        return pair.Start;

                    if (interpolationFraction >= 1f)
                        return pair.End;

                    return Vector4.Lerp(pair.Start, pair.End, Ease.InOutQuad(interpolationFraction));
                }

                Matrix4x4 interpolateMatrix(in MaterialPropertyInterpolationData.ValuePair<Matrix4x4> pair)
                {
                    return interpolationFraction < 0.5f ? pair.Start : pair.End;
                }

                float[] interpolateFloatArray(MaterialPropertyInterpolationData.ValuePair<float>[] pairs)
                {
                    if (pairs.Length == 0)
                        return [];

                    float[] results = new float[pairs.Length];

                    for (int i = 0; i < pairs.Length; i++)
                    {
                        results[i] = interpolateFloat(pairs[i]);
                    }

                    return results;
                }

                Color[] interpolateColorArray(MaterialPropertyInterpolationData.ValuePair<Color>[] pairs)
                {
                    if (pairs.Length == 0)
                        return [];

                    Color[] results = new Color[pairs.Length];

                    for (int i = 0; i < pairs.Length; i++)
                    {
                        results[i] = interpolateColor(pairs[i]);
                    }

                    return results;
                }

                Vector4[] interpolateVectorArray(MaterialPropertyInterpolationData.ValuePair<Vector4>[] pairs)
                {
                    if (pairs.Length == 0)
                        return [];

                    Vector4[] results = new Vector4[pairs.Length];

                    for (int i = 0; i < pairs.Length; i++)
                    {
                        results[i] = interpolateVector(pairs[i]);
                    }

                    return results;
                }

                Matrix4x4[] interpolateMatrixArray(MaterialPropertyInterpolationData.ValuePair<Matrix4x4>[] pairs)
                {
                    if (pairs.Length == 0)
                        return [];

                    Matrix4x4[] results = new Matrix4x4[pairs.Length];

                    for (int i = 0; i < pairs.Length; i++)
                    {
                        results[i] = interpolateMatrix(pairs[i]);
                    }

                    return results;
                }

                switch (property.PropertyType)
                {
                    case MaterialPropertyType.Integer:
                        material.SetInteger(property.PropertyNameId, interpolateInt(property.Value.Integer));
                        break;
                    case MaterialPropertyType.Float:
                        material.SetFloat(property.PropertyNameId, interpolateFloat(property.Value.Float));
                        break;
                    case MaterialPropertyType.Color:
                        material.SetColor(property.PropertyNameId, interpolateColor(property.Value.Color));
                        break;
                    case MaterialPropertyType.Vector:
                        material.SetVector(property.PropertyNameId, interpolateVector(property.Value.Vector));
                        break;
                    case MaterialPropertyType.Matrix:
                        material.SetMatrix(property.PropertyNameId, interpolateMatrix(property.Value.Matrix));
                        break;
                    case MaterialPropertyType.FloatArray:
                        material.SetFloatArray(property.PropertyNameId, interpolateFloatArray(property.Value.FloatArray));
                        break;
                    case MaterialPropertyType.ColorArray:
                        material.SetColorArray(property.PropertyNameId, interpolateColorArray(property.Value.ColorArray));
                        break;
                    case MaterialPropertyType.VectorArray:
                        material.SetVectorArray(property.PropertyNameId, interpolateVectorArray(property.Value.VectorArray));
                        break;
                    case MaterialPropertyType.MatrixArray:
                        material.SetMatrixArray(property.PropertyNameId, interpolateMatrixArray(property.Value.MatrixArray));
                        break;
                    default:
                        throw new NotImplementedException($"Property type {property.PropertyType} is not implemented");
                }
            }
        }
    }
}
