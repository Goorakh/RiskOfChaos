using RiskOfChaos.ConfigHandling;
using RiskOfChaos.ConfigHandling.AcceptableValues;
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
    [ChaosTimedEffect("increase_effect_duration", TimedEffectType.UntilStageEnd, ConfigName = "Increase Effect Duration", DefaultSelectionWeight = 0.7f, IgnoreDurationModifiers = true)]
    public sealed class IncreaseEffectDuration : TimedEffect, IEffectModificationProvider
    {
        [EffectConfig]
        static readonly ConfigHolder<float> _durationMultiplier =
            ConfigFactory<float>.CreateConfig("Effect Duration Multiplier", 2f)
                                .AcceptableValues(new AcceptableValueMin<float>(1f))
                                .OptionConfig(new FloatFieldConfig { Min = 1f, FormatString = "{0}x" })
                                .OnValueChanged(() =>
                                {
                                    if (!NetworkServer.active || !ChaosEffectTracker.Instance)
                                        return;

                                    ChaosEffectTracker.Instance.OLD_InvokeEventOnAllInstancesOfEffect<IncreaseEffectDuration>(e => e.OnValueDirty);
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
                foreach (TimedEffect effect in ChaosEffectTracker.Instance.OLD_GetAllActiveEffects())
                {
                    if (effect.EffectInfo.IgnoreDurationModifiers)
                        continue;

                    effect.MaxStocks *= _durationMultiplier.Value;
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
