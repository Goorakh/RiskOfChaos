using RiskOfChaos.ConfigHandling;
using RiskOfChaos.ConfigHandling.AcceptableValues;
using RiskOfChaos.Content;
using RiskOfChaos.EffectHandling;
using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Data;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Methods;
using RiskOfChaos.EffectHandling.Formatting;
using RiskOfChaos.ModificationController;
using RiskOfChaos.ModificationController.Projectile;
using RiskOfOptions.OptionConfigs;
using UnityEngine;
using UnityEngine.Networking;

namespace RiskOfChaos.EffectDefinitions.World.Projectile
{
    [ChaosTimedEffect("increase_projectile_speed", TimedEffectType.UntilStageEnd, ConfigName = "Increase Projectile Speed")]
    public sealed class IncreaseProjectileSpeed : MonoBehaviour
    {
        [EffectConfig]
        static readonly ConfigHolder<float> _projectileSpeedIncrease =
            ConfigFactory<float>.CreateConfig("Projectile Speed Increase", 0.5f)
                                .AcceptableValues(new AcceptableValueMin<float>(0f))
                                .OptionConfig(new FloatFieldConfig
                                {
                                    FormatString = "+{0:0.##%}",
                                    Min = 0f
                                })
                                .Build();

        [EffectCanActivate]
        static bool CanActivate()
        {
            return RoCContent.NetworkedPrefabs.ProjectileModificationProvider;
        }

        [GetEffectNameFormatter]
        static EffectNameFormatter GetNameFormatter()
        {
            return new EffectNameFormatter_GenericFloat(_projectileSpeedIncrease) { ValueFormat = "0.##%" };
        }

        ValueModificationController _projectileModificationController;

        void Start()
        {
            if (NetworkServer.active)
            {
                _projectileModificationController = Instantiate(RoCContent.NetworkedPrefabs.ProjectileModificationProvider).GetComponent<ValueModificationController>();

                ProjectileModificationProvider projectileModificationProvider = _projectileModificationController.GetComponent<ProjectileModificationProvider>();
                projectileModificationProvider.SpeedMultiplierConfigBinding.BindToConfig(_projectileSpeedIncrease, v => 1f + v);

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
