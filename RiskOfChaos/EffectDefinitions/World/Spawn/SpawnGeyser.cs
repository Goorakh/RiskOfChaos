using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Methods;
using RiskOfChaos.Utilities;
using RiskOfChaos.Utilities.Extensions;
using RoR2;
using RoR2.Navigation;
using System.Linq;
using UnityEngine;

namespace RiskOfChaos.EffectDefinitions.World.Spawn
{
    [ChaosEffect("spawn_geyser")]
    public sealed class SpawnGeyser : GenericDirectorSpawnEffect<InteractableSpawnCard>
    {
        static readonly SpawnCardEntry[] _spawnEntries = NetPrefabs.GeyserPrefabs.Select(p =>
        {
            InteractableSpawnCard spawnCard = ScriptableObject.CreateInstance<InteractableSpawnCard>();

            spawnCard.name = $"sc{p.name}";
            spawnCard.prefab = p;
            spawnCard.sendOverNetwork = true;
            spawnCard.hullSize = HullClassification.Human;
            spawnCard.nodeGraphType = MapNodeGroup.GraphType.Ground;
            spawnCard.occupyPosition = true;

            spawnCard.orientToFloor = true;

            return new SpawnCardEntry(spawnCard, 1f);
        }).ToArray();

        [EffectCanActivate]
        static bool CanActivate()
        {
            return areAnyAvailable(_spawnEntries);
        }

        public override void OnStart()
        {
            InteractableSpawnCard geyserSpawnCard = getItemToSpawn(_spawnEntries, RNG);

            foreach (CharacterBody body in PlayerUtils.GetAllPlayerBodies(true))
            {
                DirectorPlacementRule placementRule = new DirectorPlacementRule
                {
                    position = body.footPosition,
                    minDistance = 0f,
                    maxDistance = float.PositiveInfinity,
                    placementMode = SpawnUtils.ExtraPlacementModes.NearestNodeWithConditions
                };

                DirectorSpawnRequest spawnRequest = new DirectorSpawnRequest(geyserSpawnCard, placementRule, RNG.Branch());

                Xoroshiro128Plus geyserRNG = RNG.Branch();
                spawnRequest.onSpawnedServer = result =>
                {
                    if (!result.success || !result.spawnedInstance)
                        return;

                    JumpVolume jumpVolume = result.spawnedInstance.GetComponentInChildren<JumpVolume>();
                    if (jumpVolume)
                    {
                        const float MAX_DEVIATION = 50f;

                        Quaternion deviation = Quaternion.AngleAxis(geyserRNG.RangeFloat(-MAX_DEVIATION, MAX_DEVIATION), Vector3.right) *
                                               Quaternion.AngleAxis(geyserRNG.RangeFloat(-180f, 180f), Vector3.up);

                        jumpVolume.jumpVelocity = deviation * (Vector3.up * geyserRNG.RangeFloat(30f, 90f));
                    }
                };

                spawnRequest.SpawnWithFallbackPlacement(new DirectorPlacementRule
                {
                    position = body.footPosition,
                    placementMode = DirectorPlacementRule.PlacementMode.Direct
                });
            }
        }
    }
}
