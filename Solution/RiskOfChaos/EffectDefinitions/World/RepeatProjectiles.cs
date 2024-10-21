using BepInEx.Configuration;
using RiskOfChaos.ConfigHandling;
using RiskOfChaos.Content;
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
    [ChaosTimedEffect("repeat_projectiles", 90f)]
    public sealed class RepeatProjectiles : MonoBehaviour
    {
        [EffectConfig]
        static readonly ConfigHolder<int> _extraSpawnCountConfig =
            ConfigFactory<int>.CreateConfig("Additional Projectile Spawn Count", 5)
                              .Description("How many additional projectiles should be spawned per projectile")
                              .AcceptableValues(new AcceptableValueRange<int>(1, byte.MaxValue))
                              .OptionConfig(new IntFieldConfig { Min = 1, Max = byte.MaxValue })
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
                projectileModificationProvider.AdditionalSpawnCountConfigBinding.BindToConfig(_extraSpawnCountConfig);

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
