using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Methods;
using RiskOfChaos.EffectHandling.EffectComponents;
using RiskOfChaos.Utilities;
using RiskOfChaos.Utilities.Extensions;
using RoR2;
using RoR2.Navigation;
using RoR2.Projectile;
using System.Collections;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Networking;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace RiskOfChaos.EffectDefinitions.World.Spawn.Projectile
{
    [ChaosEffect("spawn_airstrikes")]
    public sealed class SpawnAirstrikes : NetworkBehaviour
    {
        static readonly SpawnUtils.NodeSelectionRules _strikePositionSelectorRules = new SpawnUtils.NodeSelectionRules(SpawnUtils.NodeGraphFlags.Ground, false, HullMask.Human, NodeFlags.None, NodeFlags.None);

        static GameObject _diabloStrikePrefab;
        static GameObject _orbitalStrikePrefab;

        [SystemInitializer]
        static void Init()
        {
            AsyncOperationHandle<GameObject> diabloStrikeLoad = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/Captain/CaptainAirstrikeAltProjectile.prefab");
            diabloStrikeLoad.OnSuccess(diabloStrikePrefab => _diabloStrikePrefab = diabloStrikePrefab);

            AsyncOperationHandle<GameObject> orbitalStrikeLoad = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/Captain/CaptainAirstrikeProjectile1.prefab");
            orbitalStrikeLoad.OnSuccess(orbitalStrikePrefab => _orbitalStrikePrefab = orbitalStrikePrefab);
        }

        [EffectCanActivate]
        static bool CanActivate()
        {
            return _diabloStrikePrefab && _orbitalStrikePrefab && DirectorCore.instance && ProjectileManager.instance && SpawnUtils.GetNodes(_strikePositionSelectorRules).Count > 0;
        }

        ChaosEffectComponent _effectComponent;

        Xoroshiro128Plus _rng;

        bool _diaboStrikesFinished;
        bool _orbitalStrikesFinished;

        void Awake()
        {
            _effectComponent = GetComponent<ChaosEffectComponent>();
            _effectComponent.EffectDestructionHandledByComponent = true;
        }

        public override void OnStartServer()
        {
            base.OnStartServer();

            _rng = new Xoroshiro128Plus(_effectComponent.Rng.nextUlong);
        }

        void Start()
        {
            if (NetworkServer.active)
            {
                StartCoroutine(fireDiabloStrikes(new Xoroshiro128Plus(_rng.nextUlong)));
                StartCoroutine(fireOrbitalStrikes(new Xoroshiro128Plus(_rng.nextUlong)));
            }
        }

        void FixedUpdate()
        {
            if (NetworkServer.active && _diaboStrikesFinished && _orbitalStrikesFinished)
            {
                _effectComponent.RetireEffect();
            }
        }

        IEnumerator fireDiabloStrikes(Xoroshiro128Plus rng)
        {
            foreach (Vector3 position in SpawnUtils.GenerateDistributedSpawnPositions(_strikePositionSelectorRules,
                                                                                      0.03f,
                                                                                      rng))
            {
                Quaternion rotation = QuaternionUtils.PointLocalDirectionAt(Vector3.up, SpawnUtils.GetEnvironmentNormalAtPoint(position))
                                    * QuaternionUtils.RandomDeviation(5f, rng);

                ProjectileManager.instance.FireProjectile(new FireProjectileInfo
                {
                    projectilePrefab = _diabloStrikePrefab,
                    position = position,
                    rotation = rotation,
                    damage = 400f * 20f * Run.instance.teamlessDamageCoefficient
                });

                yield return new WaitForSeconds(rng.RangeFloat(0.05f, 0.25f));
            }

            _diaboStrikesFinished = true;
        }

        IEnumerator fireOrbitalStrikes(Xoroshiro128Plus rng)
        {
            yield return new WaitForSeconds(15f);

            foreach (Vector3 position in SpawnUtils.GenerateDistributedSpawnPositions(_strikePositionSelectorRules,
                                                                                      0.075f,
                                                                                      rng))
            {
                ProjectileManager.instance.FireProjectile(new FireProjectileInfo
                {
                    projectilePrefab = _orbitalStrikePrefab,
                    position = position,
                    rotation = Quaternion.Euler(0f, RoR2Application.rng.RangeFloat(0f, 360f), 0f),
                    damage = 10f * 20f * Run.instance.teamlessDamageCoefficient
                });

                yield return new WaitForSeconds(rng.RangeFloat(0f, 0.1f));
            }

            _orbitalStrikesFinished = true;
        }
    }
}
