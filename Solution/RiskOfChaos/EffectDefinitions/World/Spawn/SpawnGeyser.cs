using RiskOfChaos.Content;
using RiskOfChaos.Content.AssetCollections;
using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Methods;
using RiskOfChaos.Networking.Components;
using RiskOfChaos.Utilities;
using RiskOfChaos.Utilities.Extensions;
using RoR2;
using RoR2.Navigation;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace RiskOfChaos.EffectDefinitions.World.Spawn
{
    [ChaosEffect("spawn_geyser")]
    public sealed class SpawnGeyser : GenericDirectorSpawnEffect<InteractableSpawnCard>
    {
        static SpawnCardEntry[] _spawnEntries = [];

        [ContentInitializer]
        static IEnumerator LoadContent(NetworkedPrefabAssetCollection prefabs)
        {
            List<AsyncOperationHandle> asyncOperations = [];

            string[] geyserPrefabPaths = [
                "RoR2/Base/artifactworld/AWGeyser.prefab",
                "RoR2/Base/Common/Props/Geyser.prefab",
                //"RoR2/Base/frozenwall/FW_HumanFan.prefab",
                "RoR2/Base/moon/MoonGeyser.prefab",
                //"RoR2/Base/moon2/MoonElevator.prefab",
                "RoR2/Base/rootjungle/RJ_BounceShroom.prefab",
                "RoR2/DLC1/ancientloft/AL_Geyser.prefab",
                "RoR2/DLC1/snowyforest/SFGeyser.prefab",
                "RoR2/DLC1/voidstage/mdlVoidGravityGeyser.prefab",
                "RoR2/DLC2/meridian/PMLaunchPad.prefab"
            ];

            int geyserCount = geyserPrefabPaths.Length;
            GameObject[] geyserPrefabs = new GameObject[geyserCount];
            for (int i = 0; i < geyserCount; i++)
            {
                int prefabIndex = i;

                AsyncOperationHandle<GameObject> geyserLoad = Addressables.LoadAssetAsync<GameObject>(geyserPrefabPaths[i]);
                geyserLoad.Completed += handle =>
                {
                    GameObject geyserPrefab = handle.Result;

                    GameObject geyserHolder = Prefabs.CreateNetworkedPrefab("Networked" + geyserPrefab.name, [
                        typeof(SyncJumpVolumeVelocity)
                    ]);

                    GameObject geyser = GameObject.Instantiate(geyserPrefab, geyserHolder.transform);
                    geyser.transform.localPosition = Vector3.zero;

                    geyserPrefabs[prefabIndex] = geyserHolder;

                    prefabs.Add(geyserHolder);
                };

                asyncOperations.Add(geyserLoad);
            }

            yield return asyncOperations.WaitForAllLoaded();

            List<GameObject> filteredGeyserPrefabs = new List<GameObject>(geyserPrefabs.Length);
            foreach (GameObject prefab in geyserPrefabs)
            {
                if (prefab)
                {
                    filteredGeyserPrefabs.Add(prefab);
                }
            }

            _spawnEntries = new SpawnCardEntry[filteredGeyserPrefabs.Count];
            for (int i = 0; i < _spawnEntries.Length; i++)
            {
                GameObject prefab = filteredGeyserPrefabs[i];

                InteractableSpawnCard spawnCard = ScriptableObject.CreateInstance<InteractableSpawnCard>();

                spawnCard.name = $"sc{prefab.name}";
                spawnCard.prefab = prefab;
                spawnCard.sendOverNetwork = true;
                spawnCard.hullSize = HullClassification.Human;
                spawnCard.nodeGraphType = MapNodeGroup.GraphType.Ground;
                spawnCard.occupyPosition = true;

                _spawnEntries[i] = new SpawnCardEntry(spawnCard, 1f);
            }
        }

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
                    minDistance = 2f,
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
