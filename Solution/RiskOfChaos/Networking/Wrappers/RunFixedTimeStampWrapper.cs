using RoR2;
using System;

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

        public static implicit operator Run.FixedTimeStamp(Net_RunFixedTimeStampWrapper wrapper)
        {
            return new Run.FixedTimeStamp(wrapper.t);
        }

        public static implicit operator Net_RunFixedTimeStampWrapper(Run.FixedTimeStamp timeStamp)
        {
            return new Net_RunFixedTimeStampWrapper(timeStamp);
        }
    }
}
