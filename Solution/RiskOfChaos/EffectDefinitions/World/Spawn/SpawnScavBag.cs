using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Methods;
using RiskOfChaos.Patches;
using RiskOfChaos.Utilities;
using RiskOfChaos.Utilities.Extensions;
using RoR2;
using UnityEngine;

namespace RiskOfChaos.EffectDefinitions.World.Spawn
{
    [ChaosEffect("spawn_scav_bag", DefaultSelectionWeight = 0.6f)]
    public sealed class SpawnScavBag : GenericDirectorSpawnEffect<InteractableSpawnCard>
    {
        static SpawnCardEntry[] _spawnEntries;

        [SystemInitializer]
        static void Init()
        {
            _spawnEntries = [
                loadBasicSpawnEntry("RoR2/Base/Scav/iscScavBackpack.asset", 0.8f),
                loadBasicSpawnEntry("RoR2/Base/Scav/iscScavLunarBackpack.asset", 0.2f)
            ];
        }

        [EffectCanActivate]
        static bool CanActivate()
        {
            return areAnyAvailable(_spawnEntries);
        }

        public override void OnStart()
        {
            InteractableSpawnCard bagSpawnCard = getItemToSpawn(_spawnEntries, RNG.Branch());
            DirectorPlacementRule placementRule = SpawnUtils.GetPlacementRule_AtRandomPlayerNearestNode(RNG.Branch());

            DirectorSpawnRequest spawnRequest = new DirectorSpawnRequest(bagSpawnCard, placementRule, RNG.Branch());

            GameObject scavBagObj = spawnRequest.SpawnWithFallbackPlacement(SpawnUtils.GetBestValidRandomPlacementRule());
            if (scavBagObj && Configs.EffectSelection.SeededEffectSelection.Value)
            {
                RNGOverridePatch.OverrideRNG(scavBagObj, RNG.Branch());
            }
        }
    }
}
