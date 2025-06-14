using HG;
using RiskOfChaos.Components;
using RiskOfChaos.Content.AssetCollections;
using RiskOfChaos.Networking.Components;
using RiskOfChaos.Utilities;
using RiskOfChaos.Utilities.Extensions;
using RoR2;
using RoR2.ContentManagement;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace RiskOfChaos.Content
{
    public static class Prefabs
    {
        static GameObject _prefabParentObject;

        static Transform getPrefabParent()
        {
            if (!_prefabParentObject)
            {
                _prefabParentObject = new GameObject(Main.PluginGUID + "_PrefabParent");
                GameObject.DontDestroyOnLoad(_prefabParentObject);
                _prefabParentObject.SetActive(false);

                On.RoR2.Util.IsPrefab += isPrefabHook;

                static bool isPrefabHook(On.RoR2.Util.orig_IsPrefab orig, GameObject gameObject)
                {
                    return orig(gameObject) || (_prefabParentObject && gameObject && gameObject.transform.IsChildOf(_prefabParentObject.transform));
                }
            }

            return _prefabParentObject.transform;
        }

        static NetworkHash128 getNetworkedObjectAssetId(string prefabName)
        {
            Hash128 hasher = Hash128.Compute(prefabName);
            hasher.Append(Main.PluginGUID);
            
            return new NetworkHash128
            {
                i0_7 = hasher.u64_0,
                i8_15 = hasher.u64_1
            };
        }

        static GameObject createPrefab(string name, Type[] componentTypes, bool isNetworked)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException($"'{nameof(name)}' cannot be null or whitespace.", nameof(name));

            if (componentTypes is null)
                throw new ArgumentNullException(nameof(componentTypes));
            
            if (componentTypes.Length > 0)
            {
                componentTypes = RequiredComponentsAttribute.ResolveRequiredComponentTypes(componentTypes);
            }

            NetworkHash128 assetId = default;
            if (isNetworked)
            {
                if (Array.FindIndex(componentTypes, t => t == typeof(NetworkIdentity)) == -1)
                {
                    ArrayUtils.ArrayInsert(ref componentTypes, 0, typeof(NetworkIdentity));
                }

                assetId = getNetworkedObjectAssetId(name);
            }

            GameObject prefab = new GameObject(name);
            prefab.transform.SetParent(getPrefabParent());

            foreach (Type componentType in componentTypes)
            {
                prefab.EnsureComponent(componentType);
            }

            NetworkIdentity networkIdentity = prefab.GetComponent<NetworkIdentity>();
            if (isNetworked)
            {
                if (!networkIdentity)
                {
                    Log.Error($"Prefab {name} is networked, but missing NetworkIdentity");
                    networkIdentity = prefab.AddComponent<NetworkIdentity>();
                }

                networkIdentity.m_AssetId = assetId;

                prefab.EnsureComponent<EnsureNetworkDestroy>();
            }
            else
            {
                if (networkIdentity)
                {
                    Log.Error($"Non-networked prefab '{name}' has NetworkIdentity component");
                }
            }

            return prefab;
        }

        static GameObject instantiatePrefab(GameObject original, string name, bool isNetworked)
        {
            GameObject prefab = GameObject.Instantiate(original, getPrefabParent());
            prefab.name = name;

            if (isNetworked)
            {
                NetworkIdentity networkIdentity = prefab.EnsureComponent<NetworkIdentity>();
                networkIdentity.m_AssetId = getNetworkedObjectAssetId(name);

                prefab.EnsureComponent<EnsureNetworkDestroy>();
            }

            return prefab;
        }

        public static GameObject CreateNetworkedPrefab(string name, Type[] componentTypes)
        {
            return createPrefab(name, componentTypes, true);
        }

        public static GameObject CreateNetworkedValueModificationProviderPrefab(Type providerComponentType, string name, bool canInterpolate, Type[] additionalComponents = null)
        {
            List<Type> componentTypes = [
                typeof(SetDontDestroyOnLoad),
                typeof(DestroyOnRunEnd),
                providerComponentType
            ];

            if (canInterpolate)
            {
                componentTypes.Add(typeof(NetworkedInterpolationComponent));
            }

            if (additionalComponents != null)
            {
                componentTypes.AddRange(additionalComponents);
            }

            return CreateNetworkedPrefab(name, [.. componentTypes]);
        }

        public static GameObject InstantiateNetworkedPrefab(this GameObject original, string name)
        {
            return instantiatePrefab(original, name, true);
        }

        public static GameObject CreatePrefab(string name, Type[] componentTypes)
        {
            return createPrefab(name, componentTypes, false);
        }

        public static GameObject CreateLocalValueModificationProviderPrefab(Type providerComponentType, string name, bool canInterpolate, Type[] additionalComponents = null)
        {
            List<Type> componentTypes = [
                typeof(SetDontDestroyOnLoad),
                typeof(DestroyOnRunEnd),
                providerComponentType
            ];

            if (canInterpolate)
            {
                componentTypes.Add(typeof(GenericInterpolationComponent));
            }

            if (additionalComponents != null)
            {
                componentTypes.AddRange(additionalComponents);
            }

            return CreatePrefab(name, [.. componentTypes]);
        }

        public static GameObject InstantiatePrefab(this GameObject original, string name)
        {
            return instantiatePrefab(original, name, false);
        }

        [ContentInitializer]
        static IEnumerator InitContent(NetworkedPrefabAssetCollection networkedPrefabs, LocalPrefabAssetCollection localPrefabs)
        {
            List<AsyncOperationHandle> asyncOperations = new List<AsyncOperationHandle>(10);

            // GenericTeamInventory
            {
                GameObject prefab = CreateNetworkedPrefab(nameof(RoCContent.NetworkedPrefabs.GenericTeamInventory), [
                    typeof(SetDontDestroyOnLoad),
                    typeof(TeamFilter),
                    typeof(Inventory),
                    typeof(EnemyInfoPanelInventoryProvider),
                    typeof(DestroyOnRunEnd)
                ]);

                networkedPrefabs.Add(prefab);
            }

            // MonsterItemStealController
            {
                AsyncOperationHandle<GameObject> itemStealControllerLoad = AddressableUtil.LoadAssetAsync<GameObject>(AddressableGuids.RoR2_Base_Brother_ItemStealController_prefab, AsyncReferenceHandleUnloadType.Preload);
                itemStealControllerLoad.OnSuccess(itemStealControllerPrefab =>
                {
                    GameObject prefab = itemStealControllerPrefab.InstantiateNetworkedPrefab(nameof(RoCContent.NetworkedPrefabs.MonsterItemStealController));

                    NetworkedBodyAttachment networkedBodyAttachment = prefab.GetComponent<NetworkedBodyAttachment>();
                    networkedBodyAttachment.shouldParentToAttachedBody = true;
                    networkedBodyAttachment.forceHostAuthority = true;

                    ItemStealController itemStealController = prefab.GetComponent<ItemStealController>();
                    itemStealController.stealInterval = 0.2f;

                    prefab.AddComponent<SyncStolenItemCount>();
                    prefab.AddComponent<ShowStolenItemsPositionIndicator>();

                    networkedPrefabs.Add(prefab);
                });

                asyncOperations.Add(itemStealControllerLoad);
            }

            // ItemStealerPositionIndicator
            {
                AsyncOperationHandle<GameObject> positionIndicatorLoad = AddressableUtil.LoadAssetAsync<GameObject>(AddressableGuids.RoR2_Base_Common_BossPositionIndicator_prefab, AsyncReferenceHandleUnloadType.Preload);
                positionIndicatorLoad.OnSuccess(positionIndicatorPrefab =>
                {
                    GameObject prefab = positionIndicatorPrefab.InstantiatePrefab(nameof(RoCContent.LocalPrefabs.ItemStealerPositionIndicator));

                    PositionIndicator positionIndicator = prefab.GetComponent<PositionIndicator>();

                    if (positionIndicator.insideViewObject)
                    {
                        foreach (SpriteRenderer insideSprite in positionIndicator.insideViewObject.GetComponentsInChildren<SpriteRenderer>())
                        {
                            insideSprite.color = Color.cyan;
                        }
                    }

                    if (positionIndicator.outsideViewObject)
                    {
                        foreach (SpriteRenderer outsideSprite in positionIndicator.outsideViewObject.GetComponentsInChildren<SpriteRenderer>())
                        {
                            outsideSprite.color = Color.cyan;
                        }
                    }

                    localPrefabs.Add(prefab);
                });

                asyncOperations.Add(positionIndicatorLoad);
            }

            // NetworkedSulfurPodBase
            {
                AsyncOperationHandle<GameObject> sulfurPodBaseLoad = AddressableUtil.LoadAssetAsync<GameObject>(AddressableGuids.RoR2_DLC1_sulfurpools_SPSulfurPodBase_prefab, AsyncReferenceHandleUnloadType.Preload);
                sulfurPodBaseLoad.OnSuccess(sulfurPodBasePrefab =>
                {
                    GameObject prefab = sulfurPodBasePrefab.InstantiateNetworkedPrefab(nameof(RoCContent.NetworkedPrefabs.NetworkedSulfurPodBase));

                    networkedPrefabs.Add(prefab);
                });

                asyncOperations.Add(sulfurPodBaseLoad);
            }

            // ConfigNetworker
            {
                GameObject prefab = CreateNetworkedPrefab(nameof(RoCContent.NetworkedPrefabs.ConfigNetworker), [
                    typeof(SetDontDestroyOnLoad),
                    typeof(DestroyOnRunEnd),
                    typeof(SyncConfigValue)
                ]);

                networkedPrefabs.Add(prefab);
            }

            // NewtStatueFixedOrigin
            {
                AsyncOperationHandle<GameObject> newtStatueLoad = AddressableUtil.LoadAssetAsync<GameObject>(AddressableGuids.RoR2_Base_NewtStatue_NewtStatue_prefab, AsyncReferenceHandleUnloadType.Preload);
                newtStatueLoad.OnSuccess(newtStatuePrefab =>
                {
                    GameObject prefab = newtStatuePrefab.InstantiateNetworkedPrefab(nameof(RoCContent.NetworkedPrefabs.NewtStatueFixedOrigin));
                    Transform transform = prefab.transform;

                    for (int i = 0; i < transform.childCount; i++)
                    {
                        transform.GetChild(i).Translate(new Vector3(0f, 1.25f, 0f), Space.World);
                    }

                    networkedPrefabs.Add(prefab);
                });

                asyncOperations.Add(newtStatueLoad);
            }

            // TimedChestFixedOrigin
            {
                AsyncOperationHandle<GameObject> timedChestLoad = AddressableUtil.LoadAssetAsync<GameObject>(AddressableGuids.RoR2_Base_TimedChest_TimedChest_prefab, AsyncReferenceHandleUnloadType.Preload);
                timedChestLoad.OnSuccess(timedChest =>
                {
                    GameObject prefab = timedChest.InstantiateNetworkedPrefab(nameof(RoCContent.NetworkedPrefabs.TimedChestFixedOrigin));

                    if (prefab.TryGetComponent(out ModelLocator modelLocator))
                    {
                        Transform modelTransform = modelLocator.modelTransform;
                        if (modelTransform)
                        {
                            Transform modelRoot = new GameObject("ModelRoot").transform;
                            modelRoot.parent = prefab.transform;
                            modelRoot.localPosition = new Vector3(0f, 0.75f, 0f);
                            modelRoot.localRotation = Quaternion.identity;
                            modelRoot.localScale = Vector3.one;

                            modelTransform.SetParent(modelRoot, true);
                        }
                    }

                    networkedPrefabs.Add(prefab);
                });

                asyncOperations.Add(timedChestLoad);
            }

            // BossCombatSquadNoReward
            {
                AsyncOperationHandle<GameObject> bossCombatSquadLoad = AddressableUtil.LoadAssetAsync<GameObject>(AddressableGuids.RoR2_Base_Core_BossCombatSquad_prefab, AsyncReferenceHandleUnloadType.Preload);
                bossCombatSquadLoad.OnSuccess(bossCombatSquad =>
                {
                    GameObject prefab = bossCombatSquad.InstantiateNetworkedPrefab(nameof(RoCContent.NetworkedPrefabs.BossCombatSquadNoReward));

                    if (prefab.TryGetComponent(out BossGroup bossGroup))
                    {
                        bossGroup.dropPosition = null;
                        bossGroup.dropTable = null;
                    }

                    networkedPrefabs.Add(prefab);
                });

                asyncOperations.Add(bossCombatSquadLoad);
            }

            yield return asyncOperations.WaitForAllLoaded();
        }
    }
}
