using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Methods;
using RiskOfChaos.EffectHandling.EffectComponents;
using RiskOfChaos.Patches;
using RiskOfChaos.Utilities;
using RiskOfChaos.Utilities.Extensions;
using RoR2;
using RoR2.ContentManagement;
using UnityEngine;
using UnityEngine.Networking;

namespace RiskOfChaos.EffectDefinitions.World.Spawn
{
    [ChaosEffect("spawn_scav_bag", DefaultSelectionWeight = 0.6f)]
    public sealed class SpawnScavBag : NetworkBehaviour
    {
        static readonly SpawnPool<InteractableSpawnCard> _spawnPool = new SpawnPool<InteractableSpawnCard>();

        [SystemInitializer]
        static void Init()
        {
            _spawnPool.EnsureCapacity(2);

            _spawnPool.AddAssetEntry(AddressableGuids.RoR2_Base_Scav_iscScavBackpack_asset, new SpawnPoolEntryParameters(0.8f));
            _spawnPool.AddAssetEntry(AddressableGuids.RoR2_Base_Scav_iscScavLunarBackpack_asset, new SpawnPoolEntryParameters(0.2f));
        }

        [EffectCanActivate]
        static bool CanActivate()
        {
            return _spawnPool.AnyAvailable;
        }

        ChaosEffectComponent _effectComponent;

        Xoroshiro128Plus _rng;

        AssetOrDirectReference<InteractableSpawnCard> _scavBagSpawnCardRef;

        void Awake()
        {
            _effectComponent = GetComponent<ChaosEffectComponent>();
        }

        void OnDestroy()
        {
            _scavBagSpawnCardRef?.Reset();
        }

        public override void OnStartServer()
        {
            base.OnStartServer();

            _rng = new Xoroshiro128Plus(_effectComponent.Rng.nextUlong);

            _scavBagSpawnCardRef = _spawnPool.PickRandomEntry(_rng);
            _scavBagSpawnCardRef.CallOnLoaded(onSpawnCardLoaded);
        }

        [Server]
        void onSpawnCardLoaded(InteractableSpawnCard spawnCard)
        {
            Xoroshiro128Plus spawnRng = new Xoroshiro128Plus(_rng.nextUlong);

            DirectorPlacementRule placementRule = SpawnUtils.GetPlacementRule_AtRandomPlayerNearestNode(spawnRng);

            DirectorSpawnRequest spawnRequest = new DirectorSpawnRequest(spawnCard, placementRule, spawnRng)
            {
                onSpawnedServer = onBagSpawnedServer
            };

            spawnRequest.SpawnWithFallbackPlacement(SpawnUtils.GetBestValidRandomPlacementRule());

            void onBagSpawnedServer(SpawnCard.SpawnResult spawnResult)
            {
                GameObject scavBagObj = spawnResult.spawnedInstance;
                if (scavBagObj && Configs.EffectSelection.SeededEffectSelection.Value)
                {
                    RNGOverridePatch.OverrideRNG(scavBagObj, new Xoroshiro128Plus(spawnRng.nextUlong));
                }
            }
        }
    }
}
