namespace RiskOfChaos.Utilities
{
    public static class AdditionalResourceAvailability
    {
        public static ResourceAvailability BuffCatalog;

        public static ResourceAvailability MasterCatalog;

        public static ResourceAvailability PickupCatalog;

        internal static void InitHooks()
        {
            On.RoR2.BuffCatalog.Init += (orig) =>
            {
                orig();
                BuffCatalog.MakeAvailable();
            };

            On.RoR2.MasterCatalog.Init += (orig) =>
            {
                orig();
                MasterCatalog.MakeAvailable();
            };

            On.RoR2.PickupCatalog.SetEntries += (orig, entries) =>
            {
                orig(entries);
                PickupCatalog.MakeAvailable();
            };
        }
    }
}
