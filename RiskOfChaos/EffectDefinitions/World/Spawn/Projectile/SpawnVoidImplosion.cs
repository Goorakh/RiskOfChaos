using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Methods;
using RiskOfChaos.Utilities;
using RoR2;
using RoR2.Projectile;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace RiskOfChaos.EffectDefinitions.World.Spawn.Projectile
{
    [ChaosEffect("spawn_void_implosion")]
    public sealed class SpawnVoidImplosion : GenericSpawnEffect<GameObject>
    {
        static SpawnEntry[] _projectileEntries;

        [SystemInitializer]
        static void Init()
        {
            _projectileEntries = new SpawnEntry[]
            {
                loadBasicSpawnEntry("RoR2/Base/Nullifier/NullifierDeathBombProjectile.prefab", 1f),
                loadBasicSpawnEntry("RoR2/DLC1/VoidJailer/VoidJailerDeathBombProjectile.prefab", 0.4f),
                loadBasicSpawnEntry("RoR2/DLC1/VoidMegaCrab/VoidMegaCrabDeathBombProjectile.prefab", 0.4f)
            };
        }

        [EffectCanActivate]
        static bool CanActivate()
        {
            return areAnyAvailable(_projectileEntries);
        }

        public override void OnStart()
        {
            foreach (CharacterBody playerBody in PlayerUtils.GetAllPlayerBodies(true))
            {
                ProjectileManager.instance.FireProjectile(new FireProjectileInfo
                {
                    projectilePrefab = getItemToSpawn(_projectileEntries, RNG),
                    position = playerBody.corePosition
                });
            }
        }
    }
}
