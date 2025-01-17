using RiskOfChaos.Content;
using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Methods;
using RiskOfChaos.ModificationController;
using RiskOfChaos.ModificationController.Projectile;
using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace RiskOfChaos.EffectDefinitions.Character
{
    [ChaosTimedEffect("all_attacks_grenades", 90f, AllowDuplicates = false)]
    public sealed class AllAttacksGrenades : MonoBehaviour
    {
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

                projectileModificationProvider.OverrideProjectileIndex = ProjectileCatalog.GetProjectileIndex(RoCContent.ProjectilePrefabs.GrenadeReplacedProjectile);

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
