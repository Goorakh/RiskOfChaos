using RoR2;
using System;
using System.Runtime.CompilerServices;

namespace RiskOfChaos.Networking.Wrappers
{
    internal struct CostTypeIndexWrapper : IEquatable<CostTypeIndexWrapper>
    {
        public int Value;

        CostTypeIndexWrapper(int value)
        {
            Value = value;
        }

        public readonly bool Equals(CostTypeIndexWrapper other)
        {
            return Value == other.Value;
        }

        public override readonly string ToString()
        {
            return ((CostTypeIndex)Value).ToString();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator CostTypeIndex(CostTypeIndexWrapper wrapper)
        {
            return (CostTypeIndex)wrapper.Value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator CostTypeIndexWrapper(CostTypeIndex costTypeIndex)
        {
            return new CostTypeIndexWrapper((int)costTypeIndex);
        }
    }
}
