using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Methods;
using RiskOfChaos.Utilities;
using RoR2;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;

namespace RiskOfChaos.EffectDefinitions.World.Spawn
{
    [ChaosEffect("spawn_geyser")]
    public sealed class SpawnGeyser : GenericSpawnEffect<GameObject>
    {
        static readonly SpawnEntry[] _spawnEntries = NetPrefabs.GeyserPrefabs.Select(p => new SpawnEntry(p, 1f)).ToArray();

        [EffectCanActivate]
        static bool CanActivate()
        {
            return areAnyAvailable(_spawnEntries);
        }

        public override void OnStart()
        {
            GameObject geyserPrefab = getItemToSpawn(_spawnEntries, RNG);

            foreach (CharacterBody body in PlayerUtils.GetAllPlayerBodies(true))
            {
                DirectorPlacementRule placementRule = new DirectorPlacementRule
                {
                    position = body.footPosition,
                    placementMode = DirectorPlacementRule.PlacementMode.NearestNode
                };

                GameObject geyser = GameObject.Instantiate(geyserPrefab);
                geyser.transform.position = placementRule.EvaluateToPosition(RNG);

                JumpVolume jumpVolume = geyser.GetComponentInChildren<JumpVolume>();
                if (jumpVolume)
                {
                    const float MAX_DEVIATION = 50f;

                    Quaternion deviation = Quaternion.AngleAxis(RNG.RangeFloat(-MAX_DEVIATION, MAX_DEVIATION), Vector3.right) *
                                           Quaternion.AngleAxis(RNG.RangeFloat(-180f, 180f), Vector3.up);

                    jumpVolume.jumpVelocity = deviation * (Vector3.up * RNG.RangeFloat(30f, 90f));
                }

                NetworkServer.Spawn(geyser);
            }
        }
    }
}
