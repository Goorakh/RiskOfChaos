using RiskOfChaos.ConfigHandling;
using RiskOfChaos.EffectHandling;
using RiskOfChaos.EffectHandling.Controllers;
using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Data;
using RiskOfOptions.OptionConfigs;
using RoR2;
using System.Collections.Generic;

namespace RiskOfChaos.EffectDefinitions.Meta
{
    [ChaosEffect("activate_several_effects", DefaultSelectionWeight = 0.5f, EffectWeightReductionPercentagePerActivation = 15f)]
    public sealed class ActivateSeveralEffects : BaseEffect
    {
        [InitEffectInfo]
        static readonly ChaosEffectInfo _effectInfo;

        static readonly HashSet<ChaosEffectInfo> _excludeEffects = new HashSet<ChaosEffectInfo>();

        [EffectConfig]
        static readonly ConfigHolder<int> _numEffectsToActivate =
            ConfigFactory<int>.CreateConfig("Effect Count", 2)
                              .Description("How many effects should be activated by this effect")
                              .OptionConfig(new IntSliderConfig
                              {
                                  min = 1,
                                  max = 10
                              })
                              .ValueConstrictor(CommonValueConstrictors.GreaterThanOrEqualTo(1))
                              .Build();

        [EffectConfig]
        static readonly ConfigHolder<bool> _allowDuplicateEffects =
            ConfigFactory<bool>.CreateConfig("Allow Duplicate Effects", true)
                               .Description("If the effect can select duplicate effects to activate")
                               .OptionConfig(new CheckBoxConfig())
                               .Build();

        [SystemInitializer(typeof(ChaosEffectCatalog))]
        static void InitConfig()
        {
            _excludeEffects.Add(_effectInfo);
        }

        public override void OnStart()
        {
            bool allowDuplicateEffects = _allowDuplicateEffects.Value;

            HashSet<ChaosEffectInfo> excludeEffects = !allowDuplicateEffects ? new HashSet<ChaosEffectInfo>(_excludeEffects) : _excludeEffects;

            for (int i = _numEffectsToActivate.Value - 1; i >= 0; i--)
            {
                ChaosEffectInfo effect = ChaosEffectCatalog.PickActivatableEffect(RNG, EffectCanActivateContext.Now, excludeEffects);
                ChaosEffectDispatcher.Instance.DispatchEffect(effect, EffectDispatchFlags.DontPlaySound);

                if (!allowDuplicateEffects)
                {
                    excludeEffects.Add(effect);
                }
            }
        }
    }
}
