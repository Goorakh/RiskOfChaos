using RiskOfChaos.ConfigHandling;
using RiskOfChaos.EffectHandling;
using RiskOfChaos.EffectHandling.Controllers;
using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Data;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Methods;
using RiskOfOptions.OptionConfigs;
using RoR2;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace RiskOfChaos.EffectDefinitions.World
{
    [ChaosEffect("decrease_run_timer", ConfigName = "Rewind Run Timer", DefaultSelectionWeight = 0.8f, EffectWeightReductionPercentagePerActivation = 15f, EffectRepetitionWeightCalculationMode = EffectActivationCountMode.PerRun)]
    public sealed class DecreaseRunTimer : BaseEffect
    {
        [EffectConfig]
        static readonly ConfigHolder<int> _numMinutesToRemove =
            ConfigFactory<int>.CreateConfig("Minutes To Rewind", 5)
                              .Description("The amount of minutes to rewind the run timer by")
                              .OptionConfig(new IntSliderConfig
                              {
                                  min = 1,
                                  max = 30
                              })
                              .ValueConstrictor(CommonValueConstrictors.GreaterThanOrEqualTo(1))
                              .Build();

        static int numSecondsToRemove
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _numMinutesToRemove.Value * 60;
        }

        [EffectCanActivate]
        static bool CanActivate(in EffectCanActivateContext context)
        {
            return Run.instance.GetRunStopwatch() + context.Delay >= numSecondsToRemove;
        }

        [EffectNameFormatArgs]
        static object[] GetEffectNameFormatArgs()
        {
            return new object[] { _numMinutesToRemove.Value };
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
