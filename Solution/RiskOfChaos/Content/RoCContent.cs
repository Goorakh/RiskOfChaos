using HarmonyLib;
using RiskOfChaos.Content.AssetCollections;
using RiskOfChaos.ScreenEffect;
using RoR2;
using RoR2.ContentManagement;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.Networking;

namespace RiskOfChaos.Content
{
    public class RoCContent : IContentPackProvider
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
            EntityStateAssetCollection entityStates = getAssetCollection<EntityStateAssetCollection>();
            NetworkedPrefabAssetCollection networkedPrefabs = getAssetCollection<NetworkedPrefabAssetCollection>();
            LocalPrefabAssetCollection localPrefabs = getAssetCollection<LocalPrefabAssetCollection>();
            ScreenEffectDefAssetCollection screenEffects = getAssetCollection<ScreenEffectDefAssetCollection>();

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
                    Log.Error($"Unknown return type for content intializer {contentInitializerMethod.FullDescription()}");
                }

                args.ReportProgress((float)(i + 1) / contentInitializerMethods.Count);

                yield return 0;
            }

            itemDefs.AddTo(_contentPack.itemDefs);
            buffDefs.AddTo(_contentPack.buffDefs);
            effectDefs.AddTo(_contentPack.effectDefs);
            unlockableDefs.AddTo(_contentPack.unlockableDefs);
            bodyPrefabs.AddTo(_contentPack.bodyPrefabs);
            entityStates.AddTo(_contentPack.entityStateTypes);
            networkedPrefabs.AddTo(_contentPack.networkedObjectPrefabs);

            NamedAssetCollection<GameObject> localPrefabAssetCollection = new NamedAssetCollection<GameObject>(ContentPack.getGameObjectName);
            localPrefabs.AddTo(localPrefabAssetCollection);

            NamedAssetCollection<ScreenEffectDef> screenEffectsAssetCollection = new NamedAssetCollection<ScreenEffectDef>(ScreenEffectDef.NameProvider);
            screenEffects.AddTo(screenEffectsAssetCollection);

            populateTypeFields(typeof(Items), _contentPack.itemDefs);

            populateTypeFields(typeof(Buffs), _contentPack.buffDefs, fieldName => "bd" + fieldName);

            populateTypeFields(typeof(Effects), _contentPack.effectDefs);

            populateTypeFields(typeof(Unlockables), _contentPack.unlockableDefs);

            populateTypeFields(typeof(BodyPrefabs), _contentPack.bodyPrefabs);

            populateTypeFields(typeof(NetworkedPrefabs), _contentPack.networkedObjectPrefabs);
            NetworkedPrefabs.CacheNetworkPrefabs(networkedPrefabs);

            populateTypeFields(typeof(LocalPrefabs), localPrefabAssetCollection);

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

        public static class Items
        {
            public static ItemDef InvincibleLemurianMarker;

            public static ItemDef MinAllyRegen;
        }

        public static class Buffs
        {
            public static BuffDef SetTo1Hp;
        }

        public static class Effects
        {
            public static EffectDef EquipmentTransferOrbEffect;
        }

        public static class Unlockables
        {
            [TargetAssetName("Logs.InvincibleLemurian")]
            public static UnlockableDef InvincibleLemurianLog;

            [TargetAssetName("Logs.InvincibleLemurianElder")]
            public static UnlockableDef InvincibleLemurianElderLog;
        }

        public static class BodyPrefabs
        {
            public static GameObject ChaosFakeInteractorBody;
        }

        public static class NetworkedPrefabs
        {
            static readonly Dictionary<NetworkHash128, GameObject> _networkPrefabsByAssetId = [];

            internal static void CacheNetworkPrefabs(ICollection<GameObject> networkPrefabs)
            {
                foreach (GameObject prefab in networkPrefabs)
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

                    if (!_networkPrefabsByAssetId.TryAdd(assetId, prefab))
                    {
                        GameObject existingPrefab = _networkPrefabsByAssetId[assetId];

                        Log.Error($"Duplicate assed ids! '{existingPrefab.name}' and '{prefab.name}' both have the same asset id of {assetId}");
                    }
                }
            }

            public static bool TryGetPrefab(NetworkHash128 assetId, out GameObject prefab)
            {
                return _networkPrefabsByAssetId.TryGetValue(assetId, out prefab);
            }

            public static GameObject ChaosEffectManager;

            public static GameObject GenericTeamInventory;

            public static GameObject MonsterItemStealController;

            public static GameObject NetworkedSulfurPodBase;

            public static GameObject DummyDamageInflictor;

            public static GameObject ConfigNetworker;

            public static GameObject SuperhotController;

            public static GameObject NewtStatueFixedOrigin;

            public static GameObject ExplodeAtLowHealthBodyAttachment;

            public static GameObject ValueModificationManager;

            public static GameObject CameraModificationProvider;

            public static GameObject AttackDelayModificationProvider;
        }

        public static class LocalPrefabs
        {
            public static GameObject ItemStealerPositionIndicator;

            public static GameObject ActiveEffectListUIItem;

            public static GameObject ActiveEffectsUIPanel;

            public static GameObject CreditsPanelNoBackground;

            public static GameObject ChaosEffectUIVoteItem;

            public static GameObject ChaosNextEffectDisplay;
        }
    }
}
