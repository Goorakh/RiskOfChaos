using RiskOfChaos.Utilities.Extensions;
using RiskOfChaos.Utilities.Pool;
using RoR2;
using System;
using System.Collections.Generic;
using System.Linq;

namespace RiskOfChaos.Utilities.DropTables
{
    public class CombinedSequentialPickupDropTable : PickupDropTable, IPooledObject
    {
        int _currentDropCount;

        public readonly record struct DropTableEntry(PickupDropTable DropTable, int Count);
        public DropTableEntry[] Entries = [];

        void IPooledObject.ResetValues()
        {
            _currentDropCount = 0;
            Entries = [];
        }

        DropTableEntry? getDropTableEntryForCount(int count)
        {
            foreach (DropTableEntry entry in Entries)
            {
                if (entry.Count <= 0)
                    continue;

                if (entry.Count > count)
                {
                    return entry;
                }
                else
                {
                    count -= entry.Count;
                }
            }

            if (Entries.Length > 0)
            {
                return Entries[Entries.Length - 1];
            }
            else
            {
                return null;
            }
        }

        public override void Regenerate(Run run)
        {
            base.Regenerate(run);

            foreach (DropTableEntry entry in Entries)
            {
                entry.DropTable.Regenerate(run);
            }
        }

        public override PickupIndex GenerateDropPreReplacement(Xoroshiro128Plus rng)
        {
            DropTableEntry? tableEntry = getDropTableEntryForCount(_currentDropCount++);
            if (!tableEntry.HasValue)
                return PickupIndex.none;

            return tableEntry.Value.DropTable.GenerateDropPreReplacement(rng);
        }

        public override PickupIndex[] GenerateUniqueDropsPreReplacement(int maxDrops, Xoroshiro128Plus rng)
        {
            List<PickupIndex> result = [];

            while (result.Count < maxDrops)
            {
                DropTableEntry? entry = getDropTableEntryForCount(_currentDropCount);
                if (!entry.HasValue)
                    break;

                int remainingDrops = maxDrops - result.Count;
                int remainingTableDrops = Math.Min(remainingDrops, entry.Value.Count);
                result.AddRange(entry.Value.DropTable.GenerateUniqueDropsPreReplacement(remainingTableDrops, rng.Branch()));
                _currentDropCount += remainingTableDrops;
            }

            return result.ToArray();
        }

        public override int GetPickupCount()
        {
            return Entries.Sum(e => e.Count);
        }
    }
}
