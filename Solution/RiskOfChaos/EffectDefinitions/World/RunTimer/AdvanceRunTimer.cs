using RiskOfChaos.ConfigHandling;
using RiskOfChaos.ConfigHandling.AcceptableValues;
using RiskOfChaos.EffectHandling.Controllers;
using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Data;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Methods;
using RiskOfChaos.EffectHandling.Formatting;
using RiskOfOptions.OptionConfigs;
using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace RiskOfChaos.EffectDefinitions.World.RunTimer
{
    [ChaosEffect("advance_run_timer", ConfigName = "Advance Run Timer")]
    public sealed class AdvanceRunTimer : MonoBehaviour
    {
        [EffectConfig]
        static readonly ConfigHolder<int> _numMinutesToAdd =
            ConfigFactory<int>.CreateConfig("Minutes To Add", 5)
                              .Description("The amount of minutes to advance the run timer by")
                              .AcceptableValues(new AcceptableValueMin<int>(1))
                              .OptionConfig(new IntFieldConfig { Min = 1 })
                              .Build();

        [GetEffectNameFormatter]
        static EffectNameFormatter GetNameFormatter()
        {
            return new EffectNameFormatter_PluralizedCount(_numMinutesToAdd);
        }

        void Start()
        {
            if (!NetworkServer.active)
                return;

            Run run = Run.instance;
            run.SetRunStopwatch(run.GetRunStopwatch() + (_numMinutesToAdd.Value * 60));

            foreach (ChaosEffectActivationSignaler effectActivationSignaler in ChaosEffectActivationSignaler.InstancesList)
            {
                effectActivationSignaler.SkipAllScheduledEffects();
            }
        }
    }
}
