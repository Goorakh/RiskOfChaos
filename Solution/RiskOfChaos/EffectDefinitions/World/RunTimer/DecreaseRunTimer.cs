using RiskOfChaos.ConfigHandling;
using RiskOfChaos.ConfigHandling.AcceptableValues;
using RiskOfChaos.EffectHandling.Controllers;
using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Data;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Methods;
using RiskOfChaos.EffectHandling.Formatting;
using RiskOfOptions.OptionConfigs;
using RoR2;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.Networking;

namespace RiskOfChaos.EffectDefinitions.World.RunTimer
{
    [ChaosEffect("decrease_run_timer", ConfigName = "Rewind Run Timer", DefaultSelectionWeight = 0.8f)]
    public sealed class DecreaseRunTimer : MonoBehaviour
    {
        [EffectConfig]
        static readonly ConfigHolder<int> _numMinutesToRemove =
            ConfigFactory<int>.CreateConfig("Minutes To Rewind", 5)
                              .Description("The amount of minutes to rewind the run timer by")
                              .AcceptableValues(new AcceptableValueMin<int>(1))
                              .OptionConfig(new IntFieldConfig { Min = 1 })
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
            return Mathf.Clamp01(Run.instance.GetRunStopwatch() / numSecondsToRemove);
        }

        [GetEffectNameFormatter]
        static EffectNameFormatter GetNameFormatter()
        {
            return new EffectNameFormatter_PluralizedCount(_numMinutesToRemove);
        }

        void Start()
        {
            if (!NetworkServer.active)
                return;

            Run run = Run.instance;
            float prevTime = run.GetRunStopwatch();

            float newTime = Mathf.Max(0f, prevTime - numSecondsToRemove);

            foreach (ChaosEffectActivationSignaler effectActivationSignaler in ChaosEffectActivationSignaler.InstancesList)
            {
                effectActivationSignaler.RewindEffectScheduling(prevTime - newTime);
            }

            run.SetRunStopwatch(newTime);
        }
    }
}
