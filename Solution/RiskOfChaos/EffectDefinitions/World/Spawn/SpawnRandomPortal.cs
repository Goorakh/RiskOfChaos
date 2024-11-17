using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Methods;
using RiskOfChaos.EffectHandling.EffectComponents;
using RiskOfChaos.Utilities;
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

        [SystemInitializer(typeof(ExpansionUtils))]
        static void Init()
        {
            _spawnPool.EnsureCapacity(10);

            _spawnPool.AddAssetEntry("RoR2/Base/PortalGoldshores/iscGoldshoresPortal.asset", new SpawnPoolEntryParameters(1.2f));
            _spawnPool.AddAssetEntry("RoR2/Base/PortalMS/iscMSPortal.asset", new SpawnPoolEntryParameters(1.2f));
            _spawnPool.AddAssetEntry("RoR2/Base/PortalShop/iscShopPortal.asset", new SpawnPoolEntryParameters(1.2f));
            _spawnPool.AddAssetEntry("RoR2/DLC1/GameModes/InfiniteTowerRun/InfiniteTowerAssets/iscInfiniteTowerPortal.asset", new SpawnPoolEntryParameters(1f, ExpansionUtils.DLC1));
            _spawnPool.AddAssetEntry("RoR2/DLC1/DeepVoidPortal/iscDeepVoidPortal.asset", new SpawnPoolEntryParameters(0.8f, ExpansionUtils.DLC1));
            _spawnPool.AddAssetEntry("RoR2/DLC1/PortalVoid/iscVoidPortal.asset", new SpawnPoolEntryParameters(0.8f, ExpansionUtils.DLC1));
            _spawnPool.AddAssetEntry("RoR2/DLC1/VoidOutroPortal/iscVoidOutroPortal.asset", new SpawnPoolEntryParameters(0.8f, ExpansionUtils.DLC1));
            _spawnPool.AddAssetEntry("RoR2/DLC2/iscDestinationPortal.asset", new SpawnPoolEntryParameters(0.8f, ExpansionUtils.DLC2));
            _spawnPool.AddAssetEntry("RoR2/DLC2/iscColossusPortal.asset", new SpawnPoolEntryParameters(1f, ExpansionUtils.DLC2));
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
