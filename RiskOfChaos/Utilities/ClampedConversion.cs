using System.Runtime.CompilerServices;

#pragma warning disable CS0652 // Comparison to integral constant is useless; the constant is outside the range of the type

namespace RiskOfChaos.Utilities
{
    public static class ClampedConversion
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Int32(uint value)
        {
            if (value > int.MaxValue)
                return int.MaxValue;

            if (value < int.MinValue)
                return int.MinValue;

            return (int)value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Int32(long value)
        {
            if (value > int.MaxValue)
                return int.MaxValue;

            if (value < int.MinValue)
                return int.MinValue;

            return (int)value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint UInt32(int value)
        {
            if (value > uint.MaxValue)
                return uint.MaxValue;

            if (value < uint.MinValue)
                return uint.MinValue;

            return (uint)value;
        }
    }
}
