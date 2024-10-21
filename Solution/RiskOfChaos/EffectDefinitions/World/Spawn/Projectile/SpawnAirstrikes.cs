using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Methods;
using RiskOfChaos.Utilities;
using RiskOfChaos.Utilities.Extensions;
using RoR2;
using RoR2.Navigation;
using RoR2.Projectile;
using System.Collections;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace RiskOfChaos.EffectDefinitions.World.Spawn.Projectile
{
    [ChaosEffect("spawn_airstrikes", DefaultSelectionWeight = 0.9f)]
    public sealed class SpawnAirstrikes : BaseEffect, ICoroutineEffect
    {
        static readonly SpawnUtils.NodeSelectionRules _strikePositionSelectorRules = new SpawnUtils.NodeSelectionRules(SpawnUtils.NodeGraphFlags.Ground, false, HullMask.Human, NodeFlags.None, NodeFlags.None);

        static GameObject _diabloStrikePrefab;
        static GameObject _orbitalProbePrefab;

        [SystemInitializer]
        static void Init()
        {
            _diabloStrikePrefab = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/Captain/CaptainAirstrikeAltProjectile.prefab").WaitForCompletion();
            _orbitalProbePrefab = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/Captain/CaptainAirstrikeProjectile1.prefab").WaitForCompletion();
        }

        [EffectCanActivate]
        static bool CanActivate()
        {
            return _diabloStrikePrefab && _orbitalProbePrefab && DirectorCore.instance && ProjectileManager.instance && SpawnUtils.GetNodes(_strikePositionSelectorRules).Count > 0;
        }

        public override void OnStart()
        {
        }

        public IEnumerator OnStartCoroutine()
        {
            foreach (Vector3 position in SpawnUtils.GenerateDistributedSpawnPositions(_strikePositionSelectorRules,
                                                                                      0.03f,
                                                                                      RNG.Branch()))
            {
                Quaternion rotation = QuaternionUtils.PointLocalDirectionAt(Vector3.up, SpawnUtils.GetEnvironmentNormalAtPoint(position))
                                    * QuaternionUtils.RandomDeviation(5f, RoR2Application.rng);

                ProjectileManager.instance.FireProjectile(new FireProjectileInfo
                {
                    projectilePrefab = _diabloStrikePrefab,
                    position = position,
                    rotation = rotation,
                    damage = 400f * 20f * Run.instance.teamlessDamageCoefficient
                });

                yield return new WaitForSeconds(RNG.RangeFloat(0.05f, 0.25f));
            }

            yield return new WaitForSeconds(10f);

            foreach (Vector3 position in SpawnUtils.GenerateDistributedSpawnPositions(_strikePositionSelectorRules,
                                                                                      0.075f,
                                                                                      RNG.Branch()))
            {
                ProjectileManager.instance.FireProjectile(new FireProjectileInfo
                {
                    projectilePrefab = _orbitalProbePrefab,
                    position = position,
                    rotation = Quaternion.Euler(0f, RoR2Application.rng.RangeFloat(0f, 360f), 0f),
                    damage = 10f * 20f * Run.instance.teamlessDamageCoefficient
                });

                yield return new WaitForSeconds(RNG.RangeFloat(0f, 0.1f));
            }
        }

        public void OnForceStopped()
        {
        }
    }
}
