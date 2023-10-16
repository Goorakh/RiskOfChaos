using RoR2;
using System;

namespace RiskOfChaos.Utilities.DropTables
{
    public class SequentialPickupDropTable : PickupDropTable
    {
        int _currentPickupIndex;
        public PickupIndex[] Pickups = Array.Empty<PickupIndex>();

        PickupIndex getNextPickup()
        {
            return Pickups.Length > 0 ? Pickups[_currentPickupIndex++ % Pickups.Length] : PickupIndex.none;
        }

        public override PickupIndex GenerateDropPreReplacement(Xoroshiro128Plus rng)
        {
            return getNextPickup();
        }

        public override PickupIndex[] GenerateUniqueDropsPreReplacement(int maxDrops, Xoroshiro128Plus rng)
        {
            maxDrops = Math.Min(maxDrops, Pickups.Length);

            PickupIndex[] result = new PickupIndex[maxDrops];

            for (int i = 0; i < maxDrops; i++)
            {
                result[i] = getNextPickup();
            }

            return result;
        }

        public override int GetPickupCount()
        {
            return Pickups.Length;
        }
    }
}
