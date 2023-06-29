using BepInEx.Configuration;
using RiskOfOptions;
using RiskOfOptions.Options;
using System;

namespace RiskOfChaos
{
#if DEBUG
    partial class Configs
    {
        public static class Debug
        {
            static ConfigEntry<bool> _useLocalhostConnect;
            const bool USE_LOCALHOST_CONNECT_DEFAULT_VALUE = false;
            public static bool UseLocalhostConnect => _useLocalhostConnect?.Value ?? USE_LOCALHOST_CONNECT_DEFAULT_VALUE;

            internal static void Init(ConfigFile file)
            {
                const string SECTION_NAME = "Debug";

                _useLocalhostConnect = file.Bind(new ConfigDefinition(SECTION_NAME, "Use Localhost Connect"), USE_LOCALHOST_CONNECT_DEFAULT_VALUE);
                ModSettingsManager.AddOption(new CheckBoxOption(_useLocalhostConnect), CONFIG_GUID, CONFIG_NAME);

                On.RoR2.Networking.NetworkManagerSystem.ClientSendAuth += (orig, self, conn) =>
                {
                    if (UseLocalhostConnect)
                        return;

                    orig(self, conn);
                };

                // Connecting to localhost this way makes entitlements not work, so just force them all to be enabled
                On.RoR2.PlayerCharacterMasterControllerEntitlementTracker.HasEntitlement += (orig, self, entitlementDef) =>
                {
                    return UseLocalhostConnect || orig(self, entitlementDef);
                };
            }
        }
    }
#endif
}
