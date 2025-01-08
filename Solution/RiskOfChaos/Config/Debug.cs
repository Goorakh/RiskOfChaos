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
            public const string SECTION_NAME = "Debug";

            public static readonly ConfigHolder<bool> EnableDebugVisuals =
                ConfigFactory<bool>.CreateConfig("Enable Debug Visualizations", false)
                                   .OptionConfig(new CheckBoxConfig())
                                   .Build();

            internal static void Bind(ConfigFile file)
            {
                void bindConfig(ConfigHolderBase config)
                {
                    config.Bind(file, SECTION_NAME, CONFIG_GUID, CONFIG_NAME);
                }

                bindConfig(EnableDebugVisuals);
            }
        }
    }
#endif
}
