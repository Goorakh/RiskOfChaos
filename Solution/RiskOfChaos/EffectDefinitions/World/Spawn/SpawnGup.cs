using HG;
using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Methods;
using RiskOfChaos.EffectHandling.EffectComponents;
using RiskOfChaos.Utilities;
using RiskOfChaos.Utilities.Extensions;
using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace RiskOfChaos.EffectDefinitions.World.Spawn
{
    [ChaosEffect("spawn_gup")]
    public sealed class SpawnGup : NetworkBehaviour
    {
        static GameObject _gupPrefab;

        [SystemInitializer(typeof(MasterCatalog))]
        static void Init()
        {
            _gupPrefab = MasterCatalog.FindMasterPrefab("GupMaster");
            if (!_gupPrefab)
            {
                Log.Error("Failed to find gup master prefab");
            }
        }

        [EffectCanActivate]
        static bool CanActivate()
        {
            return _gupPrefab && ExpansionUtils.IsObjectExpansionAvailable(_gupPrefab);
        }

        ChaosEffectComponent _effectComponent;

        Xoroshiro128Plus _rng;

        void Awake()
        {
            _effectComponent = GetComponent<ChaosEffectComponent>();
        }

        public override void OnStartServer()
        {
            base.OnStartServer();

            _rng = new Xoroshiro128Plus(_effectComponent.Rng.nextUlong);
        }

        void Start()
        {
            if (!NetworkServer.active)
                return;

            foreach (PlayerCharacterMasterController playerMaster in PlayerCharacterMasterController.instances)
            {
                if (!playerMaster.isConnected)
                    continue;

                CharacterMaster master = playerMaster.master;
                if (!master || master.IsDeadAndOutOfLivesServer())
                    continue;

                if (!master.TryGetBodyPosition(out Vector3 bodyPosition))
                    continue;

                spawnGupAt(bodyPosition, _rng);
            }
        }

        [Server]
        void spawnGupAt(Vector3 position, Xoroshiro128Plus rng)
        {
            MasterSummon gupSummon = new MasterSummon
            {
                masterPrefab = _gupPrefab,
                teamIndexOverride = TeamIndex.Monster,
                useAmbientLevel = true,
                position = position + new Vector3(0f, 20f, 0f) + (3f * rng.PointInUnitSphere()),
                rotation = Quaternion.identity,
                ignoreTeamMemberLimit = true
            };

            CharacterMaster gupMaster = gupSummon.Perform();
            if (!gupMaster)
                return;

            CharacterBody gupBody = gupMaster.GetBody();
            if (gupBody && gupBody.TryGetComponent(out IPhysMotor physMotor))
            {
                physMotor.ApplyForceImpulse(new PhysForceInfo
                {
                    force = new Vector3(0f, -15f, 0f),
                    massIsOne = true
                });
            }
        }
    }
}
