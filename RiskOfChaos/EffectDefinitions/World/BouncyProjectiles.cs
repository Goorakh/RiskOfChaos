using BepInEx.Configuration;
using RiskOfChaos.EffectHandling;
using RiskOfChaos.EffectHandling.Controllers;
using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Data;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Methods;
using RiskOfChaos.ModifierController.Projectile;
using RiskOfOptions.OptionConfigs;
using RiskOfOptions.Options;
using RoR2;
using System;
using UnityEngine;
using UnityEngine.Networking;

namespace RiskOfChaos.EffectDefinitions.World
{
    [ChaosEffect("bouncy_projectiles")]
    [ChaosTimedEffect(TimedEffectType.UntilStageEnd, AllowDuplicates = false)]
    public sealed class BouncyProjectiles : TimedEffect, IProjectileModificationProvider
    {
        [InitEffectInfo]
        static readonly ChaosEffectInfo _effectInfo;

        static ConfigEntry<int> _maxBulletBounceCountConfig;
        const int MAX_BULLET_BOUNCE_COUNT_DEFAULT_VALUE = 20;
        static int maxBulletBounceCount
        {
            get
            {
                if (_maxBulletBounceCountConfig == null)
                {
                    return MAX_BULLET_BOUNCE_COUNT_DEFAULT_VALUE;
                }
                else
                {
                    return Mathf.Max(1, _maxBulletBounceCountConfig.Value);
                }
            }
        }

        static ConfigEntry<int> _maxProjectileBounceCountConfig;
        const int MAX_PROJECTILE_BOUNCE_COUNT_DEFAULT_VALUE = 2;
        static int maxProjectileBounceCount
        {
            get
            {
                if (_maxProjectileBounceCountConfig == null)
                {
                    return MAX_PROJECTILE_BOUNCE_COUNT_DEFAULT_VALUE;
                }
                else
                {
                    return Mathf.Max(1, _maxProjectileBounceCountConfig.Value);
                }
            }
        }

        [SystemInitializer(typeof(ChaosEffectCatalog))]
        static void InitConfigs()
        {
            _maxBulletBounceCountConfig = _effectInfo.BindConfig("Max Bullet Bounce Count", MAX_BULLET_BOUNCE_COUNT_DEFAULT_VALUE, new ConfigDescription("The maximum amount of times bullets can be bounced"));

            _maxBulletBounceCountConfig.SettingChanged += bounceCountConfigChanged;

            addConfigOption(new IntSliderOption(_maxBulletBounceCountConfig, new IntSliderConfig
            {
                min = 1,
                max = 40
            }));

            _maxProjectileBounceCountConfig = _effectInfo.BindConfig("Max Projectile Bounce Count", MAX_PROJECTILE_BOUNCE_COUNT_DEFAULT_VALUE, new ConfigDescription("The maximum amount of times projectiels can be bounced"));

            _maxProjectileBounceCountConfig.SettingChanged += bounceCountConfigChanged;

            addConfigOption(new IntSliderOption(_maxProjectileBounceCountConfig, new IntSliderConfig
            {
                min = 1,
                max = 10
            }));
        }

        static void bounceCountConfigChanged(object sender, EventArgs e)
        {
            if (!NetworkServer.active || !TimedChaosEffectHandler.Instance)
                return;

            foreach (BouncyProjectiles effectInstance in TimedChaosEffectHandler.Instance.GetActiveEffectInstancesOfType<BouncyProjectiles>())
            {
                effectInstance.OnValueDirty?.Invoke();
            }
        }

        [EffectCanActivate]
        static bool CanActivate()
        {
            return ProjectileModificationManager.Instance;
        }

        public override void OnStart()
        {
            ProjectileModificationManager.Instance.RegisterModificationProvider(this);
        }

        public event Action OnValueDirty;

        public void ModifyValue(ref ProjectileModificationData value)
        {
            value.BulletBounceCount += (uint)maxBulletBounceCount;
            value.ProjectileBounceCount += (uint)maxProjectileBounceCount;
            value.OrbBounceCount += (uint)maxProjectileBounceCount;
        }

        public override void OnEnd()
        {
            if (ProjectileModificationManager.Instance)
            {
                ProjectileModificationManager.Instance.UnregisterModificationProvider(this);
            }
        }
    }
}
