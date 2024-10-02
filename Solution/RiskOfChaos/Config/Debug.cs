using BepInEx.Configuration;
using RiskOfChaos.ConfigHandling;
using RiskOfOptions.OptionConfigs;

namespace RiskOfChaos
{
#if DEBUG
    partial class Configs
    {
        public static class Debug
        {
            public static ConfigHolder<bool> UseLocalhostConnect =
                ConfigFactory<bool>.CreateConfig("Use Localhost Connect", false)
                                   .OptionConfig(new CheckBoxConfig())
                                   .Build();

            internal static void Bind(ConfigFile file)
            {
                const string SECTION_NAME = "Debug";

                UseLocalhostConnect.Bind(file, SECTION_NAME, CONFIG_GUID, CONFIG_NAME);

                On.RoR2.Networking.NetworkManagerSystem.ClientSendAuth += (orig, self, conn) =>
                {
                    if (UseLocalhostConnect.Value)
                        return;

                    orig(self, conn);
                };

                // Connecting to localhost this way makes entitlements not work, so just force them all to be enabled
                On.RoR2.PlayerCharacterMasterControllerEntitlementTracker.HasEntitlement += (orig, self, entitlementDef) =>
                {
                    return UseLocalhostConnect.Value || orig(self, entitlementDef);
                };
            }
        }
    }
#endif
}
