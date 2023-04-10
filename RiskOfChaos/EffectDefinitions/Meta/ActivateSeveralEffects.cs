using BepInEx.Configuration;
using RiskOfChaos.EffectHandling;
using RiskOfChaos.EffectHandling.Controllers;
using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Data;
using RiskOfOptions.OptionConfigs;
using RiskOfOptions.Options;
using RoR2;
using UnityEngine;

namespace RiskOfChaos.EffectDefinitions.Meta
{
    [ChaosEffect("activate_several_effects", DefaultSelectionWeight = 0.5f, EffectWeightReductionPercentagePerActivation = 15f)]
    public sealed class ActivateSeveralEffects : BaseEffect
    {
        [InitEffectInfo]
        static readonly ChaosEffectInfo _effectInfo;

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

        [SystemInitializer(typeof(ChaosEffectCatalog))]
        static void InitConfig()
        {
            _numEffectsToActivateConfig = Main.Instance.Config.Bind(new ConfigDefinition(_effectInfo.ConfigSectionName, "Effect Count"), NUM_EFFECTS_TO_ACTIVATE_DEFAULT_VALUE, new ConfigDescription("How many effects should be activated by this effect"));

            addConfigOption(new IntSliderOption(_numEffectsToActivateConfig, new IntSliderConfig
            {
                min = 1,
                max = 10
            }));
        }

        public override void OnStart()
        {
            int numEffects = numEffectsToActivate;
            for (int i = 0; i < numEffects; i++)
            {
                ChaosEffectInfo effectInfo = ChaosEffectCatalog.PickActivatableEffect(RNG, EffectCanActivateContext.Now);
                ChaosEffectDispatcher.Instance.DispatchEffect(effectInfo, EffectDispatchFlags.DontPlaySound | EffectDispatchFlags.DontStopTimedEffects);
            }
        }
    }
}
