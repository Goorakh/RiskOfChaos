using System;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace RiskOfChaos.Networking.Wrappers
{
    internal struct NullableColorWrapper : IEquatable<NullableColorWrapper>
    {
        public bool HasValue;
        public Color Color;

        NullableColorWrapper(Color? color)
        {
            HasValue = color.HasValue;
            Color = color.GetValueOrDefault();
        }

        public readonly bool Equals(NullableColorWrapper other)
        {
            return HasValue == other.HasValue && (!HasValue || Color == other.Color);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator Color?(NullableColorWrapper value)
        {
            return value.HasValue ? value.Color : null;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator NullableColorWrapper(Color? color)
        {
            return new NullableColorWrapper(color);
        }
    }
}
