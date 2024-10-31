using BepInEx.Configuration;
using RiskOfChaos.ConfigHandling;
using RiskOfChaos.EffectHandling;
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

            static bool perStageEffectListDisabled() => !PerStageEffectListEnabled.Value;

            public static readonly ConfigHolder<bool> PerStageEffectListEnabled =
                ConfigFactory<bool>.CreateConfig("Per-Stage Effect List", false)
                                   .Description("""
                                    If enabled, a subsection of all effects is generated each stage and only effects from this list are activated.
                                    Not supported in any chat voting mode
                                    """)
                                   .OptionConfig(new CheckBoxConfig())
                                   .Build();

            public static ConfigHolder<int> PerStageEffectListSize { get; private set; }

            internal static void Bind(ConfigFile file)
            {
                void bindConfig(ConfigHolderBase config)
                {
                    config.Bind(file, SECTION_NAME, CONFIG_GUID, CONFIG_NAME);
                }

                bindConfig(SeededEffectSelection);

                bindConfig(PerStageEffectListEnabled);

                ChaosEffectCatalog.Availability.CallWhenAvailable(() =>
                {
                    PerStageEffectListSize =
                        ConfigFactory<int>.CreateConfig("Effect List Size", 50)
                                          .Description("""
                                           The size of the per-stage effect list
                                           Not supported in any chat voting mode
                                           """)
                                          .AcceptableValues(new AcceptableValueRange<int>(1, ChaosEffectCatalog.EffectCount))
                                          .OptionConfig(new IntSliderConfig
                                          {
                                              min = 1,
                                              max = ChaosEffectCatalog.EffectCount,
                                              checkIfDisabled = perStageEffectListDisabled
                                          })
                                          .Build();

                    bindConfig(PerStageEffectListSize);
                });
            }
        }
    }
}
