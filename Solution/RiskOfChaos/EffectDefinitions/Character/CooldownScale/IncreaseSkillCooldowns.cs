using RiskOfChaos.ConfigHandling;
using RiskOfChaos.ConfigHandling.AcceptableValues;
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
    [ChaosTimedEffect("increase_skill_cooldown", TimedEffectType.UntilStageEnd, ConfigName = "Increase Skill Cooldowns")]
    public sealed class IncreaseSkillCooldowns : TimedEffect, ISkillSlotModificationProvider
    {
        [EffectConfig]
        static readonly ConfigHolder<float> _cooldownIncrease =
            ConfigFactory<float>.CreateConfig("Cooldown Increase", 0.5f)
                                .Description("How much to increase skill cooldowns by")
                                .AcceptableValues(new AcceptableValueMin<float>(0f))
                                .OptionConfig(new StepSliderConfig
                                {
                                    FormatString = "+{0:P0}",
                                    min = 0f,
                                    max = 2f,
                                    increment = 0.01f
                                })
                                .OnValueChanged(() =>
                                {
                                    if (!NetworkServer.active || !TimedChaosEffectHandler.Instance)
                                        return;

                                    TimedChaosEffectHandler.Instance.InvokeEventOnAllInstancesOfEffect<IncreaseSkillCooldowns>(e => e.OnValueDirty);
                                })
                                .FormatsEffectName()
                                .Build();

        [EffectCanActivate]
        static bool CanActivate()
        {
            return SkillSlotModificationManager.Instance;
        }

        [GetEffectNameFormatter]
        static EffectNameFormatter GetEffectNameFormatter()
        {
            return new EffectNameFormatter_GenericFloat(_cooldownIncrease.Value) { ValueFormat = "P0" };
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
            value.CooldownScale *= 1f + _cooldownIncrease.Value;
        }
    }
}
