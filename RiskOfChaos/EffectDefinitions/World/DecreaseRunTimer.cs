using RiskOfChaos.ConfigHandling;
using RiskOfChaos.EffectHandling;
using RiskOfChaos.EffectHandling.Controllers;
using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Data;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Methods;
using RiskOfChaos.EffectHandling.Formatting;
using RiskOfOptions.OptionConfigs;
using RoR2;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace RiskOfChaos.EffectDefinitions.World
{
    [ChaosEffect("decrease_run_timer", ConfigName = "Rewind Run Timer", DefaultSelectionWeight = 0.8f)]
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

        [EffectWeightMultiplierSelector]
        static float GetEffectWeightMultiplier()
        {
            if (!Run.instance)
                return 0f;

            // scale weight up linearly from 0-1 as run time gets closer to the amount of time to remove
            float currentTime = Run.instance.GetRunStopwatch();
            return currentTime > numSecondsToRemove ? 1f : currentTime / numSecondsToRemove;
        }

        [GetEffectNameFormatter]
        static EffectNameFormatter GetNameFormatter()
        {
            return new EffectNameFormatter_PluralizedCount(_numMinutesToRemove.Value);
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
