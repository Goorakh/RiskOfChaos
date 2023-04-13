using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Methods;
using RiskOfChaos.Utilities;
using RoR2;
using RoR2.Projectile;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace RiskOfChaos.EffectDefinitions.World.Spawn.Projectile
{
    [ChaosEffect("spawn_void_implosion")]
    public sealed class SpawnVoidImplosion : BaseEffect
    {
        readonly struct ImplosionProjectileInfo
        {
            public readonly GameObject ProjectilePrefab;
            public readonly float Weight;

            public readonly bool IsAvailable => ProjectilePrefab && ExpansionUtils.IsObjectExpansionAvailable(ProjectilePrefab);

            public ImplosionProjectileInfo(GameObject prefab, float weight)
            {
                ProjectilePrefab = prefab;
                Weight = weight;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static ImplosionProjectileInfo LoadFromAddressablePath(string path, float weight)
            {
                return new ImplosionProjectileInfo(Addressables.LoadAssetAsync<GameObject>(path).WaitForCompletion(), weight);
            }
        }
        static ImplosionProjectileInfo[] _availableProjectiles;

        [SystemInitializer]
        static void Init()
        {
            _availableProjectiles = new ImplosionProjectileInfo[]
            {
                ImplosionProjectileInfo.LoadFromAddressablePath("RoR2/Base/Nullifier/NullifierDeathBombProjectile.prefab", 1f),
                ImplosionProjectileInfo.LoadFromAddressablePath("RoR2/DLC1/VoidJailer/VoidJailerDeathBombProjectile.prefab", 0.4f),
                ImplosionProjectileInfo.LoadFromAddressablePath("RoR2/DLC1/VoidMegaCrab/VoidMegaCrabDeathBombProjectile.prefab", 0.4f)
            };
        }

        [EffectCanActivate]
        static bool CanActivate()
        {
            return _availableProjectiles != null && _availableProjectiles.Any(i => i.IsAvailable);
        }

        static WeightedSelection<GameObject> getProjectilePrefabSelection()
        {
            WeightedSelection<GameObject> weightedSelection = new WeightedSelection<GameObject>(_availableProjectiles.Length);
            foreach (ImplosionProjectileInfo projectileInfo in _availableProjectiles)
            {
                if (projectileInfo.IsAvailable)
                {
                    weightedSelection.AddChoice(projectileInfo.ProjectilePrefab, projectileInfo.Weight);
                }
            }

            return weightedSelection;
        }

        public override void OnStart()
        {
            WeightedSelection<GameObject> projectilePrefabSelection = getProjectilePrefabSelection();

            foreach (CharacterBody playerBody in PlayerUtils.GetAllPlayerBodies(true))
            {
                ProjectileManager.instance.FireProjectile(new FireProjectileInfo
                {
                    projectilePrefab = projectilePrefabSelection.Evaluate(RNG.nextNormalizedFloat),
                    position = playerBody.corePosition
                });
            }
        }
    }
}
