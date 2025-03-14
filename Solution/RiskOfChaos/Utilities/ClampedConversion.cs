﻿using System.Runtime.CompilerServices;

namespace RiskOfChaos.Utilities
{
    public static class ClampedConversion
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Int32(uint value)
        {
            if (value > int.MaxValue)
                return int.MaxValue;

            // Comparison to integral constant is useless; the constant is outside the range of the type
            // if (value < int.MinValue)
            //     return int.MinValue;

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
            // Comparison to integral constant is useless; the constant is outside the range of the type
            // if (value > uint.MaxValue)
            //     return uint.MaxValue;

            if (value < uint.MinValue)
                return uint.MinValue;

            return (uint)value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint UInt32(long value)
        {
            if (value > uint.MaxValue)
                return uint.MaxValue;

            if (value < uint.MinValue)
                return uint.MinValue;

            return (uint)value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint UInt32(ulong value)
        {
            if (value > uint.MaxValue)
                return uint.MaxValue;

            // if (value < uint.MinValue)
            //     return uint.MinValue;

            return (uint)value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static sbyte Int8(int value)
        {
            if (value > sbyte.MaxValue)
                return sbyte.MaxValue;

            if (value < sbyte.MinValue)
                return sbyte.MinValue;

            return (sbyte)value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static byte UInt8(int value)
        {
            if (value > byte.MaxValue)
                return byte.MaxValue;

            if (value < byte.MinValue)
                return byte.MinValue;

            return (byte)value;
        }
    }
}
