using BepInEx.Configuration;
using RiskOfOptions;

namespace RiskOfChaos
{
    public static partial class Configs
    {
        const string CONFIG_GUID = $"RoC_Config_General";
        const string CONFIG_NAME = $"Risk of Chaos: General";

        internal static void Init(ConfigFile file)
        {
            General.Init(file);

            ChatVoting.Init(file);

#if DEBUG
            Debug.Init(file);
#endif

            // ModSettingsManager.SetModIcon(general_icon, GENERAL_GUID, GENERAL_NAME);
            ModSettingsManager.SetModDescription("General config options for Risk of Chaos", CONFIG_GUID, CONFIG_NAME);
        }
    }
}
