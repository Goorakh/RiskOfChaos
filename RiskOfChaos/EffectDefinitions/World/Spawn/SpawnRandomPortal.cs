using RiskOfChaos.EffectHandling;
using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Methods;
using RiskOfChaos.Utilities;
using RoR2;
using UnityEngine.AddressableAssets;

namespace RiskOfChaos.EffectDefinitions.World.Spawn
{
    [ChaosEffect("spawn_random_portal")]
    public sealed class SpawnRandomPortal : GenericDirectorSpawnEffect<InteractableSpawnCard>
    {
        static SpawnCardEntry[] _spawnCards;

        [SystemInitializer]
        static void Init()
        {
            static InteractableSpawnCard loadSpawnCard(string path)
            {
                return Addressables.LoadAssetAsync<InteractableSpawnCard>(path).WaitForCompletion();
            }

            static SpawnCardEntry getEntrySingle(string iscPath, float weight = 1f)
            {
                return new SpawnCardEntry(loadSpawnCard(iscPath), weight);
            }

            _spawnCards = new SpawnCardEntry[]
            {
                getEntrySingle("RoR2/Base/PortalGoldshores/iscGoldshoresPortal.asset"),
                getEntrySingle("RoR2/Base/PortalMS/iscMSPortal.asset"),
                getEntrySingle("RoR2/Base/PortalShop/iscShopPortal.asset"),
                getEntrySingle("RoR2/DLC1/GameModes/InfiniteTowerRun/InfiniteTowerAssets/iscInfiniteTowerPortal.asset"),
                getEntrySingle("RoR2/DLC1/DeepVoidPortal/iscDeepVoidPortal.asset"),
                getEntrySingle("RoR2/DLC1/PortalVoid/iscVoidPortal.asset"),
                getEntrySingle("RoR2/DLC1/VoidOutroPortal/iscVoidOutroPortal.asset")
            };
        }

        [EffectCanActivate]
        static bool CanActivate(EffectCanActivateContext context)
        {
            return areAnyAvailable(_spawnCards);
        }

        public override void OnStart()
        {
            DirectorPlacementRule placementRule = SpawnUtils.GetPlacementRule_AtRandomPlayerNearestNode(new Xoroshiro128Plus(RNG.nextUlong));
            DirectorSpawnRequest spawnRequest = new DirectorSpawnRequest(getItemToSpawn(_spawnCards, RNG), placementRule, new Xoroshiro128Plus(RNG.nextUlong));

            if (!DirectorCore.instance.TrySpawnObject(spawnRequest))
            {
                spawnRequest.placementRule = SpawnUtils.GetBestValidRandomPlacementRule();
                DirectorCore.instance.TrySpawnObject(spawnRequest);
            }
        }
    }
}
