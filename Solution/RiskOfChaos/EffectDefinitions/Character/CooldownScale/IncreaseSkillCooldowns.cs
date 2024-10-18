using RiskOfChaos.ConfigHandling;
using RiskOfChaos.ConfigHandling.AcceptableValues;
using RiskOfChaos.Content;
using RiskOfChaos.EffectHandling;
using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Data;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Methods;
using RiskOfChaos.EffectHandling.Formatting;
using RiskOfOptions.OptionConfigs;
using UnityEngine.Networking;

namespace RiskOfChaos.EffectDefinitions.Character.CooldownScale
{
    [ChaosTimedEffect("increase_skill_cooldown", TimedEffectType.UntilStageEnd, ConfigName = "Increase Skill Cooldowns")]
    [RequiredComponents(typeof(CooldownScaleMultiplierEffect))]
    public sealed class IncreaseSkillCooldowns : NetworkBehaviour
    {
        [EffectConfig]
        static readonly ConfigHolder<float> _cooldownIncrease =
            ConfigFactory<float>.CreateConfig("Cooldown Increase", 0.5f)
                                .Description("How much to increase skill cooldowns by")
                                .AcceptableValues(new AcceptableValueMin<float>(0f))
                                .OptionConfig(new FloatFieldConfig
                                {
                                    FormatString = "+{0:P0}",
                                    Min = 0f
                                })
                                .FormatsEffectName()
                                .Build();

        [GetEffectNameFormatter]
        static EffectNameFormatter GetEffectNameFormatter()
        {
            return new EffectNameFormatter_GenericFloat(_cooldownIncrease.Value) { ValueFormat = "P0" };
        }

        CooldownScaleMultiplierEffect _cooldownMultiplierEffect;

        void Awake()
        {
            _cooldownMultiplierEffect = GetComponent<CooldownScaleMultiplierEffect>();
        }

        public override void OnStartServer()
        {
            base.OnStartServer();

            _cooldownIncrease.SettingChanged += onCooldownIncreaseChanged;
            updateMultiplier();
        }

        void onCooldownIncreaseChanged(object sender, ConfigChangedArgs<float> e)
        {
            updateMultiplier();
        }

        [Server]
        void updateMultiplier()
        {
            _cooldownMultiplierEffect.Multiplier = 1f + _cooldownIncrease.Value;
        }

        void OnDestroy()
        {
            _cooldownIncrease.SettingChanged -= onCooldownIncreaseChanged;
        }
    }
}
