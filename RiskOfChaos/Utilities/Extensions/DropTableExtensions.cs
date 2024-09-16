using RoR2;

namespace RiskOfChaos.Utilities.Extensions
{
    public static class DropTableExtensions
    {
        public static void AddPickupIfMissing(this BasicPickupDropTable dropTable, PickupIndex pickup, float weight)
        {
            dropTable.selector.AddOrModifyWeight(pickup, weight);
        }
    }
}
