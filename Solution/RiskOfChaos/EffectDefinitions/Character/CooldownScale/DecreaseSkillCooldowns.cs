using BepInEx.Configuration;
using RiskOfChaos.ConfigHandling;
using RiskOfChaos.Content;
using RiskOfChaos.EffectHandling;
using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Data;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Methods;
using RiskOfChaos.EffectHandling.Formatting;
using RiskOfChaos.ModificationController;
using RiskOfChaos.ModificationController.SkillSlots;
using RiskOfOptions.OptionConfigs;
using UnityEngine;
using UnityEngine.Networking;

namespace RiskOfChaos.EffectDefinitions.Character.CooldownScale
{
    [ChaosTimedEffect("decrease_skill_cooldown", TimedEffectType.UntilStageEnd, ConfigName = "Decrease Skill Cooldowns")]
    public sealed class DecreaseSkillCooldowns : MonoBehaviour
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

        [EffectCanActivate]
        static bool CanActivate()
        {
            return RoCContent.NetworkedPrefabs.SkillSlotModificationProvider;
        }

        [GetEffectNameFormatter]
        static EffectNameFormatter GetEffectNameFormatter()
        {
            return new EffectNameFormatter_GenericFloat(_cooldownDecrease.Value) { ValueFormat = "P0" };
        }

        ValueModificationController _skillSlotModificationController;

        void Start()
        {
            if (NetworkServer.active)
            {
                _skillSlotModificationController = Instantiate(RoCContent.NetworkedPrefabs.SkillSlotModificationProvider).GetComponent<ValueModificationController>();

                SkillSlotModificationProvider skillSlotModificationProvider = _skillSlotModificationController.GetComponent<SkillSlotModificationProvider>();
                skillSlotModificationProvider.CooldownMultiplierConfigBinding.BindToConfig(_cooldownDecrease, v => 1f - v);

                NetworkServer.Spawn(_skillSlotModificationController.gameObject);
            }
        }

        void OnDestroy()
        {
            if (_skillSlotModificationController)
            {
                _skillSlotModificationController.Retire();
                _skillSlotModificationController = null;
            }
        }
    }
}
