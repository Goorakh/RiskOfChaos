using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Methods;
using RiskOfChaos.Utilities;
using RoR2;
using UnityEngine.AddressableAssets;

namespace RiskOfChaos.EffectDefinitions.World.Spawn
{
    [ChaosEffect("spawn_scav_bag", DefaultSelectionWeight = 0.6f)]
    public sealed class SpawnScavBag : GenericDirectorSpawnEffect<InteractableSpawnCard>
    {
        static SpawnCardEntry[] _spawnEntries;

        [SystemInitializer]
        static void Init()
        {
            _spawnEntries = new SpawnCardEntry[]
            {
                loadBasicSpawnEntry("RoR2/Base/Scav/iscScavBackpack.asset"),
                loadBasicSpawnEntry("RoR2/Base/Scav/iscScavLunarBackpack.asset", 0.25f)
            };
        }

        [EffectCanActivate]
        static bool CanActivate()
        {
            return areAnyAvailable(_spawnEntries);
        }

        public override void OnStart()
        {
            DirectorSpawnRequest spawnRequest = new DirectorSpawnRequest(getItemToSpawn(_spawnEntries, RNG), SpawnUtils.GetPlacementRule_AtRandomPlayerNearestNode(RNG), new Xoroshiro128Plus(RNG.nextUlong));

            if (!DirectorCore.instance.TrySpawnObject(spawnRequest))
            {
                spawnRequest.placementRule = SpawnUtils.GetBestValidRandomPlacementRule();
                DirectorCore.instance.TrySpawnObject(spawnRequest);
            }
        }
    }
}
