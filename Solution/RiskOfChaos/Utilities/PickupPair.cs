using RoR2;
using System;

namespace RiskOfChaos.Utilities
{
    public struct PickupPair : IEquatable<PickupPair>
    {
        public PickupIndex PickupA;
        public PickupIndex PickupB;

        public PickupPair(PickupIndex pickupA, PickupIndex pickupB)
        {
            PickupA = pickupA;
            PickupB = pickupB;
        }

        public override readonly bool Equals(object obj)
        {
            return obj is PickupPair pair && Equals(pair);
        }

        public readonly bool Equals(PickupPair other)
        {
            return PickupA == other.PickupA &&
                   PickupB == other.PickupB;
        }

        public override readonly int GetHashCode()
        {
            return HashCode.Combine(PickupA, PickupB);
        }

        public static bool operator ==(PickupPair left, PickupPair right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(PickupPair left, PickupPair right)
        {
            return !(left == right);
        }
    }
}
