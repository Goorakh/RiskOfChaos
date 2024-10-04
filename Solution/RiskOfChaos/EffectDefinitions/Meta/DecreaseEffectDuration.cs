using BepInEx.Configuration;
using RiskOfChaos.ConfigHandling;
using RiskOfChaos.EffectHandling;
using RiskOfChaos.EffectHandling.Controllers;
using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Data;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Methods;
using RiskOfChaos.EffectHandling.Formatting;
using RiskOfChaos.ModifierController.Effect;
using RiskOfOptions.OptionConfigs;
using System;
using UnityEngine.Networking;

namespace RiskOfChaos.EffectDefinitions.Meta
{
    [ChaosTimedEffect("decrease_effect_duration", TimedEffectType.UntilStageEnd, ConfigName = "Decrease Effect Duration", IgnoreDurationModifiers = true)]
    public sealed class DecreaseEffectDuration : TimedEffect, IEffectModificationProvider
    {
        [EffectConfig]
        static readonly ConfigHolder<float> _durationMultiplier =
            ConfigFactory<float>.CreateConfig("Duration Multiplier", 0.5f)
                                .AcceptableValues(new AcceptableValueRange<float>(0f, 1f))
                                .OptionConfig(new StepSliderConfig
                                {
                                    FormatString = "{0}x",
                                    min = 0f,
                                    max = 1f,
                                    increment = 0.1f
                                })
                                .OnValueChanged(() =>
                                {
                                    if (!NetworkServer.active || !ChaosEffectTracker.Instance)
                                        return;

                                    ChaosEffectTracker.Instance.OLD_InvokeEventOnAllInstancesOfEffect<DecreaseEffectDuration>(e => e.OnValueDirty);
                                })
                                .FormatsEffectName()
                                .Build();

        [EffectCanActivate]
        static bool CanActivate()
        {
            return EffectModificationManager.Instance;
        }

        [GetEffectNameFormatter]
        static EffectNameFormatter GetNameFormatter()
        {
            return new EffectNameFormatter_GenericFloat(_durationMultiplier.Value);
        }

        public event Action OnValueDirty;

        public override void OnStart()
        {
            EffectModificationManager.Instance.RegisterModificationProvider(this);

            if (ChaosEffectTracker.Instance)
            {
                foreach (TimedEffect effect in ChaosEffectTracker.Instance.GetAllActiveEffects())
                {
                    if (effect.EffectInfo.IgnoreDurationModifiers)
                        continue;

                    effect.MaxStocks *= _durationMultiplier.Value;

                    if (effect.TimedType != TimedEffectType.FixedDuration && effect.StocksRemaining <= 0)
                    {
                        ChaosEffectTracker.Instance.EndEffectServer(effect);
                    }
                }
            }
        }

        public void ModifyValue(ref EffectModificationInfo value)
        {
            value.DurationMultiplier *= _durationMultiplier.Value;
        }

        public override void OnEnd()
        {
            if (EffectModificationManager.Instance)
            {
                EffectModificationManager.Instance.UnregisterModificationProvider(this);
            }
        }
    }
}
