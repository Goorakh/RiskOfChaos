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
    [ChaosTimedEffect("increase_proc_coefficients", TimedEffectType.UntilStageEnd, ConfigName = "Increase Proc Coefficients")]
    public sealed class IncreaseProcCoefficients : MonoBehaviour
    {
        [EffectConfig]
        static readonly ConfigHolder<float> _multiplierPerActivation =
            ConfigFactory<float>.CreateConfig("Proc Multiplier", 2f)
                                .AcceptableValues(new AcceptableValueMin<float>(1f))
                                .OptionConfig(new FloatFieldConfig { Min = 1f, FormatString = "{0}x" })
                                .FormatsEffectName()
                                .Build();

        [GetEffectNameFormatter]
        static EffectNameFormatter GetDisplayNameFormatter()
        {
            return new EffectNameFormatter_GenericFloat(_multiplierPerActivation.Value);
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
            damageInfo.procCoefficient *= _multiplierPerActivation.Value;
        }
    }
}
