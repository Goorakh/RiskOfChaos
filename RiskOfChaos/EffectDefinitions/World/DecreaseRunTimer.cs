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
    [ChaosEffect("decrease_run_timer", ConfigName = "Rewind Run Timer", DefaultSelectionWeight = 0.8f, EffectWeightReductionPercentagePerActivation = 15f, EffectRepetitionWeightCalculationMode = EffectActivationCountMode.PerRun)]
    public sealed class DecreaseRunTimer : BaseEffect
    {
        [InitEffectInfo]
        static readonly ChaosEffectInfo _effectInfo;

        const int NUM_MINUTES_TO_REMOVE_DEFAULT_VALUE = 5;
        static ConfigEntry<int> _numMinutesToRemove;

        static int numMinutesToRemove
        {
            get
            {
                if (_numMinutesToRemove == null)
                {
                    return NUM_MINUTES_TO_REMOVE_DEFAULT_VALUE;
                }
                else
                {
                    return Mathf.Max(_numMinutesToRemove.Value, 1);
                }
            }
        }

        static int numSecondsToRemove
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => numMinutesToRemove * 60;
        }

        [SystemInitializer(typeof(ChaosEffectCatalog))]
        static void InitConfig()
        {
            _numMinutesToRemove = _effectInfo.BindConfig("Minutes To Rewind", NUM_MINUTES_TO_REMOVE_DEFAULT_VALUE, new ConfigDescription("The amount of minutes to rewind the run timer by"));

            addConfigOption(new IntSliderOption(_numMinutesToRemove, new IntSliderConfig
            {
                min = 1,
                max = 30
            }));
        }

        [EffectCanActivate]
        static bool CanActivate(EffectCanActivateContext context)
        {
            return Run.instance.GetRunStopwatch() + context.Delay >= numSecondsToRemove;
        }

        [EffectNameFormatArgs]
        static object[] GetEffectNameFormatArgs()
        {
            return new object[] { numMinutesToRemove };
        }

        public override void OnStart()
        {
            Run run = Run.instance;
            float oldTime = run.GetRunStopwatch();

            ChaosEffectDispatcher.Instance?.RewindEffectScheduling(Mathf.Min(oldTime, numSecondsToRemove));

            run.SetRunStopwatch(Mathf.Max(0f, oldTime - numSecondsToRemove));
        }
    }
}
