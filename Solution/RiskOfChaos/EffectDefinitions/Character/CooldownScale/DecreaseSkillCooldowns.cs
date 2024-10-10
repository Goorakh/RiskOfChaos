using BepInEx.Configuration;
using RiskOfChaos.ConfigHandling;
using RiskOfChaos.EffectHandling;
using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Data;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Methods;
using RiskOfChaos.EffectHandling.Formatting;
using RiskOfOptions.OptionConfigs;
using UnityEngine.Networking;

namespace RiskOfChaos.EffectDefinitions.Character.CooldownScale
{
    [ChaosTimedEffect("decrease_skill_cooldown", TimedEffectType.UntilStageEnd, ConfigName = "Decrease Skill Cooldowns")]
    [RequiredComponents(typeof(CooldownScaleMultiplierEffect))]
    public sealed class DecreaseSkillCooldowns : NetworkBehaviour
    {
        [EffectConfig]
        static readonly ConfigHolder<float> _cooldownDecrease =
            ConfigFactory<float>.CreateConfig("Cooldown Decrease", 0.5f)
                                .Description("How much to decrease skill cooldowns by")
                                .AcceptableValues(new AcceptableValueRange<float>(0f, 1f))
                                .OptionConfig(new StepSliderConfig
                                {
                                    FormatString = "-{0:P0}",
                                    min = 0f,
                                    max = 1f,
                                    increment = 0.01f
                                })
                                .FormatsEffectName()
                                .Build();

        [GetEffectNameFormatter]
        static EffectNameFormatter GetEffectNameFormatter()
        {
            return new EffectNameFormatter_GenericFloat(_cooldownDecrease.Value) { ValueFormat = "P0" };
        }

        CooldownScaleMultiplierEffect _cooldownMultiplierEffect;

        void Awake()
        {
            _cooldownMultiplierEffect = GetComponent<CooldownScaleMultiplierEffect>();
        }

        public override void OnStartServer()
        {
            base.OnStartServer();

            _cooldownDecrease.SettingChanged += onCooldownDecreaseChanged;
            updateMultiplier();
        }

        void onCooldownDecreaseChanged(object sender, ConfigChangedArgs<float> e)
        {
            updateMultiplier();
        }

        [Server]
        void updateMultiplier()
        {
            _cooldownMultiplierEffect.Multiplier = 1f - _cooldownDecrease.Value;
        }

        void OnDestroy()
        {
            _cooldownDecrease.SettingChanged -= onCooldownDecreaseChanged;
        }
    }
}
