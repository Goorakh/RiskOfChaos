using BepInEx.Configuration;
using RiskOfChaos.ConfigHandling;
using RiskOfChaos.EffectHandling;
using RiskOfChaos.EffectHandling.Controllers;
using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Data;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Methods;
using RiskOfChaos.EffectHandling.Formatting;
using RiskOfChaos.OLD_ModifierController.HoldoutZone;
using RiskOfOptions.OptionConfigs;
using System;
using UnityEngine.Networking;

namespace RiskOfChaos.EffectDefinitions.World.HoldoutZone
{
    [ChaosTimedEffect("decrease_holdout_zone_radius", TimedEffectType.UntilStageEnd, ConfigName = "Decrease Teleporter Zone Radius")]
    public sealed class DecreaseHoldoutZoneRadius : TimedEffect, IHoldoutZoneModificationProvider
    {
        [EffectConfig]
        static readonly ConfigHolder<float> _radiusDecrease =
            ConfigFactory<float>.CreateConfig("Radius Decrease", 0.5f)
                                .Description("Percentage decrease of teleporter radius")
                                .AcceptableValues(new AcceptableValueRange<float>(0f, 1f))
                                .OptionConfig(new StepSliderConfig
                                {
                                    FormatString = "-{0:P0}",
                                    increment = 0.01f,
                                    min = 0f,
                                    max = 1f
                                })
                                .OnValueChanged(() =>
                                {
                                    if (!NetworkServer.active || !ChaosEffectTracker.Instance)
                                        return;

                                    ChaosEffectTracker.Instance.OLD_InvokeEventOnAllInstancesOfEffect<DecreaseHoldoutZoneRadius>(e => e.OnValueDirty);
                                })
                                .FormatsEffectName()
                                .Build();

        public event Action OnValueDirty;

        [EffectCanActivate]
        static bool CanActivate()
        {
            return HoldoutZoneModificationManager.Instance;
        }

        [GetEffectNameFormatter]
        static EffectNameFormatter GetNameFormatter()
        {
            return new EffectNameFormatter_GenericFloat(_radiusDecrease.Value) { ValueFormat = "P0" };
        }

        public override void OnStart()
        {
            HoldoutZoneModificationManager.Instance.RegisterModificationProvider(this);
        }

        public override void OnEnd()
        {
            if (HoldoutZoneModificationManager.Instance)
            {
                HoldoutZoneModificationManager.Instance.UnregisterModificationProvider(this);
            }
        }

        public void ModifyValue(ref HoldoutZoneModificationInfo value)
        {
            value.RadiusMultiplier *= 1f - _radiusDecrease.Value;
        }
    }
}
