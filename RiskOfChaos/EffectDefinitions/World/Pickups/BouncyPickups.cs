using RiskOfChaos.ConfigHandling;
using RiskOfChaos.EffectHandling.Controllers;
using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Data;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Methods;
using RiskOfChaos.ModifierController.Pickups;
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
                              .OptionConfig(new IntSliderConfig
                              {
                                  min = 1,
                                  max = 10
                              })
                              .OnValueChanged(() =>
                              {
                                  if (!NetworkServer.active || !TimedChaosEffectHandler.Instance)
                                      return;

                                  TimedChaosEffectHandler.Instance.InvokeEventOnAllInstancesOfEffect<BouncyPickups>(e => e.OnValueDirty);
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
