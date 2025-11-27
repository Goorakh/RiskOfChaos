using RoR2;
using System;

namespace RiskOfChaos.Networking
{
    internal readonly struct UniquePickupWrapper : IEquatable<UniquePickupWrapper>
    {
        public readonly int PickupIndexValue;

        public readonly float DecayValue;

        public readonly int UpgradeValue;

        UniquePickupWrapper(UniquePickup uniquePickup)
        {
            PickupIndexValue = uniquePickup.pickupIndex.value;
            DecayValue = uniquePickup.decayValue;
            UpgradeValue = uniquePickup.upgradeValue;
        }

        bool IEquatable<UniquePickupWrapper>.Equals(UniquePickupWrapper other)
        {
            return ((UniquePickup)this).Equals(other);
        }

        public static implicit operator UniquePickup(in UniquePickupWrapper wrapper)
        {
            return new UniquePickup
            {
                pickupIndex = new PickupIndex(wrapper.PickupIndexValue),
                decayValue = wrapper.DecayValue,
                upgradeValue = wrapper.UpgradeValue
            };
        }

        public static implicit operator UniquePickupWrapper(in UniquePickup uniquePickup)
        {
            return new UniquePickupWrapper(uniquePickup);
        }
    }
}
