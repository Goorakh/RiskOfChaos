using RiskOfChaos.ConfigHandling;
using RiskOfChaos.ConfigHandling.AcceptableValues;
using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Data;
using RiskOfChaos.Utilities;
using RiskOfChaos.Utilities.Extensions;
using RiskOfOptions.OptionConfigs;
using RoR2;

namespace RiskOfChaos.EffectDefinitions.Character
{
    [ChaosEffect("freeze_all")]
    public sealed class FreezeAll : BaseEffect
    {
        [EffectConfig]
        static readonly ConfigHolder<float> _freezeDuration =
            ConfigFactory<float>.CreateConfig("Freeze Duration", 2.5f)
                                .Description("How long all characters will be frozen for, in seconds")
                                .AcceptableValues(new AcceptableValueMin<float>(0.5f))
                                .OptionConfig(new StepSliderConfig
                                {
                                    min = 0.5f,
                                    max = 10f,
                                    increment = 0.5f
                                })
                                .Build();

        public override void OnStart()
        {
            CharacterBody.readOnlyInstancesList.TryDo(body =>
            {
                if (body && body.TryGetComponent(out SetStateOnHurt setStateOnHurt))
                {
                    ref bool canBeFrozen = ref setStateOnHurt.canBeFrozen;
                    bool originalCanBeFrozen = canBeFrozen;

                    canBeFrozen = true;
                    try
                    {
                        setStateOnHurt.SetFrozen(_freezeDuration.Value);
                    }
                    finally
                    {
                        canBeFrozen = originalCanBeFrozen;
                    }
                }
            }, FormatUtils.GetBestBodyName);
        }
    }
}
