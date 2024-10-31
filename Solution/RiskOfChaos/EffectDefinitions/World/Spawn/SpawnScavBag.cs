using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Methods;
using RiskOfChaos.EffectHandling.EffectComponents;
using RiskOfChaos.Patches;
using RiskOfChaos.Utilities;
using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace RiskOfChaos.EffectDefinitions.World.Spawn
{
    [ChaosEffect("spawn_scav_bag", DefaultSelectionWeight = 0.6f)]
    public sealed class SpawnScavBag : NetworkBehaviour
    {
        static readonly SpawnPool<InteractableSpawnCard> _spawnPool = new SpawnPool<InteractableSpawnCard>
        {
            RequiredExpansionsProvider = SpawnPoolUtils.InteractableSpawnCardExpansionsProvider
        };

        [SystemInitializer]
        static void Init()
        {
            _spawnPool.EnsureCapacity(2);

            _spawnPool.AddAssetEntry("RoR2/Base/Scav/iscScavBackpack.asset", 0.8f);
            _spawnPool.AddAssetEntry("RoR2/Base/Scav/iscScavLunarBackpack.asset", 0.2f);
        }

        [EffectCanActivate]
        static bool CanActivate()
        {
            return _spawnPool.AnyAvailable;
        }

        ChaosEffectComponent _effectComponent;

        Xoroshiro128Plus _rng;

        InteractableSpawnCard _selectedSpawnCard;

        void Awake()
        {
            _effectComponent = GetComponent<ChaosEffectComponent>();
        }

        public override void OnStartServer()
        {
            base.OnStartServer();

            _rng = new Xoroshiro128Plus(_effectComponent.Rng.nextUlong);

            _selectedSpawnCard = _spawnPool.PickRandomEntry(_rng);
        }

        void Start()
        {
            if (!NetworkServer.active)
                return;

            DirectorPlacementRule placementRule = SpawnUtils.GetPlacementRule_AtRandomPlayerNearestNode(_rng);

            DirectorSpawnRequest spawnRequest = new DirectorSpawnRequest(_selectedSpawnCard, placementRule, _rng);

            GameObject scavBagObj = spawnRequest.SpawnWithFallbackPlacement(SpawnUtils.GetBestValidRandomPlacementRule());
            if (scavBagObj && Configs.EffectSelection.SeededEffectSelection.Value)
            {
                RNGOverridePatch.OverrideRNG(scavBagObj, new Xoroshiro128Plus(_rng.nextUlong));
            }
        }
    }
}
