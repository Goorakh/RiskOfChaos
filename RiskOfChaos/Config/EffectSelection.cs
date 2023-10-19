using BepInEx.Configuration;
using RiskOfChaos.ConfigHandling;
using RiskOfOptions.OptionConfigs;

namespace RiskOfChaos
{
    partial class Configs
    {
        public static class EffectSelection
        {
            public const string SECTION_NAME = "Effect Selection";

            public static readonly ConfigHolder<bool> SeededEffectSelection =
                ConfigFactory<bool>.CreateConfig("Seeded Effect Selection", false)
                                   .Description("If the effects should be consistent with the run seed, only really changes anything if you're setting run seeds manually")
                                   .OptionConfig(new CheckBoxConfig())
                                   .MovedFrom(General.SECTION_NAME)
                                   .Build();

            internal static void Bind(ConfigFile file)
            {
                void bindConfig<T>(ConfigHolder<T> config)
                {
                    config.Bind(file, SECTION_NAME, CONFIG_GUID, CONFIG_NAME);
                }

                bindConfig(SeededEffectSelection);
            }
        }
    }
}
