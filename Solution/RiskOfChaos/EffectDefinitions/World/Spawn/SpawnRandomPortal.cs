using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Methods;
using RiskOfChaos.EffectHandling.EffectComponents;
using RiskOfChaos.Utilities;
using RiskOfChaos.Utilities.Extensions;
using RoR2;
using UnityEngine.Networking;

namespace RiskOfChaos.EffectDefinitions.World.Spawn
{
    [ChaosEffect("spawn_random_portal")]
    public sealed class SpawnRandomPortal : NetworkBehaviour
    {
        static readonly SpawnPool<InteractableSpawnCard> _spawnPool = new SpawnPool<InteractableSpawnCard>
        {
            RequiredExpansionsProvider = SpawnPoolUtils.InteractableSpawnCardExpansionsProvider
        };

        [SystemInitializer]
        static void Init()
        {
            _spawnPool.EnsureCapacity(10);

            _spawnPool.AddAssetEntry("RoR2/Base/PortalGoldshores/iscGoldshoresPortal.asset", 1.2f);
            _spawnPool.AddAssetEntry("RoR2/Base/PortalMS/iscMSPortal.asset", 1.2f);
            _spawnPool.AddAssetEntry("RoR2/Base/PortalShop/iscShopPortal.asset", 1.2f);
            _spawnPool.AddAssetEntry("RoR2/DLC1/GameModes/InfiniteTowerRun/InfiniteTowerAssets/iscInfiniteTowerPortal.asset", 1f);
            _spawnPool.AddAssetEntry("RoR2/DLC1/DeepVoidPortal/iscDeepVoidPortal.asset", 0.8f);
            _spawnPool.AddAssetEntry("RoR2/DLC1/PortalVoid/iscVoidPortal.asset", 0.8f);
            _spawnPool.AddAssetEntry("RoR2/DLC1/VoidOutroPortal/iscVoidOutroPortal.asset", 0.8f);
            _spawnPool.AddAssetEntry("RoR2/DLC2/iscDestinationPortal.asset", 0.8f);
            _spawnPool.AddAssetEntry("RoR2/DLC2/iscColossusPortal.asset", 1f);
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

            spawnRequest.SpawnWithFallbackPlacement(SpawnUtils.GetBestValidRandomPlacementRule());
        }
    }
}
