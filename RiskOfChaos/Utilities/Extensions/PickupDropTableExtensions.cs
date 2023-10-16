using RoR2;

namespace RiskOfChaos.Utilities.Extensions
{
    public static class PickupDropTableExtensions
    {
        public static void FinalizeManualSetup(this PickupDropTable dropTable)
        {
            if (Run.instance)
            {
#pragma warning disable Publicizer001 // Accessing a member that was not originally public
                dropTable.Regenerate(Run.instance);
#pragma warning restore Publicizer001 // Accessing a member that was not originally public
            }
        }
    }
}
