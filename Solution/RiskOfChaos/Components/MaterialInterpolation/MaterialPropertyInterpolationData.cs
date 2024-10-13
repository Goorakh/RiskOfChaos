using HG;
using RoR2;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Networking;

namespace RiskOfChaos.Components.MaterialInterpolation
{
    public readonly struct MaterialPropertyInterpolationData : IEquatable<MaterialPropertyInterpolationData>
    {
        public readonly string PropertyName;
        public readonly int PropertyNameId;
        public readonly MaterialPropertyType PropertyType;

        public readonly ValuePairUnion Value;

        public MaterialPropertyInterpolationData(string propertyName, MaterialPropertyType propertyType, ValuePairUnion value)
        {
            PropertyName = propertyName;
            PropertyNameId = Shader.PropertyToID(PropertyName);
            PropertyType = propertyType;
            Value = value;
        }

        public MaterialPropertyInterpolationData(string propertyName, in ValuePair<int> intPair) : this(propertyName,
                                                                                                        MaterialPropertyType.Integer,
                                                                                                        new ValuePairUnion { Integer = intPair })
        {
        }

        public MaterialPropertyInterpolationData(string propertyName, int start, int end) : this(propertyName, new ValuePair<int>(start, end))
        {
        }

        public MaterialPropertyInterpolationData(string propertyName, in ValuePair<float> floatPair) : this(propertyName,
                                                                                                            MaterialPropertyType.Float,
                                                                                                            new ValuePairUnion { Float = floatPair })
        {
        }

        public MaterialPropertyInterpolationData(string propertyName, float start, float end) : this(propertyName, new ValuePair<float>(start, end))
        {
        }

        public MaterialPropertyInterpolationData(string propertyName, in ValuePair<Color> colorPair) : this(propertyName,
                                                                                                            MaterialPropertyType.Color,
                                                                                                            new ValuePairUnion { Color = colorPair })
        {
        }

        public MaterialPropertyInterpolationData(string propertyName, in Color start, in Color end) : this(propertyName, new ValuePair<Color>(start, end))
        {
        }

        public MaterialPropertyInterpolationData(string propertyName, in ValuePair<Vector4> vectorPair) : this(propertyName,
                                                                                                               MaterialPropertyType.Vector,
                                                                                                               new ValuePairUnion { Vector = vectorPair })
        {
        }

        public MaterialPropertyInterpolationData(string propertyName, in Vector4 start, in Vector4 end) : this(propertyName, new ValuePair<Vector4>(start, end))
        {
        }

        public MaterialPropertyInterpolationData(string propertyName, in ValuePair<Matrix4x4> matrixPair) : this(propertyName,
                                                                                                                 MaterialPropertyType.Matrix,
                                                                                                                 new ValuePairUnion { Matrix = matrixPair })
        {
        }

        public MaterialPropertyInterpolationData(string propertyName, in Matrix4x4 start, in Matrix4x4 end) : this(propertyName, new ValuePair<Matrix4x4>(start, end))
        {
        }

        public MaterialPropertyInterpolationData(string propertyName, ValuePair<float>[] floatArray) : this(propertyName,
                                                                                                            MaterialPropertyType.FloatArray,
                                                                                                            new ValuePairUnion { FloatArray = floatArray })
        {
        }

        public MaterialPropertyInterpolationData(string propertyName, ValuePair<Color>[] colorArray) : this(propertyName,
                                                                                                            MaterialPropertyType.ColorArray,
                                                                                                            new ValuePairUnion { ColorArray = colorArray })
        {
        }

        public MaterialPropertyInterpolationData(string propertyName, ValuePair<Vector4>[] vectorArray) : this(propertyName,
                                                                                                               MaterialPropertyType.VectorArray,
                                                                                                               new ValuePairUnion { VectorArray = vectorArray })
        {
        }

        public MaterialPropertyInterpolationData(string propertyName, ValuePair<Matrix4x4>[] matrixArray) : this(propertyName,
                                                                                                                 MaterialPropertyType.MatrixArray,
                                                                                                                 new ValuePairUnion { MatrixArray = matrixArray })
        {
        }

        public readonly void Serialize(NetworkWriter writer)
        {
            writer.Write(PropertyName);
            writer.WritePackedIndex32((int)PropertyType);

            Value.Write(PropertyType, writer);
        }

        public static MaterialPropertyInterpolationData Deserialize(NetworkReader reader)
        {
            string propertyName = reader.ReadString();
            MaterialPropertyType propertyType = (MaterialPropertyType)reader.ReadPackedIndex32();
            ValuePairUnion value = ValuePairUnion.Read(propertyType, reader);

            return new MaterialPropertyInterpolationData(propertyName, propertyType, value);
        }

        public readonly bool Equals(MaterialPropertyInterpolationData other)
        {
            return PropertyType == other.PropertyType &&
                   string.Equals(PropertyName, other.PropertyName) &&
                   ValuePairUnion.Equals(Value, other.Value, PropertyType);
        }

        [StructLayout(LayoutKind.Explicit)]
        public struct ValuePairUnion
        {
            [FieldOffset(0)]
            public ValuePair<int> Integer;

            [FieldOffset(0)]
            public ValuePair<float> Float;

            [FieldOffset(0)]
            public ValuePair<Color> Color;

            [FieldOffset(0)]
            public ValuePair<Vector4> Vector;

            [FieldOffset(0)]
            public ValuePair<Matrix4x4> Matrix;

            [FieldOffset(0)]
            public ValuePair<float>[] FloatArray;

            [FieldOffset(0)]
            public ValuePair<Color>[] ColorArray;

            [FieldOffset(0)]
            public ValuePair<Vector4>[] VectorArray;

            [FieldOffset(0)]
            public ValuePair<Matrix4x4>[] MatrixArray;

            public readonly void Write(MaterialPropertyType propertyType, NetworkWriter writer)
            {
                switch (propertyType)
                {
                    case MaterialPropertyType.Integer:
                        writer.WritePackedUInt32((uint)Integer.Start);
                        writer.WritePackedUInt32((uint)Integer.End);
                        break;
                    case MaterialPropertyType.Float:
                        writer.Write(Float.Start);
                        writer.Write(Float.End);
                        break;
                    case MaterialPropertyType.Color:
                        writer.Write((Color32)Color.Start);
                        writer.Write((Color32)Color.End);
                        break;
                    case MaterialPropertyType.Vector:
                        writer.Write(Vector.Start);
                        writer.Write(Vector.End);
                        break;
                    case MaterialPropertyType.Matrix:
                        writer.Write(Matrix.Start);
                        writer.Write(Matrix.End);
                        break;
                    case MaterialPropertyType.FloatArray:
                        writer.WritePackedUInt32((uint)FloatArray.Length);
                        foreach (ValuePair<float> value in FloatArray)
                        {
                            writer.Write(value.Start);
                            writer.Write(value.End);
                        }

                        break;
                    case MaterialPropertyType.ColorArray:
                        writer.WritePackedUInt32((uint)ColorArray.Length);
                        foreach (ValuePair<Color> value in ColorArray)
                        {
                            writer.Write((Color32)value.Start);
                            writer.Write((Color32)value.End);
                        }

                        break;
                    case MaterialPropertyType.VectorArray:
                        writer.WritePackedUInt32((uint)VectorArray.Length);
                        foreach (ValuePair<Vector4> value in VectorArray)
                        {
                            writer.Write(value.Start);
                            writer.Write(value.End);
                        }

                        break;
                    case MaterialPropertyType.MatrixArray:
                        writer.WritePackedUInt32((uint)MatrixArray.Length);
                        foreach (ValuePair<Matrix4x4> value in MatrixArray)
                        {
                            writer.Write(value.Start);
                            writer.Write(value.End);
                        }

                        break;
                    default:
                        throw new NotImplementedException($"Property type {propertyType} is not implemented");
                }
            }

            public static ValuePairUnion Read(MaterialPropertyType propertyType, NetworkReader reader)
            {
                ValuePairUnion result = default;

                switch (propertyType)
                {
                    case MaterialPropertyType.Integer:
                        result.Integer = new ValuePair<int>((int)reader.ReadPackedUInt32(), (int)reader.ReadPackedUInt32());
                        break;
                    case MaterialPropertyType.Float:
                        result.Float = new ValuePair<float>(reader.ReadSingle(), reader.ReadSingle());
                        break;
                    case MaterialPropertyType.Color:
                        result.Color = new ValuePair<Color>(reader.ReadColor32(), reader.ReadColor32());
                        break;
                    case MaterialPropertyType.Vector:
                        result.Vector = new ValuePair<Vector4>(reader.ReadVector4(), reader.ReadVector4());
                        break;
                    case MaterialPropertyType.Matrix:
                        result.Matrix = new ValuePair<Matrix4x4>(reader.ReadMatrix4x4(), reader.ReadMatrix4x4());
                        break;
                    case MaterialPropertyType.FloatArray:
                        result.FloatArray = new ValuePair<float>[reader.ReadPackedUInt32()];

                        for (int i = 0; i < result.FloatArray.Length; i++)
                        {
                            result.FloatArray[i] = new ValuePair<float>(reader.ReadSingle(), reader.ReadSingle());
                        }

                        break;
                    case MaterialPropertyType.ColorArray:
                        result.ColorArray = new ValuePair<Color>[reader.ReadPackedUInt32()];

                        for (int i = 0; i < result.ColorArray.Length; i++)
                        {
                            result.ColorArray[i] = new ValuePair<Color>(reader.ReadColor32(), reader.ReadColor32());
                        }

                        break;
                    case MaterialPropertyType.VectorArray:
                        result.VectorArray = new ValuePair<Vector4>[reader.ReadPackedUInt32()];

                        for (int i = 0; i < result.VectorArray.Length; i++)
                        {
                            result.VectorArray[i] = new ValuePair<Vector4>(reader.ReadVector4(), reader.ReadVector4());
                        }

                        break;
                    case MaterialPropertyType.MatrixArray:
                        result.MatrixArray = new ValuePair<Matrix4x4>[reader.ReadPackedUInt32()];

                        for (int i = 0; i < result.MatrixArray.Length; i++)
                        {
                            result.MatrixArray[i] = new ValuePair<Matrix4x4>(reader.ReadMatrix4x4(), reader.ReadMatrix4x4());
                        }

                        break;
                    default:
                        throw new NotImplementedException($"Property type {propertyType} is not implemented");
                }

                return result;
            }

            public static bool Equals(in ValuePairUnion a, in ValuePairUnion b, MaterialPropertyType propertyType)
            {
                switch (propertyType)
                {
                    case MaterialPropertyType.Integer:
                        return a.Integer == b.Integer;
                    case MaterialPropertyType.Float:
                        return a.Float == b.Float;
                    case MaterialPropertyType.Color:
                        return a.Color == b.Color;
                    case MaterialPropertyType.Vector:
                        return a.Vector == b.Vector;
                    case MaterialPropertyType.Matrix:
                        return a.Matrix == b.Matrix;
                    case MaterialPropertyType.FloatArray:
                        return ArrayUtils.SequenceEquals(a.FloatArray, b.FloatArray);
                    case MaterialPropertyType.ColorArray:
                        return ArrayUtils.SequenceEquals(a.ColorArray, b.ColorArray);
                    case MaterialPropertyType.VectorArray:
                        return ArrayUtils.SequenceEquals(a.VectorArray, b.VectorArray);
                    case MaterialPropertyType.MatrixArray:
                        return ArrayUtils.SequenceEquals(a.MatrixArray, b.MatrixArray);
                    default:
                        throw new NotImplementedException($"Property type {propertyType} is not implemented");
                }
            }
        }

        public readonly struct ValuePair<T> : IEquatable<ValuePair<T>> where T : IEquatable<T>
        {
            public readonly T Start, End;

            public ValuePair(T start, T end)
            {
                Start = start;
                End = end;
            }

            public readonly override bool Equals(object obj)
            {
                return obj is ValuePair<T> pair && Equals(pair);
            }

            public readonly bool Equals(ValuePair<T> other)
            {
                return EqualityComparer<T>.Default.Equals(Start, other.Start) &&
                       EqualityComparer<T>.Default.Equals(End, other.End);
            }

            public readonly override int GetHashCode()
            {
                return HashCode.Combine(Start, End);
            }

            public static bool operator ==(in ValuePair<T> left, in ValuePair<T> right)
            {
                return left.Equals(right);
            }

            public static bool operator !=(in ValuePair<T> left, in ValuePair<T> right)
            {
                return !(left == right);
            }
        }
    }
}
