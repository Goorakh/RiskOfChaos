using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Methods;
using RiskOfChaos.Utilities;
using RoR2;
using RoR2.Projectile;
using System.Collections;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace RiskOfChaos.EffectDefinitions.World.Spawn.Projectile
{
    [ChaosEffect("spawn_airstrikes", DefaultSelectionWeight = 0.9f, EffectWeightReductionPercentagePerActivation = 10f)]
    public sealed class SpawnAirstrikes : BaseEffect, ICoroutineEffect
    {
        static GameObject _diabloStrikePrefab;

        [SystemInitializer]
        static void Init()
        {
            AsyncOperationHandle<GameObject> diabloStrikeHandle = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/Captain/CaptainAirstrikeAltProjectile.prefab");
            diabloStrikeHandle.Completed += static diabloStrikeHandle =>
            {
                _diabloStrikePrefab = diabloStrikeHandle.Result;
            };
        }

        [EffectCanActivate]
        static bool CanActivate()
        {
            return _diabloStrikePrefab && DirectorCore.instance && ProjectileManager.instance;
        }

        public override void OnStart()
        {
        }

        public IEnumerator OnStartCoroutine()
        {
            const int NUM_SPAWNS = 50;

            DirectorPlacementRule placementRule = SpawnUtils.GetBestValidRandomPlacementRule();

            for (int i = 0; i < NUM_SPAWNS; i++)
            {
                Vector3 spawnPosition = placementRule.EvaluateToPosition(RNG);

                ProjectileManager.instance.FireProjectile(new FireProjectileInfo
                {
                    projectilePrefab = _diabloStrikePrefab,
                    position = spawnPosition,
                    rotation = Quaternion.Euler(RNG.RangeFloat(-5f, 5f), RNG.RangeFloat(0f, 360f), RNG.RangeFloat(-5f, 5f)),
                    damage = 400f * 75f
                });

                yield return new WaitForSeconds(RNG.RangeFloat(0.05f, 0.25f));
            }
        }

        public void OnForceStopped()
        {
        }
    }
}
