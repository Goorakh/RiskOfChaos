using RiskOfChaos.ConfigHandling;
using RiskOfChaos.ConfigHandling.AcceptableValues;
using RiskOfChaos.EffectHandling;
using RiskOfChaos.EffectHandling.Controllers;
using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Data;
using RiskOfChaos.Utilities.Extensions;
using RiskOfOptions.OptionConfigs;
using RoR2;
using System.Collections.Generic;

namespace RiskOfChaos.EffectDefinitions.Meta
{
    [ChaosEffect("activate_several_effects", DefaultSelectionWeight = 0.5f)]
    public sealed class ActivateSeveralEffects : BaseEffect
    {
        [InitEffectInfo]
        static readonly ChaosEffectInfo _effectInfo;

        static readonly HashSet<ChaosEffectInfo> _excludeEffects = [];

        [EffectConfig]
        static readonly ConfigHolder<int> _numEffectsToActivate =
            ConfigFactory<int>.CreateConfig("Effect Count", 2)
                              .Description("How many effects should be activated by this effect")
                              .AcceptableValues(new AcceptableValueMin<int>(1))
                              .OptionConfig(new IntFieldConfig { Min = 1 })
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
                ChaosEffectInfo effect = ChaosEffectActivationSignaler.PickEffect(RNG.Branch(), excludeEffects, out ChaosEffectDispatchArgs dispatchArgs);
                dispatchArgs.DispatchFlags |= EffectDispatchFlags.DontPlaySound;
                dispatchArgs.OverrideRNGSeed = RNG.nextUlong;
                ChaosEffectDispatcher.Instance.DispatchEffect(effect, dispatchArgs);

                if (!allowDuplicateEffects)
                {
                    excludeEffects.Add(effect);
                }
            }
        }
    }
}
