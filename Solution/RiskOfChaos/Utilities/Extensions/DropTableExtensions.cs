using RiskOfChaos.Utilities.Comparers;
using RoR2;

namespace RiskOfChaos.Utilities.Extensions
{
    public static class DropTableExtensions
    {
        public static void AddPickupIfMissing(this BasicPickupDropTable dropTable, UniquePickup pickup, float weight)
        {
            dropTable.selector.AddOrModifyWeight(pickup, weight, UniquePickupComparer.PickupIndexComparer);
        }
    }
}
