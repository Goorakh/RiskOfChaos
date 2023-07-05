using BepInEx.Configuration;
using RiskOfChaos.EffectHandling;
using RiskOfChaos.EffectHandling.Controllers;
using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Data;
using RiskOfChaos.Utilities.Extensions;
using RiskOfOptions.OptionConfigs;
using RiskOfOptions.Options;
using RoR2;
using System.Collections.Generic;
using UnityEngine;

namespace RiskOfChaos.EffectDefinitions.Meta
{
    [ChaosEffect("activate_several_effects", DefaultSelectionWeight = 0.5f, EffectWeightReductionPercentagePerActivation = 15f)]
    public sealed class ActivateSeveralEffects : BaseEffect
    {
        [InitEffectInfo]
        static readonly ChaosEffectInfo _effectInfo;

        static readonly HashSet<ChaosEffectInfo> _excludeEffects = new HashSet<ChaosEffectInfo>();

        const int NUM_EFFECTS_TO_ACTIVATE_DEFAULT_VALUE = 2;

        static ConfigEntry<int> _numEffectsToActivateConfig;

        static int numEffectsToActivate
        {
            get
            {
                if (_numEffectsToActivateConfig == null)
                {
                    return NUM_EFFECTS_TO_ACTIVATE_DEFAULT_VALUE;
                }
                else
                {
                    return Mathf.Max(_numEffectsToActivateConfig.Value, 1);
                }
            }
        }

        static ConfigEntry<bool> _allowDuplicateEffectsConfig;
        const bool ALLOW_DUPLICATE_EFFECTS_DEFAULT_VALUE = true;

        static bool allowDuplicateEffects => _allowDuplicateEffectsConfig?.Value ?? ALLOW_DUPLICATE_EFFECTS_DEFAULT_VALUE;

        [SystemInitializer(typeof(ChaosEffectCatalog))]
        static void InitConfig()
        {
            _excludeEffects.Add(_effectInfo);

            _numEffectsToActivateConfig = _effectInfo.BindConfig("Effect Count", NUM_EFFECTS_TO_ACTIVATE_DEFAULT_VALUE, new ConfigDescription("How many effects should be activated by this effect"));

            addConfigOption(new IntSliderOption(_numEffectsToActivateConfig, new IntSliderConfig
            {
                min = 1,
                max = 10
            }));

            _allowDuplicateEffectsConfig = _effectInfo.BindConfig("Allow Duplicate Effects", ALLOW_DUPLICATE_EFFECTS_DEFAULT_VALUE, new ConfigDescription("If the effect can select duplicate effects to activate"));

            addConfigOption(new CheckBoxOption(_allowDuplicateEffectsConfig));
        }

        public override void OnStart()
        {
            WeightedSelection<ChaosEffectInfo> effectSelection = ChaosEffectCatalog.GetAllActivatableEffects(EffectCanActivateContext.Now, _excludeEffects);

            for (int i = numEffectsToActivate - 1; i >= 0; i--)
            {
                if (effectSelection.Count <= 0)
                {
                    Log.Warning("No remaining activatable effects in selection");
                    return;
                }

                ChaosEffectInfo effect = allowDuplicateEffects ? effectSelection.GetRandom(RNG) : effectSelection.GetAndRemoveRandom(RNG);
                ChaosEffectDispatcher.Instance.DispatchEffect(effect, EffectDispatchFlags.DontPlaySound);
            }
        }
    }
}
