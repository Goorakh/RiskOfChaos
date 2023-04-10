using BepInEx.Configuration;
using RiskOfChaos.EffectHandling;
using RiskOfChaos.EffectHandling.Controllers;
using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Data;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Methods;
using RiskOfOptions.OptionConfigs;
using RiskOfOptions.Options;
using RoR2;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace RiskOfChaos.EffectDefinitions.World
{
    [ChaosEffect("advance_run_timer", ConfigName = "Advance Run Timer", EffectWeightReductionPercentagePerActivation = 15f, EffectRepetitionWeightCalculationMode = EffectActivationCountMode.PerRun)]
    public sealed class AdvanceRunTimer : BaseEffect
    {
        [InitEffectInfo]
        static readonly ChaosEffectInfo _effectInfo;

        const int NUM_MINUTES_TO_ADD_DEFAULT_VALUE = 5;
        static ConfigEntry<int> _numMinutesToAdd;

        static int numMinutesToAdd
        {
            get
            {
                if (_numMinutesToAdd == null)
                {
                    return NUM_MINUTES_TO_ADD_DEFAULT_VALUE;
                }
                else
                {
                    return Mathf.Max(_numMinutesToAdd.Value, 1);
                }
            }
        }

        static int numSecondsToAdd
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => numMinutesToAdd * 60;
        }

        [SystemInitializer(typeof(ChaosEffectCatalog))]
        static void InitConfig()
        {
            _numMinutesToAdd = Main.Instance.Config.Bind(new ConfigDefinition(_effectInfo.ConfigSectionName, "Minutes To Add"), NUM_MINUTES_TO_ADD_DEFAULT_VALUE, new ConfigDescription("The amount of minutes to advance the run timer by"));

            addConfigOption(new IntSliderOption(_numMinutesToAdd, new IntSliderConfig
            {
                min = 1,
                max = 30
            }));
        }

        [EffectNameFormatArgs]
        static object[] GetEffectNameFormatArgs()
        {
            return new object[] { numMinutesToAdd };
        }

        public override void OnStart()
        {
            Run run = Run.instance;
            run.SetRunStopwatch(run.GetRunStopwatch() + numSecondsToAdd);

            ChaosEffectDispatcher.Instance?.SkipAllScheduledEffects();
        }
    }
}
