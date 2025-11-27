using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Methods;
using RiskOfChaos.EffectHandling.EffectComponents;
using RiskOfChaos.Utilities;
using RiskOfChaos.Utilities.Extensions;
using RoR2;
using RoR2.ContentManagement;
using UnityEngine.Networking;

namespace RiskOfChaos.EffectDefinitions.World.Spawn
{
    [ChaosEffect("spawn_random_portal")]
    public sealed class SpawnRandomPortal : NetworkBehaviour
    {
        static readonly SpawnPool<InteractableSpawnCard> _spawnPool = new SpawnPool<InteractableSpawnCard>();

        [SystemInitializer(typeof(ExpansionUtils))]
        static void Init()
        {
            _spawnPool.EnsureCapacity(10);

            _spawnPool.AddAssetEntry(AddressableGuids.RoR2_Base_PortalGoldshores_iscGoldshoresPortal_asset, new SpawnPoolEntryParameters(1.2f));
            _spawnPool.AddAssetEntry(AddressableGuids.RoR2_Base_PortalMS_iscMSPortal_asset, new SpawnPoolEntryParameters(1.2f));
            _spawnPool.AddAssetEntry(AddressableGuids.RoR2_Base_PortalShop_iscShopPortal_asset, new SpawnPoolEntryParameters(1.2f));
            _spawnPool.AddAssetEntry(AddressableGuids.RoR2_DLC1_GameModes_InfiniteTowerRun_ITAssets_iscInfiniteTowerPortal_asset, new SpawnPoolEntryParameters(1f, ExpansionUtils.DLC1));
            _spawnPool.AddAssetEntry(AddressableGuids.RoR2_DLC1_DeepVoidPortal_iscDeepVoidPortal_asset, new SpawnPoolEntryParameters(0.8f, ExpansionUtils.DLC1));
            _spawnPool.AddAssetEntry(AddressableGuids.RoR2_DLC1_PortalVoid_iscVoidPortal_asset, new SpawnPoolEntryParameters(0.8f, ExpansionUtils.DLC1));
            _spawnPool.AddAssetEntry(AddressableGuids.RoR2_DLC1_VoidOutroPortal_iscVoidOutroPortal_asset, new SpawnPoolEntryParameters(0.8f, ExpansionUtils.DLC1));
            _spawnPool.AddAssetEntry(AddressableGuids.RoR2_DLC2_iscDestinationPortal_asset, new SpawnPoolEntryParameters(0.8f, ExpansionUtils.DLC2));
            _spawnPool.AddAssetEntry(AddressableGuids.RoR2_DLC2_iscColossusPortal_asset, new SpawnPoolEntryParameters(1f, ExpansionUtils.DLC2));
        }

        [EffectCanActivate]
        static bool CanActivate()
        {
            return _spawnPool.AnyAvailable;
        }

        ChaosEffectComponent _effectComponent;

        Xoroshiro128Plus _rng;

        AssetOrDirectReference<InteractableSpawnCard> _portalSpawnCardRef;

        void Awake()
        {
            _effectComponent = GetComponent<ChaosEffectComponent>();
        }

        void OnDestroy()
        {
            _portalSpawnCardRef?.Reset();
        }

        public override void OnStartServer()
        {
            base.OnStartServer();

            _rng = new Xoroshiro128Plus(_effectComponent.Rng.nextUlong);

            _portalSpawnCardRef = _spawnPool.PickRandomEntry(_rng);
            _portalSpawnCardRef.CallOnLoaded(onSpawnCardLoaded);
        }

        [Server]
        void onSpawnCardLoaded(InteractableSpawnCard spawnCard)
        {
            DirectorPlacementRule placementRule = SpawnUtils.GetPlacementRule_AtRandomPlayerNearestNode(_rng);
            DirectorSpawnRequest spawnRequest = new DirectorSpawnRequest(spawnCard, placementRule, _rng);

            spawnRequest.SpawnWithFallbackPlacement(SpawnUtils.GetBestValidRandomPlacementRule());
        }
    }
}
