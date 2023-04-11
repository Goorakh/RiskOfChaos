using RoR2;

namespace RiskOfChaos.Utilities.Extensions
{
    public static class DropTableExtensions
    {
        public static void AddPickupIfMissing(this BasicPickupDropTable dropTable, PickupIndex pickup, float weight)
        {
#pragma warning disable Publicizer001 // Accessing a member that was not originally public
            WeightedSelection<PickupIndex> selector = dropTable.selector;
#pragma warning restore Publicizer001 // Accessing a member that was not originally public

            selector.AddOrModifyWeight(pickup, weight);
        }
    }
}
