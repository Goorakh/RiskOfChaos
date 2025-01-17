using BepInEx.Configuration;
using RiskOfChaos.ConfigHandling;
using RiskOfChaos.ConfigHandling.AcceptableValues;
using RiskOfOptions.OptionConfigs;
using System.Runtime.CompilerServices;

namespace RiskOfChaos
{
    partial class Configs
    {
        public static class General
        {
            public const string SECTION_NAME = "General";

            public static readonly ConfigHolder<bool> DisableEffectDispatching =
                ConfigFactory<bool>.CreateConfig("Disable Effect Activation", false)
                                   .Description("If timer-based effect activation should be disabled completely")
                                   .OptionConfig(new CheckBoxConfig())
                                   .Networked()
                                   .Build();

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            static bool effectDispatchingDisabled()
            {
                return DisableEffectDispatching.LocalValue;
            }

            const float TIME_BETWEEN_EFFECTS_MIN_VALUE = 5f;
            public static readonly ConfigHolder<float> TimeBetweenEffects =
                ConfigFactory<float>.CreateConfig("Effect Timer", 60f)
                                    .Description("How often new effects should happen")
                                    .AcceptableValues(new AcceptableValueMin<float>(TIME_BETWEEN_EFFECTS_MIN_VALUE))
                                    .OptionConfig(new FloatFieldConfig
                                    {
                                        FormatString = "{0}s",
                                        Min = TIME_BETWEEN_EFFECTS_MIN_VALUE,
                                        checkIfDisabled = effectDispatchingDisabled,
                                    })
                                    .Build();

            public static readonly ConfigHolder<bool> RunEffectsTimerWhileRunTimerPaused =
                ConfigFactory<bool>.CreateConfig("Dispatch Effects While Timer Paused", true)
                                   .Description("If the mod should activate effects while the run timer is paused (in Bazaar, Gilded Coast, etc.)")
                                   .OptionConfig(new CheckBoxConfig
                                   {
                                       checkIfDisabled = effectDispatchingDisabled
                                   })
                                   .Networked()
                                   .Build();

            internal static void Bind(ConfigFile file)
            {
                void bindConfig(ConfigHolderBase config)
                {
                    config.Bind(file, SECTION_NAME, CONFIG_GUID, CONFIG_NAME);
                }

                bindConfig(DisableEffectDispatching);

                bindConfig(TimeBetweenEffects);

                bindConfig(RunEffectsTimerWhileRunTimerPaused);
            }
        }
    }
}
