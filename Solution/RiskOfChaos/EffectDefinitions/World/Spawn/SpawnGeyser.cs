using RiskOfChaos.Content;
using RiskOfChaos.Content.AssetCollections;
using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Methods;
using RiskOfChaos.EffectHandling.EffectComponents;
using RiskOfChaos.Networking.Components;
using RiskOfChaos.Utilities;
using RiskOfChaos.Utilities.Extensions;
using RoR2;
using RoR2.Navigation;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Networking;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace RiskOfChaos.EffectDefinitions.World.Spawn
{
    [ChaosEffect("spawn_geyser")]
    public sealed class SpawnGeyser : NetworkBehaviour
    {
        static readonly SpawnPool<SpawnCard> _spawnPool = new SpawnPool<SpawnCard>();

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

            asyncOperations.EnsureCapacity(asyncOperations.Count + geyserCount);

            GameObject[] geyserPrefabs = new GameObject[geyserCount];
            for (int i = 0; i < geyserCount; i++)
            {
                int prefabIndex = i;

                AsyncOperationHandle<GameObject> geyserLoad = Addressables.LoadAssetAsync<GameObject>(geyserPrefabPaths[i]);
                geyserLoad.OnSuccess(geyserPrefab =>
                {
                    GameObject geyserHolder = Prefabs.CreateNetworkedPrefab("Networked" + geyserPrefab.name, [
                        typeof(SyncJumpVolumeVelocity)
                    ]);

                    GameObject geyser = GameObject.Instantiate(geyserPrefab, geyserHolder.transform);
                    geyser.transform.localPosition = Vector3.zero;

                    geyserPrefabs[prefabIndex] = geyserHolder;

                    prefabs.Add(geyserHolder);
                });

                asyncOperations.Add(geyserLoad);
            }

            yield return asyncOperations.WaitForAllLoaded();

            _spawnPool.EnsureCapacity(geyserPrefabs.Length);

            foreach (GameObject geyserPrefab in geyserPrefabs)
            {
                if (!geyserPrefab)
                    continue;

                SpawnCard spawnCard = ScriptableObject.CreateInstance<SpawnCard>();

                spawnCard.name = $"sc{geyserPrefab.name}";
                spawnCard.prefab = geyserPrefab;
                spawnCard.sendOverNetwork = true;
                spawnCard.hullSize = HullClassification.Human;
                spawnCard.nodeGraphType = MapNodeGroup.GraphType.Ground;
                spawnCard.occupyPosition = true;

                _spawnPool.AddEntry(spawnCard, 1f);
            }

            _spawnPool.TrimExcess();
        }

        [EffectCanActivate]
        static bool CanActivate()
        {
            return _spawnPool.AnyAvailable;
        }

        ChaosEffectComponent _effectComponent;

        Xoroshiro128Plus _rng;

        void Awake()
        {
            _effectComponent = GetComponent<ChaosEffectComponent>();
        }

        public override void OnStartServer()
        {
            base.OnStartServer();

            _rng = new Xoroshiro128Plus(_effectComponent.Rng.nextUlong);
        }

        void Start()
        {
            if (!NetworkServer.active)
                return;

            SpawnCard geyserSpawnCard = _spawnPool.PickRandomEntry(_rng);

            foreach (PlayerCharacterMasterController playerMaster in PlayerCharacterMasterController.instances)
            {
                if (!playerMaster.isConnected)
                    continue;

                CharacterMaster master = playerMaster.master;
                if (!master || master.IsDeadAndOutOfLivesServer())
                    continue;

                if (!master.TryGetBodyPosition(out Vector3 spawnPosition))
                    continue;

                DirectorPlacementRule placementRule = new DirectorPlacementRule
                {
                    position = spawnPosition,
                    minDistance = 2f,
                    maxDistance = float.PositiveInfinity,
                    placementMode = SpawnUtils.ExtraPlacementModes.NearestNodeWithConditions
                };

                DirectorSpawnRequest spawnRequest = new DirectorSpawnRequest(geyserSpawnCard, placementRule, _rng);

                Xoroshiro128Plus geyserRNG = _rng.Branch();
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
                    position = spawnPosition,
                    placementMode = DirectorPlacementRule.PlacementMode.Direct
                });
            }
        }
    }
}
