using RiskOfChaos.ConfigHandling;
using RiskOfChaos.EffectHandling.Controllers;
using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Data;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Methods;
using RiskOfChaos.OLD_ModifierController.Pickups;
using RiskOfChaos.Utilities;
using RiskOfOptions.OptionConfigs;
using System;
using UnityEngine.Networking;

namespace RiskOfChaos.EffectDefinitions.World.Pickups
{
    [ChaosTimedEffect("bouncy_pickups", 60f, AllowDuplicates = true)]
    public sealed class BouncyPickups : TimedEffect, IPickupModificationProvider
    {
        [EffectConfig]
        static readonly ConfigHolder<int> _bounceCount =
            ConfigFactory<int>.CreateConfig("Bounce Count", 2)
                              .Description("How many times items should bounce before settling")
                              .OptionConfig(new IntFieldConfig { Min = 1 })
                              .OnValueChanged(() =>
                              {
                                  if (!NetworkServer.active || !ChaosEffectTracker.Instance)
                                      return;

                                  ChaosEffectTracker.Instance.OLD_InvokeEventOnAllInstancesOfEffect<BouncyPickups>(e => e.OnValueDirty);
                              })
                              .Build();

        [EffectCanActivate]
        static bool CanActivate()
        {
            return PickupModificationManager.Instance;
        }

        public event Action OnValueDirty;

        public void ModifyValue(ref PickupModificationInfo value)
        {
            value.BounceCount += ClampedConversion.UInt32(_bounceCount.Value);
        }

        public override void OnStart()
        {
            PickupModificationManager.Instance.RegisterModificationProvider(this);
        }

        public override void OnEnd()
        {
            if (PickupModificationManager.Instance)
            {
                PickupModificationManager.Instance.UnregisterModificationProvider(this);
            }
        }
    }
}
