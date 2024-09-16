using RoR2;

namespace RiskOfChaos.Utilities.Extensions
{
    public static class PickupDropTableExtensions
    {
        public static void FinalizeManualSetup(this PickupDropTable dropTable)
        {
            if (Run.instance)
            {
                dropTable.Regenerate(Run.instance);
            }
        }
    }
}
