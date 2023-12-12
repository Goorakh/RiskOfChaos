using RiskOfChaos.ConfigHandling;
using RiskOfChaos.EffectHandling;
using RiskOfChaos.EffectHandling.Controllers;
using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Data;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Methods;
using RiskOfChaos.EffectHandling.Formatting;
using RiskOfChaos.ModifierController.SkillSlots;
using RiskOfOptions.OptionConfigs;
using System;
using UnityEngine.Networking;

namespace RiskOfChaos.EffectDefinitions.Character.CooldownScale
{
    [ChaosTimedEffect("decrease_skill_cooldown", TimedEffectType.UntilStageEnd, ConfigName = "Decrease Skill Cooldowns")]
    public sealed class DecreaseSkillCooldowns : TimedEffect, ISkillSlotModificationProvider
    {
        [EffectConfig]
        static readonly ConfigHolder<float> _cooldownDecrease =
            ConfigFactory<float>.CreateConfig("Cooldown Decrease", 0.5f)
                                .Description("How much to decrease skill cooldowns by")
                                .OptionConfig(new StepSliderConfig
                                {
                                    formatString = "-{0:P0}",
                                    min = 0f,
                                    max = 1f,
                                    increment = 0.01f
                                })
                                .OnValueChanged(() =>
                                {
                                    if (!NetworkServer.active || !TimedChaosEffectHandler.Instance)
                                        return;

                                    TimedChaosEffectHandler.Instance.InvokeEventOnAllInstancesOfEffect<DecreaseSkillCooldowns>(e => e.OnValueDirty);
                                })
                                .ValueConstrictor(CommonValueConstrictors.Clamped01Float)
                                .Build();

        [EffectCanActivate]
        static bool CanActivate()
        {
            return SkillSlotModificationManager.Instance;
        }

        [GetEffectNameFormatter]
        static EffectNameFormatter GetEffectNameFormatter()
        {
            return new EffectNameFormatter_GenericFloat(_cooldownDecrease.Value) { ValueFormat = "P0" };
        }

        public override void OnStart()
        {
            SkillSlotModificationManager.Instance.RegisterModificationProvider(this);
        }

        public override void OnEnd()
        {
            if (SkillSlotModificationManager.Instance)
            {
                SkillSlotModificationManager.Instance.UnregisterModificationProvider(this);
            }
        }

        public event Action OnValueDirty;

        public void ModifyValue(ref SkillSlotModificationData value)
        {
            value.CooldownScale *= 1f - _cooldownDecrease.Value;
        }
    }
}
