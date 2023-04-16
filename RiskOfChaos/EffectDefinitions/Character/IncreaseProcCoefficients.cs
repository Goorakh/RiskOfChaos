using BepInEx.Configuration;
using RiskOfChaos.EffectHandling;
using RiskOfChaos.EffectHandling.Controllers;
using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Data;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Methods;
using RiskOfOptions.OptionConfigs;
using RiskOfOptions.Options;
using RoR2;
using System.Collections.Generic;
using UnityEngine;

namespace RiskOfChaos.EffectDefinitions.Character
{
    [ChaosEffect("increase_proc_coefficients", ConfigName = "Increase Proc Coefficients")]
    public sealed class IncreaseProcCoefficients : TimedEffect
    {
        [InitEffectInfo]
        static readonly ChaosEffectInfo _effectInfo;

        static ConfigEntry<float> _multiplierPerActivationConfig;
        const float MULTIPLIER_PER_ACTIVATION_DEFAULT_VALUE = 2f;

        static float multiplierPerActivation
        {
            get
            {
                if (_multiplierPerActivationConfig == null)
                {
                    return MULTIPLIER_PER_ACTIVATION_DEFAULT_VALUE;
                }
                else
                {
                    return Mathf.Max(_multiplierPerActivationConfig.Value, 1f);
                }
            }
        }

        [SystemInitializer(typeof(ChaosEffectCatalog))]
        static void InitConfigs()
        {
            _multiplierPerActivationConfig = Main.Instance.Config.Bind(new ConfigDefinition(_effectInfo.ConfigSectionName, "Proc Multiplier"), MULTIPLIER_PER_ACTIVATION_DEFAULT_VALUE);

            addConfigOption(new StepSliderOption(_multiplierPerActivationConfig, new StepSliderConfig
            {
                formatString = "{0:F1}",
                min = 1f,
                max = 10f,
                increment = 0.5f
            }));
        }

        [EffectNameFormatArgs]
        static object[] GetDisplayNameFormatArgs()
        {
            return new object[]
            {
                multiplierPerActivation
            };
        }

        public override TimedEffectType TimedType => TimedEffectType.UntilStageEnd;

        static bool _hasAppliedPatches = false;
        static void tryApplyPatches()
        {
            if (_hasAppliedPatches)
                return;

            On.RoR2.GlobalEventManager.OnHitAll += (orig, self, damageInfo, hitObject) =>
            {
                tryMultiplyProcCoefficient(damageInfo);
                orig(self, damageInfo, hitObject);
            };

            On.RoR2.GlobalEventManager.OnHitEnemy += (orig, self, damageInfo, victim) =>
            {
                tryMultiplyProcCoefficient(damageInfo);
                orig(self, damageInfo, victim);
            };

            On.RoR2.HealthComponent.TakeDamage += (orig, self, damageInfo) =>
            {
                tryMultiplyProcCoefficient(damageInfo);
                orig(self, damageInfo);
            };

            _hasAppliedPatches = true;
        }

        static void tryMultiplyProcCoefficient(DamageInfo damageInfo)
        {
            if (!TimedChaosEffectHandler.Instance)
                return;

            int effectActiveCount = TimedChaosEffectHandler.Instance.GetEffectActiveCount(_effectInfo);
            if (effectActiveCount <= 0)
                return;

            damageInfo.procCoefficient *= Mathf.Pow(multiplierPerActivation, effectActiveCount);
        }

        public override void OnStart()
        {
            tryApplyPatches();
        }

        public override void OnEnd()
        {
        }
    }
}
