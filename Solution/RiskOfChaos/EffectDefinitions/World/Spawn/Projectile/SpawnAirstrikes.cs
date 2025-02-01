using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Methods;
using RiskOfChaos.EffectHandling.EffectComponents;
using RiskOfChaos.Utilities;
using RoR2;
using RoR2.Navigation;
using RoR2.Projectile;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

namespace RiskOfChaos.EffectDefinitions.World.Spawn.Projectile
{
    [ChaosEffect("spawn_airstrikes")]
    public sealed class SpawnAirstrikes : NetworkBehaviour
    {
        static readonly SpawnUtils.NodeSelectionRules _strikePositionSelectorRules = new SpawnUtils.NodeSelectionRules(SpawnUtils.NodeGraphFlags.Ground, false, HullMask.Human, NodeFlags.None, NodeFlags.None);

        static GameObject _diabloStrikePrefab;
        static GameObject _orbitalStrikePrefab;

        [SystemInitializer(typeof(ProjectileCatalog))]
        static void Init()
        {
            _diabloStrikePrefab = ProjectileCatalog.GetProjectilePrefab(ProjectileCatalog.FindProjectileIndex("CaptainAirstrikeAltProjectile"));
            if (!_diabloStrikePrefab)
            {
                Log.Error("Failed to find diablo strike projectile prefab");
            }

            _orbitalStrikePrefab = ProjectileCatalog.GetProjectilePrefab(ProjectileCatalog.FindProjectileIndex("CaptainAirstrikeProjectile1"));
            if (!_orbitalStrikePrefab)
            {
                Log.Error("Failed to find orbital strike projectile prefab");
            }
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
                Vector3 up = VectorUtils.Spread(SpawnUtils.GetEnvironmentNormalAtPoint(position), 5f, rng);

                Quaternion rotation = QuaternionUtils.PointLocalDirectionAt(Vector3.up, up);

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
