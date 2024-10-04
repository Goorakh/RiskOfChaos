using RoR2;
using System;
using System.Runtime.CompilerServices;

namespace RiskOfChaos.Networking.Wrappers
{
    internal struct Net_RunFixedTimeStampWrapper : IEquatable<Net_RunFixedTimeStampWrapper>
    {
        public float t;

        public Net_RunFixedTimeStampWrapper(Run.FixedTimeStamp timeStamp)
        {
            t = timeStamp.t;
        }

        public readonly bool Equals(Net_RunFixedTimeStampWrapper other)
        {
            return t == other.t;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator Run.FixedTimeStamp(Net_RunFixedTimeStampWrapper wrapper)
        {
            return new Run.FixedTimeStamp(wrapper.t);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator Net_RunFixedTimeStampWrapper(Run.FixedTimeStamp timeStamp)
        {
            return new Net_RunFixedTimeStampWrapper(timeStamp);
        }

        public override readonly string ToString()
        {
            return t.ToString();
        }
    }
}
