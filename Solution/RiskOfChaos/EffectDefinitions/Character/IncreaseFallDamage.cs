using RiskOfChaos.ConfigHandling;
using RiskOfChaos.ConfigHandling.AcceptableValues;
using RiskOfChaos.EffectHandling;
using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Data;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Methods;
using RiskOfChaos.EffectHandling.Formatting;
using RiskOfChaos.Patches;
using RiskOfOptions.OptionConfigs;
using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace RiskOfChaos.EffectDefinitions.Character
{
    [ChaosTimedEffect("increase_fall_damage", TimedEffectType.UntilStageEnd, ConfigName = "Increase Fall Damage")]
    [IncompatibleEffects(typeof(DisableFallDamage))]
    public sealed class IncreaseFallDamage : MonoBehaviour
    {
        [InitEffectInfo]
        public static readonly TimedEffectInfo EffectInfo;

        [EffectConfig]
        static readonly ConfigHolder<float> _damageIncreaseAmount =
            ConfigFactory<float>.CreateConfig("Increase Amount", 1f)
                                .Description("The amount to increase fall damage by")
                                .AcceptableValues(new AcceptableValueMin<float>(0f))
                                .OptionConfig(new StepSliderConfig
                                {
                                    FormatString = "+{0:P0}",
                                    min = 0f,
                                    max = 2f,
                                    increment = 0.05f
                                })
                                .Build();

        [GetEffectNameFormatter]
        static EffectNameFormatter GetEffectNameFormatter()
        {
            return new EffectNameFormatter_GenericFloat(_damageIncreaseAmount) { ValueFormat = "P0" };
        }

        void Start()
        {
            if (NetworkServer.active)
            {
                DamageModificationHooks.ModifyDamageInfo += modifyDamage;
            }
        }

        void OnDestroy()
        {
            DamageModificationHooks.ModifyDamageInfo -= modifyDamage;
        }

        static void modifyDamage(DamageInfo damageInfo)
        {
            if ((damageInfo.damageType & DamageType.FallDamage) != DamageType.FallDamage)
                return;

            damageInfo.damage *= 1f + _damageIncreaseAmount.Value;

            damageInfo.damageType &= ~(DamageTypeCombo)DamageType.NonLethal;
            damageInfo.damageType |= (DamageTypeCombo)DamageType.BypassOneShotProtection;
        }
    }
}
