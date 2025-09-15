using HG.Coroutines;
using RiskOfChaos.Components;
using RiskOfChaos.Content;
using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Methods;
using RiskOfChaos.EffectHandling.EffectComponents;
using RiskOfChaos.Networking.Components;
using RiskOfChaos.Utilities;
using RiskOfChaos.Utilities.Extensions;
using RoR2;
using RoR2.ContentManagement;
using RoR2.Navigation;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace RiskOfChaos.EffectDefinitions.World.Spawn
{
    [ChaosEffect("spawn_geyser")]
    public sealed class SpawnGeyser : NetworkBehaviour
    {
        static readonly SpawnPool<SpawnCard> _spawnPool = new SpawnPool<SpawnCard>();

        [ContentInitializer]
        static IEnumerator LoadContent(ContentIntializerArgs args)
        {
            ParallelProgressCoroutine parallelCoroutine = new ParallelProgressCoroutine(args.ProgressReceiver);

            string[] geyserPrefabGuids = [
                AddressableGuids.RoR2_Base_artifactworld_AWGeyser_prefab,
                AddressableGuids.RoR2_Base_Common_Props_Geyser_prefab,
                //"RoR2/Base/frozenwall/FW_HumanFan.prefab",
                AddressableGuids.RoR2_Base_moon_MoonGeyser_prefab,
                //"RoR2/Base/moon2/MoonElevator.prefab",
                AddressableGuids.RoR2_Base_rootjungle_RJ_BounceShroom_prefab,
                AddressableGuids.RoR2_DLC1_ancientloft_AL_Geyser_prefab,
                AddressableGuids.RoR2_DLC1_snowyforest_SFGeyser_prefab,
                AddressableGuids.RoR2_DLC1_voidstage_mdlVoidGravityGeyser_prefab,
                AddressableGuids.RoR2_DLC2_meridian_PMLaunchPad_prefab
            ];

            int geyserCount = geyserPrefabGuids.Length;

            GameObject[] geyserPrefabs = new GameObject[geyserCount];
            for (int i = 0; i < geyserCount; i++)
            {
                int prefabIndex = i;

                AsyncOperationHandle<GameObject> geyserLoad = AddressableUtil.LoadTempAssetAsync<GameObject>(geyserPrefabGuids[i]);
                geyserLoad.OnSuccess(geyserPrefab =>
                {
                    GameObject geyserHolder = Prefabs.CreateNetworkedPrefab("Networked" + geyserPrefab.name, [
                        typeof(SyncJumpVolumeVelocity),
                        typeof(GrantTemporaryItemsOnJump)
                    ]);

                    GameObject geyser = Instantiate(geyserPrefab, geyserHolder.transform);
                    geyser.transform.localPosition = Vector3.zero;

                    JumpVolume geyserJumpVolume = geyser.GetComponentInChildren<JumpVolume>();

                    SyncJumpVolumeVelocity syncJumpVolumeVelocity = geyserHolder.GetComponent<SyncJumpVolumeVelocity>();
                    syncJumpVolumeVelocity.JumpVolume = geyserJumpVolume;

                    GrantTemporaryItemsOnJump grantItemsOnJump = geyserHolder.GetComponent<GrantTemporaryItemsOnJump>();
                    grantItemsOnJump.JumpVolume = geyserJumpVolume;
                    grantItemsOnJump.Items = [
                        new GrantTemporaryItemsOnJump.ConditionalItem
                        {
                            ItemDef = AddressableUtil.LoadTempAssetAsync<ItemDef>(AddressableGuids.RoR2_Base_Feather_Feather_asset).WaitForCompletion(),
                            GrantToPlayers = true,
                            IgnoreIfItemAlreadyPresent = true,
                            NotifyPickupIfNoneActive = true,
                        },
                        new GrantTemporaryItemsOnJump.ConditionalItem
                        {
                            ItemDef = AddressableUtil.LoadTempAssetAsync<ItemDef>(AddressableGuids.RoR2_Base_TeleportWhenOob_TeleportWhenOob_asset).WaitForCompletion(),
                            GrantToInvincibleLemurian = true,
                            IgnoreIfItemAlreadyPresent = true,
                        }
                    ];

                    geyserPrefabs[prefabIndex] = geyserHolder;

                    args.ContentPack.networkedObjectPrefabs.Add([geyserHolder]);
                });

                parallelCoroutine.Add(geyserLoad);
            }

            yield return parallelCoroutine;

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

                _spawnPool.AddEntry(spawnCard, new SpawnPoolEntryParameters(1f));
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
        AssetOrDirectReference<SpawnCard> _geyserSpawnCardRef;

        void Awake()
        {
            _effectComponent = GetComponent<ChaosEffectComponent>();
        }

        void OnDestroy()
        {
            _geyserSpawnCardRef?.Reset();
        }

        public override void OnStartServer()
        {
            base.OnStartServer();

            _rng = new Xoroshiro128Plus(_effectComponent.Rng.nextUlong);

            _geyserSpawnCardRef = _spawnPool.PickRandomEntry(_rng);
            _geyserSpawnCardRef.CallOnLoaded(onGeyserSpawnCardLoaded);
        }

        [Server]
        void onGeyserSpawnCardLoaded(SpawnCard geyserSpawnCard)
        {
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
