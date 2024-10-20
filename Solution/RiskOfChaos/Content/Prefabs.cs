using EntityStates;
using HG;
using RiskOfChaos.Components;
using RiskOfChaos.Content.AssetCollections;
using RiskOfChaos.EffectDefinitions.Character;
using RiskOfChaos.Networking.Components;
using RiskOfChaos.Utilities.Extensions;
using RoR2;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
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

            if (isNetworked)
            {
                NetworkIdentity networkIdentity = prefab.GetComponent<NetworkIdentity>();
                if (!networkIdentity)
                {
                    Log.Error($"Prefab {name} is networked, but missing NetworkIdentity");
                    networkIdentity = prefab.AddComponent<NetworkIdentity>();
                }

                networkIdentity.m_AssetId = assetId;
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
            }

            return prefab;
        }

        public static GameObject CreateNetworkedPrefab(string name, Type[] componentTypes)
        {
            return createPrefab(name, componentTypes, true);
        }

        public static GameObject CreateValueModificationProviderPrefab(Type providerComponentType, string name, Type[] additionalComponents = null)
        {
            List<Type> componentTypes = [
                typeof(SetDontDestroyOnLoad),
                typeof(DestroyOnRunEnd),
                providerComponentType
            ];

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

        public static GameObject InstantiatePrefab(this GameObject original, string name)
        {
            return instantiatePrefab(original, name, false);
        }

        [ContentInitializer]
        static IEnumerator InitContent(NetworkedPrefabAssetCollection networkedPrefabs, LocalPrefabAssetCollection localPrefabs)
        {
            List<AsyncOperationHandle> asyncOperations = [];

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
                AsyncOperationHandle<GameObject> itemStealControllerLoad = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/Brother/ItemStealController.prefab");
                itemStealControllerLoad.Completed += handle =>
                {
                    GameObject prefab = handle.Result.InstantiateNetworkedPrefab(nameof(RoCContent.NetworkedPrefabs.MonsterItemStealController));

                    NetworkedBodyAttachment networkedBodyAttachment = prefab.GetComponent<NetworkedBodyAttachment>();
                    networkedBodyAttachment.shouldParentToAttachedBody = true;
                    networkedBodyAttachment.forceHostAuthority = true;

                    ItemStealController itemStealController = prefab.GetComponent<ItemStealController>();
                    itemStealController.stealInterval = 0.2f;

                    prefab.AddComponent<SyncStolenItemCount>();
                    prefab.AddComponent<ShowStolenItemsPositionIndicator>();

                    networkedPrefabs.Add(prefab);
                };

                asyncOperations.Add(itemStealControllerLoad);
            }

            // ItemStealerPositionIndicator
            {
                AsyncOperationHandle<GameObject> positionIndicatorLoad = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/Common/BossPositionIndicator.prefab");
                positionIndicatorLoad.Completed += handle =>
                {
                    GameObject prefab = handle.Result.InstantiatePrefab(nameof(RoCContent.LocalPrefabs.ItemStealerPositionIndicator));

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
                };

                asyncOperations.Add(positionIndicatorLoad);
            }

            // NetworkedSulfurPodBase
            {
                AsyncOperationHandle<GameObject> sulfurPodBaseLoad = Addressables.LoadAssetAsync<GameObject>("RoR2/DLC1/sulfurpools/SPSulfurPodBase.prefab");
                sulfurPodBaseLoad.Completed += handle =>
                {
                    GameObject prefab = handle.Result.InstantiateNetworkedPrefab(nameof(RoCContent.NetworkedPrefabs.NetworkedSulfurPodBase));

                    networkedPrefabs.Add(prefab);
                };

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

            // SuperhotController
            {
                GameObject prefab = CreateNetworkedPrefab(nameof(RoCContent.NetworkedPrefabs.SuperhotController), [
                    typeof(NetworkedBodyAttachment),
                    typeof(SuperhotPlayerController)
                ]);

                NetworkIdentity networkIdentity = prefab.GetComponent<NetworkIdentity>();
                networkIdentity.localPlayerAuthority = true;

                NetworkedBodyAttachment networkedBodyAttachment = prefab.GetComponent<NetworkedBodyAttachment>();
                networkedBodyAttachment.shouldParentToAttachedBody = true;
                networkedBodyAttachment.forceHostAuthority = false;

                networkedPrefabs.Add(prefab);
            }

            // NewtStatueFixedOrigin
            {
                AsyncOperationHandle<GameObject> newtStatueLoad = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/NewtStatue/NewtStatue.prefab");
                newtStatueLoad.Completed += handle =>
                {
                    GameObject prefab = handle.Result.InstantiateNetworkedPrefab(nameof(RoCContent.NetworkedPrefabs.NewtStatueFixedOrigin));
                    Transform transform = prefab.transform;

                    for (int i = 0; i < transform.childCount; i++)
                    {
                        transform.GetChild(i).Translate(new Vector3(0f, 1.25f, 0f), Space.World);
                    }

                    networkedPrefabs.Add(prefab);
                };

                asyncOperations.Add(newtStatueLoad);
            }

            // ExplodeAtLowHealthBodyAttachment
            {
                AsyncOperationHandle<GameObject> fuelArrayAttachmentLoad = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/QuestVolatileBattery/QuestVolatileBatteryAttachment.prefab");
                fuelArrayAttachmentLoad.Completed += handle =>
                {
                    GameObject prefab = handle.Result.InstantiateNetworkedPrefab(nameof(RoCContent.NetworkedPrefabs.ExplodeAtLowHealthBodyAttachment));

                    EntityStateMachine stateMachine = prefab.GetComponent<EntityStateMachine>();
                    stateMachine.initialStateType = new SerializableEntityStateType(typeof(ExplodeAtLowHealth.MonitorState));
                    stateMachine.mainStateType = new SerializableEntityStateType(typeof(ExplodeAtLowHealth.MonitorState));

                    prefab.EnsureComponent<GenericOwnership>();

                    networkedPrefabs.Add(prefab);
                };

                asyncOperations.Add(fuelArrayAttachmentLoad);
            }

            yield return asyncOperations.WaitForAllLoaded();
        }
    }
}
