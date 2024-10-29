using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Methods;
using RiskOfChaos.EffectHandling.EffectComponents;
using RiskOfChaos.Utilities;
using RoR2;
using RoR2.Projectile;
using UnityEngine;
using UnityEngine.Networking;

namespace RiskOfChaos.EffectDefinitions.World.Spawn.Projectile
{
    [ChaosEffect("spawn_void_implosion")]
    public sealed class SpawnVoidImplosion : NetworkBehaviour
    {
        static readonly SpawnPool<GameObject> _implosionProjectiles = new SpawnPool<GameObject>();

        [SystemInitializer]
        static void Init()
        {
            _implosionProjectiles.AddAssetEntry("RoR2/Base/Nullifier/NullifierDeathBombProjectile.prefab", 1f);
            _implosionProjectiles.AddAssetEntry("RoR2/DLC1/VoidJailer/VoidJailerDeathBombProjectile.prefab", 0.4f);
            _implosionProjectiles.AddAssetEntry("RoR2/DLC1/VoidMegaCrab/VoidMegaCrabDeathBombProjectile.prefab", 0.4f);
        }

        [EffectCanActivate]
        static bool CanActivate()
        {
            return _implosionProjectiles.AnyAvailable;
        }

        ChaosEffectComponent _effectComponent;

        GameObject _projectilePrefab;

        void Awake()
        {
            _effectComponent = GetComponent<ChaosEffectComponent>();
        }

        public override void OnStartServer()
        {
            base.OnStartServer();

            Xoroshiro128Plus rng = new Xoroshiro128Plus(_effectComponent.Rng.nextUlong);

            _projectilePrefab = _implosionProjectiles.PickRandomEntry(rng);
        }

        void Start()
        {
            if (!NetworkServer.active)
                return;

            foreach (CharacterBody playerBody in PlayerUtils.GetAllPlayerBodies(true))
            {
                ProjectileManager.instance.FireProjectile(new FireProjectileInfo
                {
                    projectilePrefab = _projectilePrefab,
                    position = playerBody.corePosition + new Vector3(0f, 5f, 0f)
                });
            }
        }
    }
}
