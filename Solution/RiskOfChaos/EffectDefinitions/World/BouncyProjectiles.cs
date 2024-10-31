using RiskOfChaos.ConfigHandling;
using RiskOfChaos.ConfigHandling.AcceptableValues;
using RiskOfChaos.Content;
using RiskOfChaos.EffectHandling;
using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Data;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Methods;
using RiskOfChaos.ModificationController;
using RiskOfChaos.ModificationController.Projectile;
using RiskOfOptions.OptionConfigs;
using UnityEngine;
using UnityEngine.Networking;

namespace RiskOfChaos.EffectDefinitions.World
{
    [ChaosTimedEffect("bouncy_projectiles", TimedEffectType.UntilStageEnd)]
    public sealed class BouncyProjectiles : MonoBehaviour
    {
        [EffectConfig]
        static readonly ConfigHolder<int> _maxBulletBounceCount =
            ConfigFactory<int>.CreateConfig("Max Bullet Bounce Count", 20)
                              .Description("The maximum amount of times bullets can be bounced")
                              .AcceptableValues(new AcceptableValueMin<int>(1))
                              .OptionConfig(new IntFieldConfig { Min = 1 })
                              .Build();

        [EffectConfig]
        static readonly ConfigHolder<int> _maxProjectileBounceCount =
            ConfigFactory<int>.CreateConfig("Max Projectile Bounce Count", 7)
                              .Description("The maximum amount of times projectiles can be bounced")
                              .AcceptableValues(new AcceptableValueMin<int>(1))
                              .OptionConfig(new IntFieldConfig { Min = 1 })
                              .Build();

        [EffectCanActivate]
        static bool CanActivate()
        {
            return RoCContent.NetworkedPrefabs.ProjectileModificationProvider;
        }

        ValueModificationController _projectileModificationController;

        void Start()
        {
            if (NetworkServer.active)
            {
                _projectileModificationController = Instantiate(RoCContent.NetworkedPrefabs.ProjectileModificationProvider).GetComponent<ValueModificationController>();

                ProjectileModificationProvider projectileModificationProvider = _projectileModificationController.GetComponent<ProjectileModificationProvider>();

                projectileModificationProvider.BulletBounceCountConfigBinding.BindToConfig(_maxBulletBounceCount);
                projectileModificationProvider.ProjectileBounceCountConfigBinding.BindToConfig(_maxProjectileBounceCount);
                projectileModificationProvider.OrbBounceCountConfigBinding.BindToConfig(_maxProjectileBounceCount);

                NetworkServer.Spawn(_projectileModificationController.gameObject);
            }
        }

        void OnDestroy()
        {
            if (_projectileModificationController)
            {
                _projectileModificationController.Retire();
                _projectileModificationController = null;
            }
        }
    }
}
