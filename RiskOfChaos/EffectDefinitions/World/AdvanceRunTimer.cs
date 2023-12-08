using RiskOfChaos.ConfigHandling;
using RiskOfChaos.EffectHandling;
using RiskOfChaos.EffectHandling.Controllers;
using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Data;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Methods;
using RiskOfOptions.OptionConfigs;
using RoR2;

namespace RiskOfChaos.EffectDefinitions.World
{
    [ChaosEffect("advance_run_timer", ConfigName = "Advance Run Timer", EffectWeightReductionPercentagePerActivation = 15f, EffectRepetitionWeightCalculationMode = EffectActivationCountMode.PerRun)]
    public sealed class AdvanceRunTimer : BaseEffect
    {
        [EffectConfig]
        static readonly ConfigHolder<int> _numMinutesToAdd =
            ConfigFactory<int>.CreateConfig("Minutes To Add", 5)
                              .Description("The amount of minutes to advance the run timer by")
                              .OptionConfig(new IntSliderConfig
                              {
                                  min = 1,
                                  max = 30
                              })
                              .ValueConstrictor(CommonValueConstrictors.GreaterThanOrEqualTo(1))
                              .Build();

        [EffectNameFormatArgs]
        static string[] GetEffectNameFormatArgs()
        {
            return new string[] { _numMinutesToAdd.Value.ToString() };
        }

        public override void OnStart()
        {
            Run run = Run.instance;
            run.SetRunStopwatch(run.GetRunStopwatch() + (_numMinutesToAdd.Value * 60));

            ChaosEffectDispatcher.Instance?.SkipAllScheduledEffects();
        }
    }
}
