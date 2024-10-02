using BepInEx.Configuration;
using RiskOfChaos.ConfigHandling;
using RiskOfChaos.EffectHandling.Controllers;
using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Data;
using RiskOfChaos.ModifierController.Projectile;
using RiskOfChaos.Utilities;
using RiskOfOptions.OptionConfigs;
using System;
using UnityEngine.Networking;

namespace RiskOfChaos.EffectDefinitions.World
{
    [ChaosTimedEffect("repeat_projectiles", 90f)]
    public sealed class RepeatProjectiles : TimedEffect, IProjectileModificationProvider
    {
        [EffectConfig]
        static readonly ConfigHolder<int> _extraSpawnCountConfig =
            ConfigFactory<int>.CreateConfig("Additional Projectile Spawn Count", 5)
                              .Description("How many additional projectiles should be spawned per projectile")
                              .AcceptableValues(new AcceptableValueRange<int>(1, byte.MaxValue))
                              .OptionConfig(new IntFieldConfig { Min = 1, Max = byte.MaxValue })
                              .OnValueChanged(() =>
                              {
                                  if (!NetworkServer.active || !TimedChaosEffectHandler.Instance)
                                      return;
                              
                                  TimedChaosEffectHandler.Instance.InvokeEventOnAllInstancesOfEffect<RepeatProjectiles>(e => e.OnValueDirty);
                              })
                              .Build();

        public event Action OnValueDirty;

        public override void OnStart()
        {
            ProjectileModificationManager.Instance.RegisterModificationProvider(this);
        }

        public override void OnEnd()
        {
            if (ProjectileModificationManager.Instance)
            {
                ProjectileModificationManager.Instance.UnregisterModificationProvider(this);
            }
        }

        public void ModifyValue(ref ProjectileModificationData value)
        {
            value.ExtraSpawnCount = ClampedConversion.UInt8(value.ExtraSpawnCount + _extraSpawnCountConfig.Value);
        }
    }
}
