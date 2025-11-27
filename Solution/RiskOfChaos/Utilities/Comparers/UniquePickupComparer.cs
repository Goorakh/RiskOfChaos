using RoR2;
using System;
using System.Collections.Generic;

namespace RiskOfChaos.Utilities.Comparers
{
    public sealed class UniquePickupComparer : IEqualityComparer<UniquePickup>
    {
        public static UniquePickupComparer PickupIndexComparer { get; } = new UniquePickupComparer(PickupPropertyFlags.PickupIndex);

        public static UniquePickupComparer FullComparer { get; } = new UniquePickupComparer(PickupPropertyFlags.All);

        readonly PickupPropertyFlags _compareProperties;

        public UniquePickupComparer(PickupPropertyFlags compareProperties)
        {
            _compareProperties = compareProperties;
        }

        bool IEqualityComparer<UniquePickup>.Equals(UniquePickup x, UniquePickup y)
        {
            if ((_compareProperties & PickupPropertyFlags.PickupIndex) != 0 && x.pickupIndex != y.pickupIndex)
                return false;

            if ((_compareProperties & PickupPropertyFlags.DecayValue) != 0 && x.decayValue != y.decayValue)
                return false;

            if ((_compareProperties & PickupPropertyFlags.UpgradeValue) != 0 && x.upgradeValue != y.upgradeValue)
                return false;

            return true;
        }

        int IEqualityComparer<UniquePickup>.GetHashCode(UniquePickup pickup)
        {
            HashCode hashCode = new HashCode();

            if ((_compareProperties & PickupPropertyFlags.PickupIndex) != 0)
                hashCode.Add(pickup.pickupIndex);

            if ((_compareProperties & PickupPropertyFlags.DecayValue) != 0)
                hashCode.Add(pickup.decayValue);

            if ((_compareProperties & PickupPropertyFlags.UpgradeValue) != 0)
                hashCode.Add(pickup.upgradeValue);

            return hashCode.ToHashCode();
        }

        [Flags]
        public enum PickupPropertyFlags
        {
            None,
            PickupIndex = 1 << 0,
            DecayValue = 1 << 1,
            UpgradeValue = 1 << 2,
            All = ~0b0
        }
    }
}
