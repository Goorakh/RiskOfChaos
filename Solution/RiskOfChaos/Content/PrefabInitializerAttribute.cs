using HarmonyLib;
using HG;
using HG.Coroutines;
using HG.Reflection;
using RiskOfChaos.Utilities;
using RiskOfChaos.Utilities.Extensions;
using RoR2;
using RoR2.ExpansionManagement;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace RiskOfChaos.Content
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
    public sealed class PrefabInitializerAttribute : SearchableAttribute
    {
        public new MethodInfo target => base.target as MethodInfo;

        public static IEnumerator RunPrefabInitializers(ExtendedContentPack contentPack, IProgress<float> progressReceiver)
        {
            List<GameObject> allPrefabs = [
                .. contentPack.prefabs,
                .. contentPack.bodyPrefabs,
                .. contentPack.gameModePrefabs,
                .. contentPack.masterPrefabs,
                .. contentPack.networkedObjectPrefabs,
                .. contentPack.projectilePrefabs
            ];

            List<AssetReferenceT<GameObject>> prefabsToLoad = [];

            foreach (ArtifactDef artifactDef in contentPack.artifactDefs)
            {
                if (artifactDef.pickupModelReference != null && artifactDef.pickupModelReference.RuntimeKeyIsValid())
                {
                    prefabsToLoad.Add(artifactDef.pickupModelReference);
                }
#pragma warning disable CS0618 // Type or member is obsolete
                else if (artifactDef.pickupModelPrefab)
                {
                    allPrefabs.Add(artifactDef.pickupModelPrefab);
                }
#pragma warning restore CS0618 // Type or member is obsolete
            }

            foreach (EffectDef effectDef in contentPack.effectDefs)
            {
                if (effectDef.prefab)
                {
                    allPrefabs.Add(effectDef.prefab);
                }
            }

            foreach (EquipmentDef equipmentDef in contentPack.equipmentDefs)
            {
                if (equipmentDef.pickupModelReference != null && equipmentDef.pickupModelReference.RuntimeKeyIsValid())
                {
                    prefabsToLoad.Add(equipmentDef.pickupModelReference);
                }
#pragma warning disable CS0618 // Type or member is obsolete
                else if (equipmentDef.pickupModelPrefab)
                {
                    allPrefabs.Add(equipmentDef.pickupModelPrefab);
                }
#pragma warning restore CS0618 // Type or member is obsolete
            }

            foreach (ExpansionDef expansionDef in contentPack.expansionDefs)
            {
                if (expansionDef.runBehaviorPrefab)
                {
                    allPrefabs.Add(expansionDef.runBehaviorPrefab);
                }
            }

            foreach (GameEndingDef gameEndingDef in contentPack.gameEndingDefs)
            {
                if (gameEndingDef.defaultKillerOverride)
                {
                    allPrefabs.Add(gameEndingDef.defaultKillerOverride);
                }
            }

            foreach (ItemDef itemDef in contentPack.itemDefs)
            {
                if (itemDef.pickupModelReference != null && itemDef.pickupModelReference.RuntimeKeyIsValid())
                {
                    prefabsToLoad.Add(itemDef.pickupModelReference);
                }
#pragma warning disable CS0618 // Type or member is obsolete
                else if (itemDef.pickupModelPrefab)
                {
                    allPrefabs.Add(itemDef.pickupModelPrefab);
                }
#pragma warning restore CS0618 // Type or member is obsolete
            }

            foreach (ItemTierDef itemTierDef in contentPack.itemTierDefs)
            {
                if (itemTierDef.dropletDisplayPrefab)
                {
                    allPrefabs.Add(itemTierDef.dropletDisplayPrefab);
                }

                if (itemTierDef.highlightPrefab)
                {
                    allPrefabs.Add(itemTierDef.highlightPrefab);
                }
            }

            foreach (MiscPickupDef miscPickupDef in contentPack.miscPickupDefs)
            {
                if (miscPickupDef.displayPrefab)
                {
                    allPrefabs.Add(miscPickupDef.displayPrefab);
                }

                if (miscPickupDef.dropletDisplayPrefab)
                {
                    allPrefabs.Add(miscPickupDef.dropletDisplayPrefab);
                }
            }

            foreach (SpawnCard spawnCard in contentPack.spawnCards)
            {
                if (spawnCard.prefab)
                {
                    allPrefabs.Add(spawnCard.prefab);
                }
            }

            foreach (SurvivorDef survivorDef in contentPack.survivorDefs)
            {
                if (survivorDef.bodyPrefab)
                {
                    allPrefabs.Add(survivorDef.bodyPrefab);
                }

                if (survivorDef.displayPrefab)
                {
                    allPrefabs.Add(survivorDef.displayPrefab);
                }
            }

            foreach (UnlockableDef unlockableDef in contentPack.unlockableDefs)
            {
                if (unlockableDef.displayModelPrefab)
                {
                    allPrefabs.Add(unlockableDef.displayModelPrefab);
                }
            }

            Log.Debug($"Executing prefab initializers on {allPrefabs.Count} prefab(s) and {prefabsToLoad.Count} prefab reference(s)");

            List<PrefabInitializerAttribute> attributes = [];
            GetInstances(attributes);

            HashSet<GameObject> initializedPrefabs = new HashSet<GameObject>(allPrefabs.Count + prefabsToLoad.Count);

            ParallelProgressCoroutine initializerCoroutines = new ParallelProgressCoroutine(progressReceiver);

            foreach (GameObject prefab in allPrefabs)
            {
                if (initializedPrefabs.Add(prefab))
                {
                    ReadableProgress<float> prefabInitializerProgress = new ReadableProgress<float>();
                    IEnumerator prefabInitializerCoroutine = startPrefabInitializers(prefab, prefabInitializerProgress, attributes);

                    if (prefabInitializerCoroutine != null)
                    {
                        initializerCoroutines.Add(prefabInitializerCoroutine, prefabInitializerProgress);
                    }
                }
            }

            foreach (AssetReferenceT<GameObject> prefabReference in prefabsToLoad)
            {
                ReadableProgress<float> prefabInitializerProgress = new ReadableProgress<float>();
                IEnumerator prefabInitializerCoroutine = startPrefabInitializers(prefabReference, prefabInitializerProgress, attributes, initializedPrefabs);

                if (prefabInitializerCoroutine != null)
                {
                    initializerCoroutines.Add(prefabInitializerCoroutine, prefabInitializerProgress);
                }
            }

            return initializerCoroutines;
        }

        static IEnumerator startPrefabInitializers(AssetReferenceT<GameObject> prefabReference, IProgress<float> progressReceiver, IEnumerable<PrefabInitializerAttribute> attributes, HashSet<GameObject> initializedPrefabs)
        {
            using PartitionedProgress totalProgress = new PartitionedProgress(progressReceiver);
            IProgress<float> loadPrefabProgressReceiver = totalProgress.AddPartition();
            IProgress<float> initializePrefabProgressReceiver = totalProgress.AddPartition();

            AsyncOperationHandle<GameObject> prefabLoadHandle = AddressableUtil.LoadTempAssetAsync(prefabReference);
            yield return prefabLoadHandle.AsProgressCoroutine(loadPrefabProgressReceiver);

            if (prefabLoadHandle.Status != AsyncOperationStatus.Succeeded || !initializedPrefabs.Add(prefabLoadHandle.Result))
            {
                if (prefabLoadHandle.OperationException != null)
                {
                    Log.Error($"Failed to load prefab {prefabReference.RuntimeKey} for initialization: {prefabLoadHandle.OperationException}");
                }

                progressReceiver.Report(1f);
                yield break;
            }

            yield return startPrefabInitializers(prefabLoadHandle.Result, initializePrefabProgressReceiver, attributes);
        }

        static IEnumerator startPrefabInitializers(GameObject prefab, IProgress<float> progressReceiver, IEnumerable<PrefabInitializerAttribute> attributes)
        {
            ParallelProgressCoroutine parallelCoroutines = new ParallelProgressCoroutine(progressReceiver);

            foreach (MonoBehaviour component in prefab.GetComponentsInChildren<MonoBehaviour>(true))
            {
                Type componentType = component.GetType();

                foreach (PrefabInitializerAttribute attribute in attributes)
                {
                    MethodInfo method = attribute.target;
                    if (!method.DeclaringType.IsAssignableFrom(componentType))
                        continue;

                    ParameterInfo[] methodParameters = method.GetParameters();
                    if (methodParameters.Length != 1 || methodParameters[0].ParameterType != typeof(PrefabInitializerArgs))
                    {
                        Log.Error($"Invalid parameters for Prefab Initializer method {method.FullDescription()}");
                        continue;
                    }

                    ReadableProgress<float> prefabInitializerProgress = new ReadableProgress<float>();
                    PrefabInitializerArgs prefabInitializerArgs = new PrefabInitializerArgs(prefab, prefabInitializerProgress);

                    object returnValue = method.Invoke(null, [prefabInitializerArgs]);

                    IEnumerator enumerator = null;
                    if (returnValue is IEnumerator enumeratorValue)
                    {
                        enumerator = enumeratorValue;
                    }
                    else if (returnValue is IEnumerable enumerableValue)
                    {
                        enumerator = enumerableValue.GetEnumerator();
                    }

                    if (enumerator != null)
                    {
                        parallelCoroutines.Add(enumerator, prefabInitializerProgress);
                    }
                    else if (returnValue != null)
                    {
                        Log.Error($"Unknown return type for prefab intializer {method.FullDescription()}");
                    }
                }
            }

            return parallelCoroutines;
        }
    }
}
