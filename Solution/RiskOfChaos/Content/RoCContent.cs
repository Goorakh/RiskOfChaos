using HarmonyLib;
using RiskOfChaos.Content.AssetCollections;
using RiskOfChaos.ScreenEffect;
using RiskOfChaos.Utilities;
using RoR2;
using RoR2.ContentManagement;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reflection;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace RiskOfChaos.Content
{
    public partial class RoCContent : IContentPackProvider
    {
        readonly ContentPack _contentPack = new ContentPack();

        public string identifier => Main.PluginGUID;

        bool _isRegistered;

        internal ScreenEffectDef[] ScreenEffectDefs { get; private set; } = [];

        internal RoCContent()
        {
        }

        internal void Register()
        {
            if (_isRegistered)
                return;

            ContentManager.collectContentPackProviders += addContentPackProviderDelegate =>
            {
                addContentPackProviderDelegate(this);
            };

            _isRegistered = true;
        }

        public IEnumerator LoadStaticContentAsync(LoadStaticContentAsyncArgs args)
        {
            _contentPack.identifier = identifier;

            Dictionary<Type, object> typeToAssetCollectionInstanceDict = [];

            T getAssetCollection<T>() where T : new()
            {
                T result = new T();
                typeToAssetCollectionInstanceDict.Add(typeof(T), result);
                return result;
            }

            ItemDefAssetCollection itemDefs = getAssetCollection<ItemDefAssetCollection>();
            BuffDefAssetCollection buffDefs = getAssetCollection<BuffDefAssetCollection>();
            EffectDefAssetCollection effectDefs = getAssetCollection<EffectDefAssetCollection>();
            UnlockableDefAssetCollection unlockableDefs = getAssetCollection<UnlockableDefAssetCollection>();
            BodyPrefabAssetCollection bodyPrefabs = getAssetCollection<BodyPrefabAssetCollection>();
            MasterPrefabAssetCollection masterPrefabs = getAssetCollection<MasterPrefabAssetCollection>();
            EntityStateAssetCollection entityStates = getAssetCollection<EntityStateAssetCollection>();
            ProjectilePrefabAssetCollection projectilePrefabs = getAssetCollection<ProjectilePrefabAssetCollection>();
            NetworkedPrefabAssetCollection networkedPrefabs = getAssetCollection<NetworkedPrefabAssetCollection>();
            LocalPrefabAssetCollection localPrefabs = getAssetCollection<LocalPrefabAssetCollection>();
            ScreenEffectDefAssetCollection screenEffects = getAssetCollection<ScreenEffectDefAssetCollection>();
            SkillDefAssetCollection skillDefs = getAssetCollection<SkillDefAssetCollection>();
            SkillFamilyAssetCollection skillFamilies = getAssetCollection<SkillFamilyAssetCollection>();

            List<MethodInfo> contentInitializerMethods = ContentInitializerAttribute.GetContentInitializers();
            for (int i = 0; i < contentInitializerMethods.Count; i++)
            {
                MethodInfo contentInitializerMethod = contentInitializerMethods[i];
                ParameterInfo[] parameterInfos = contentInitializerMethod.GetParameters();
                object[] parameters = new object[parameterInfos.Length];
                for (int j = 0; j < parameters.Length; j++)
                {
                    Type parameterType = parameterInfos[j].ParameterType;

                    if (typeToAssetCollectionInstanceDict.TryGetValue(parameterType, out object parameter))
                    {
                        parameters[j] = parameter;
                    }
                    else
                    {
                        Log.Error($"Unknown parameter type {parameterType.FullName} in {contentInitializerMethod.FullDescription()} ({j})");
                    }
                }

                Log.Debug($"Collecting content from initializer {i + 1}/{contentInitializerMethods.Count} ({contentInitializerMethod.FullDescription()})");

                object returnValue = contentInitializerMethod.Invoke(null, parameters);

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
                    yield return enumerator;
                }
                else if (returnValue is not null)
                {
                    Log.Error($"Unknown return type for content initializer {contentInitializerMethod.FullDescription()}");
                }

                args.ReportProgress(Util.Remap((float)(i + 1) / contentInitializerMethods.Count, 0f, 1f, 0f, 0.75f));

                yield return null;
            }

            List<GameObject> allPrefabs = [];

            foreach (ItemDef itemDef in itemDefs)
            {
                if (itemDef.pickupModelReference != null && itemDef.pickupModelReference.RuntimeKeyIsValid())
                {
                    AsyncOperationHandle<GameObject> pickupModelLoad = AddressableUtil.LoadAssetAsync(itemDef.pickupModelReference, AsyncReferenceHandleUnloadType.Preload);
                    yield return pickupModelLoad;

                    if (pickupModelLoad.Status == AsyncOperationStatus.Succeeded)
                    {
                        allPrefabs.Add(pickupModelLoad.Result);
                    }
                }
#pragma warning disable CS0618 // Type or member is obsolete
                else if (itemDef.pickupModelPrefab)
                {
                    allPrefabs.Add(itemDef.pickupModelPrefab);
                }
#pragma warning restore CS0618 // Type or member is obsolete
            }

            foreach (EffectDef effectDef in effectDefs)
            {
                GameObject effectPrefab = effectDef.prefab;
                if (effectPrefab)
                {
                    allPrefabs.Add(effectPrefab);
                }
            }

            allPrefabs.AddRange(bodyPrefabs);
            allPrefabs.AddRange(masterPrefabs);
            allPrefabs.AddRange(projectilePrefabs);
            allPrefabs.AddRange(networkedPrefabs);
            allPrefabs.AddRange(localPrefabs);

            for (int i = 0; i < allPrefabs.Count; i++)
            {
                yield return PrefabInitializerAttribute.RunPrefabInitializers(allPrefabs[i]);
                args.ReportProgress(Util.Remap((float)(i + 1) / allPrefabs.Count, 0f, 1f, 0.75f, 1f));
            }

            itemDefs.FlushAssets(_contentPack.itemDefs);
            buffDefs.FlushAssets(_contentPack.buffDefs);
            effectDefs.FlushAssets(_contentPack.effectDefs);
            unlockableDefs.FlushAssets(_contentPack.unlockableDefs);
            bodyPrefabs.FlushAssets(_contentPack.bodyPrefabs);
            masterPrefabs.FlushAssets(_contentPack.masterPrefabs);
            entityStates.FlushAssets(_contentPack.entityStateTypes);
            projectilePrefabs.FlushAssets(_contentPack.projectilePrefabs);
            networkedPrefabs.FlushAssets(_contentPack.networkedObjectPrefabs);
            skillDefs.FlushAssets(_contentPack.skillDefs);
            skillFamilies.FlushAssets(_contentPack.skillFamilies);

            NamedAssetCollection<GameObject> localPrefabAssetCollection = new NamedAssetCollection<GameObject>(ContentPack.getGameObjectName);
            localPrefabs.FlushAssets(localPrefabAssetCollection);

            NamedAssetCollection<ScreenEffectDef> screenEffectsAssetCollection = new NamedAssetCollection<ScreenEffectDef>(ScreenEffectDef.NameProvider);
            screenEffects.FlushAssets(screenEffectsAssetCollection);

            populateTypeFields(typeof(Items), _contentPack.itemDefs);

            populateTypeFields(typeof(Buffs), _contentPack.buffDefs, fieldName => "bd" + fieldName);

            populateTypeFields(typeof(Effects), _contentPack.effectDefs);

            populateTypeFields(typeof(Unlockables), _contentPack.unlockableDefs);

            populateTypeFields(typeof(BodyPrefabs), _contentPack.bodyPrefabs);

            populateTypeFields(typeof(ProjectilePrefabs), _contentPack.projectilePrefabs);

            populateTypeFields(typeof(NetworkedPrefabs), _contentPack.networkedObjectPrefabs);
            NetworkedPrefabs.AllPrefabs = [.. _contentPack.networkedObjectPrefabs];
            NetworkedPrefabs.CacheNetworkPrefabs();

            populateTypeFields(typeof(LocalPrefabs), localPrefabAssetCollection);
            LocalPrefabs.AllPrefabs = [.. localPrefabAssetCollection];

            ScreenEffectDefs = [.. screenEffectsAssetCollection];

            args.ReportProgress(1f);
            yield break;
        }

        public IEnumerator GenerateContentPackAsync(GetContentPackAsyncArgs args)
        {
            ContentPack.Copy(_contentPack, args.output);
            args.ReportProgress(1f);
            yield break;
        }

        public IEnumerator FinalizeAsync(FinalizeAsyncArgs args)
        {
            args.ReportProgress(1f);
            yield break;
        }

        static void populateTypeFields<TAsset>(Type typeToPopulate, NamedAssetCollection<TAsset> assets, Func<string, string> fieldNameToAssetNameConverter = null)
        {
            foreach (FieldInfo fieldInfo in typeToPopulate.GetFields(BindingFlags.Static | BindingFlags.Public))
            {
                if (fieldInfo.FieldType == typeof(TAsset))
                {
                    TargetAssetNameAttribute customAttribute = fieldInfo.GetCustomAttribute<TargetAssetNameAttribute>();
                    string assetName;
                    if (customAttribute != null)
                    {
                        assetName = customAttribute.targetAssetName;
                    }
                    else if (fieldNameToAssetNameConverter != null)
                    {
                        assetName = fieldNameToAssetNameConverter(fieldInfo.Name);
                    }
                    else
                    {
                        assetName = fieldInfo.Name;
                    }

                    TAsset tasset = assets.Find(assetName);
                    if (tasset != null)
                    {
                        fieldInfo.SetValue(null, tasset);
                    }
                    else
                    {
                        Log.Warning($"Failed to assign {fieldInfo.DeclaringType.Name}.{fieldInfo.Name}: Asset \"{assetName}\" not found");
                    }
                }
            }
        }

        public static partial class Items
        {
            public static ItemDef InvincibleLemurianMarker;

            public static ItemDef MinAllyRegen;

            public static ItemDef PulseAway;
        }

        public static partial class Buffs
        {
            public static BuffDef SetTo1Hp;
        }

        public static partial class Effects
        {
            public static EffectDef EquipmentTransferOrbEffect;
        }

        public static partial class Unlockables
        {
            [TargetAssetName("Logs.InvincibleLemurian")]
            public static UnlockableDef InvincibleLemurianLog;

            [TargetAssetName("Logs.InvincibleLemurianElder")]
            public static UnlockableDef InvincibleLemurianElderLog;
        }

        public static partial class BodyPrefabs
        {
            public static GameObject ChaosFakeInteractorBody;
        }

        public static partial class ProjectilePrefabs
        {
            public static GameObject GrenadeReplacedProjectile;

            public static GameObject PulseGolemHookProjectile;
        }

        public static class NetworkedPrefabs
        {
            internal static GameObject[] AllPrefabs;

            static readonly Dictionary<NetworkHash128, GameObject> _prefabsByAssetId = [];
            public static readonly ReadOnlyDictionary<NetworkHash128, GameObject> PrefabsByAssetId = new ReadOnlyDictionary<NetworkHash128, GameObject>(_prefabsByAssetId);

            internal static void CacheNetworkPrefabs()
            {
                _prefabsByAssetId.Clear();
                _prefabsByAssetId.EnsureCapacity(AllPrefabs.Length);

                foreach (GameObject prefab in AllPrefabs)
                {
                    if (!prefab.TryGetComponent(out NetworkIdentity networkIdentity))
                    {
                        Log.Error($"Networked prefab '{prefab.name}' is missing NetworkIdentity component");
                        continue;
                    }

                    NetworkHash128 assetId = networkIdentity.assetId;
                    if (!assetId.IsValid())
                    {
                        Log.Error($"Invalid asset id for networked prefab '{prefab.name}'");
                        continue;
                    }

                    if (!_prefabsByAssetId.TryAdd(assetId, prefab))
                    {
                        GameObject existingPrefab = _prefabsByAssetId[assetId];

                        Log.Error($"Duplicate asset ids! '{existingPrefab.name}' and '{prefab.name}' both have the same asset id of {assetId}");
                    }
                }

                _prefabsByAssetId.TrimExcess();
            }

            public static GameObject ChaosEffectManager;

            public static GameObject GenericTeamInventory;

            public static GameObject MonsterItemStealController;

            public static GameObject NetworkedSulfurPodBase;

            public static GameObject DummyDamageInflictor;

            public static GameObject ConfigNetworker;

            public static GameObject SuperhotController;

            public static GameObject NewtStatueFixedOrigin;

            public static GameObject ExplodeAtLowHealthController;

            public static GameObject InterpolatedScreenEffect;

            public static GameObject CameraModificationProvider;

            public static GameObject AttackDelayModificationProvider;

            public static GameObject CostModificationProvider;

            public static GameObject CostConversionProvider;

            public static GameObject EffectModificationProvider;

            public static GameObject GravityModificationProvider;

            public static GameObject SimpleHoldoutZoneModificationProvider;

            public static GameObject KnockbackModificationProvider;

            public static GameObject PickupModificationProvider;

            public static GameObject ProjectileModificationProvider;

            public static GameObject SkillSlotModificationProvider;

            public static GameObject GenericTimeScaleModificationProvider;

            public static GameObject DirectorModificationProvider;

            public static GameObject BossCombatSquadNoReward;

            public static GameObject TimedChestFixedOrigin;
        }

        public static class LocalPrefabs
        {
            internal static GameObject[] AllPrefabs;

            public static GameObject ItemStealerPositionIndicator;

            public static GameObject ActiveEffectListUIItem;

            public static GameObject ActiveEffectsUIPanel;

            public static GameObject CreditsPanelNoBackground;

            public static GameObject ChaosEffectUIVoteItem;

            public static GameObject ChaosNextEffectDisplay;

            public static GameObject ScreenEffectManager;

            public static GameObject ValueModificationManager;

            public static GameObject UIModificationProvider;
        }
    }
}
