using RiskOfChaos.ConfigHandling;
using RiskOfChaos.ConfigHandling.AcceptableValues;
using RiskOfChaos.Content;
using RiskOfChaos.EffectHandling;
using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Data;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Methods;
using RiskOfChaos.EffectHandling.Formatting;
using RiskOfChaos.ModificationController;
using RiskOfChaos.ModificationController.Knockback;
using RiskOfOptions.OptionConfigs;
using UnityEngine;
using UnityEngine.Networking;

namespace RiskOfChaos.EffectDefinitions.World.Knockback
{
    [ChaosTimedEffect("increase_knockback", TimedEffectType.UntilStageEnd, ConfigName = "Increase Knockback")]
    [IncompatibleEffects(typeof(DisableKnockback))]
    public sealed class IncreaseKnockback : MonoBehaviour
    {
        [EffectConfig]
        static readonly ConfigHolder<float> _knockbackMultiplier =
            ConfigFactory<float>.CreateConfig("Knockback Multiplier", 3f)
                                .Description("The multiplier used to increase knockback while the effect is active")
                                .AcceptableValues(new AcceptableValueMin<float>(1f))
                                .OptionConfig(new FloatFieldConfig { Min = 1f, FormatString = "{0}x" })
                                .Build();

        [EffectCanActivate]
        static bool CanActivate()
        {
            return RoCContent.NetworkedPrefabs.KnockbackModificationProvider;
        }

        [GetEffectNameFormatter]
        static EffectNameFormatter GetNameFormatter()
        {
            return new EffectNameFormatter_GenericFloat(_knockbackMultiplier);
        }

        ValueModificationController _knockbackModificationController;

        void Start()
        {
            if (NetworkServer.active)
            {
                _knockbackModificationController = Instantiate(RoCContent.NetworkedPrefabs.KnockbackModificationProvider).GetComponent<ValueModificationController>();

                KnockbackModificationProvider knockbackModificationProvider = _knockbackModificationController.GetComponent<KnockbackModificationProvider>();
                knockbackModificationProvider.KnockbackMultiplierConfigBinding.BindToConfig(_knockbackMultiplier);

                NetworkServer.Spawn(_knockbackModificationController.gameObject);
            }
        }

        void OnDestroy()
        {
            if (_knockbackModificationController)
            {
                _knockbackModificationController.Retire();
                _knockbackModificationController = null;
            }
        }
    }
}
