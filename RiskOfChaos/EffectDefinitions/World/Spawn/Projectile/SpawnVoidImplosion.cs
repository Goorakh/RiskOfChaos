using RiskOfChaos.EffectHandling;
using RiskOfChaos.Utilities;
using RoR2;
using RoR2.Projectile;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace RiskOfChaos.EffectDefinitions.World.Spawn.Projectile
{
    [ChaosEffect("spawn_void_implosion")]
    public class SpawnVoidImplosion : BaseEffect
    {
        static readonly GameObject _nullifierDeathBombProjectilePrefab = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/Nullifier/NullifierDeathBombProjectile.prefab").WaitForCompletion();
        static readonly GameObject _megaCrabDeathBombProjectilePrefab = Addressables.LoadAssetAsync<GameObject>("RoR2/DLC1/VoidMegaCrab/VoidMegaCrabDeathBombProjectile.prefab").WaitForCompletion();

        [EffectCanActivate]
        static bool CanActivate()
        {
            return _nullifierDeathBombProjectilePrefab && (_megaCrabDeathBombProjectilePrefab || !ExpansionUtils.DLC1Enabled);
        }

        public override void OnStart()
        {
            bool dlc1Enabled = ExpansionUtils.DLC1Enabled;

            foreach (CharacterBody playerBody in PlayerUtils.GetAllPlayerBodies(true))
            {
                GameObject projectilePrefab;
                if (dlc1Enabled && RNG.nextNormalizedFloat <= 0.3f)
                {
                    projectilePrefab = _megaCrabDeathBombProjectilePrefab;
                }
                else
                {
                    projectilePrefab = _nullifierDeathBombProjectilePrefab;
                }

                ProjectileManager.instance.FireProjectile(new FireProjectileInfo
                {
                    projectilePrefab = projectilePrefab,
                    position = playerBody.corePosition
                });
            }
        }
    }
}
