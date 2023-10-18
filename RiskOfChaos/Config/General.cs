using BepInEx.Configuration;
using RiskOfChaos.ConfigHandling;
using RiskOfOptions.OptionConfigs;
using UnityEngine;

namespace RiskOfChaos
{
    partial class Configs
    {
        public static class General
        {
            public const string SECTION_NAME = "General";

            public static readonly ConfigHolder<bool> DisableEffectDispatching =
                ConfigFactory<bool>.CreateConfig("Disable Effect Activation", false)
                                   .Description("If effect activation should be disabled completely")
                                   .OptionConfig(new CheckBoxConfig())
                                   .Build();

            static bool effectDispatchingDisabled() => DisableEffectDispatching.Value;

            const float TIME_BETWEEN_EFFECTS_MIN_VALUE = 5f;
            public static readonly ConfigHolder<float> TimeBetweenEffects =
                ConfigFactory<float>.CreateConfig("Effect Timer", 60f)
                                    .Description("How often new effects should happen")
                                    .OptionConfig(new StepSliderConfig
                                    {
                                        checkIfDisabled = effectDispatchingDisabled,
                                        formatString = "{0:F0}s",
                                        increment = 5f,
                                        min = TIME_BETWEEN_EFFECTS_MIN_VALUE,
                                        max = 60f * 5f
                                    })
                                    .ValueConstrictor(CommonValueConstrictors.GreaterThanOrEqualTo(TIME_BETWEEN_EFFECTS_MIN_VALUE))
                                    .Build();

            public static readonly ConfigHolder<bool> RunEffectsTimerWhileRunTimerPaused =
                ConfigFactory<bool>.CreateConfig("Dispatch Effects While Timer Paused", true)
                                   .Description("If the mod should activate effects while the run timer is paused (in Bazaar, Gilded Coast, etc.)")
                                   .OptionConfig(new CheckBoxConfig
                                   {
                                       checkIfDisabled = effectDispatchingDisabled
                                   })
                                   .Build();

            public static readonly ConfigHolder<bool> SeededEffectSelection =
                ConfigFactory<bool>.CreateConfig("Seeded Effect Selection", false)
                                   .Description("If the effects should be consistent with the run seed, only really changes anything if you're setting run seeds manually")
                                   .OptionConfig(new CheckBoxConfig
                                   {
                                       checkIfDisabled = effectDispatchingDisabled
                                   })
                                   .Build();

            internal static void Bind(ConfigFile file)
            {
                void bindConfig<T>(ConfigHolder<T> config)
                {
                    config.Bind(file, SECTION_NAME, CONFIG_GUID, CONFIG_NAME);
                }

                bindConfig(DisableEffectDispatching);

                bindConfig(TimeBetweenEffects);

                bindConfig(RunEffectsTimerWhileRunTimerPaused);

                bindConfig(SeededEffectSelection);
            }
        }
    }
}
