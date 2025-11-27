using RiskOfChaos.Utilities.Pool;
using RoR2;
using System;
using System.Collections.Generic;

namespace RiskOfChaos.Utilities.DropTables
{
    public sealed class SequentialPickupDropTable : PickupDropTable, IPooledObject
    {
        int _currentPickupIndex;
        public UniquePickup[] Pickups = [];

        void IPooledObject.ResetValues()
        {
            _currentPickupIndex = 0;
            Pickups = [];
        }

        UniquePickup getNextPickup()
        {
            return Pickups.Length > 0 ? Pickups[_currentPickupIndex++ % Pickups.Length] : UniquePickup.none;
        }

        public override UniquePickup GeneratePickupPreReplacement(Xoroshiro128Plus rng)
        {
            return getNextPickup();
        }

        public override void GenerateDistinctPickupsPreReplacement(List<UniquePickup> dest, int desiredCount, Xoroshiro128Plus rng)
        {
            int maxDrops = Math.Min(desiredCount, Pickups.Length);
            for (int i = 0; i < maxDrops; i++)
            {
                dest.Add(getNextPickup());
            }
        }

        public override int GetPickupCount()
        {
            return Pickups.Length;
        }
    }
}
