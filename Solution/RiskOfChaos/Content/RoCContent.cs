using RiskOfChaos.ScreenEffect;
using RoR2;
using RoR2.ContentManagement;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Reflection;
using UnityEngine;
using UnityEngine.Networking;

namespace RiskOfChaos.Content
{
    public partial class RoCContent : IContentPackProvider
    {
        readonly ExtendedContentPack _contentPack = new ExtendedContentPack(new ContentPack());

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
            Stopwatch stopwatch = Stopwatch.StartNew();

            _contentPack.identifier = identifier;

            using PartitionedProgress progress = new PartitionedProgress(args.progressReceiver);
            IProgress<float> contentInitializersProgress = progress.AddPartition(1f);
            IProgress<float> prefabInitializersProgress = progress.AddPartition(0.8f);

            yield return ContentInitializerAttribute.RunContentInitializers(_contentPack, contentInitializersProgress);

            yield return PrefabInitializerAttribute.RunPrefabInitializers(_contentPack, prefabInitializersProgress);

            populateTypeFields(typeof(Items), _contentPack.itemDefs);

            populateTypeFields(typeof(Buffs), _contentPack.buffDefs, fieldName => "bd" + fieldName);

            populateTypeFields(typeof(Effects), _contentPack.effectDefs);

            populateTypeFields(typeof(Unlockables), _contentPack.unlockableDefs);

            populateTypeFields(typeof(BodyPrefabs), _contentPack.bodyPrefabs);

            populateTypeFields(typeof(ProjectilePrefabs), _contentPack.projectilePrefabs);

            populateTypeFields(typeof(NetworkedPrefabs), _contentPack.networkedObjectPrefabs);
            NetworkedPrefabs.AllPrefabs = [.. _contentPack.networkedObjectPrefabs];
            NetworkedPrefabs.CacheNetworkPrefabs();

            populateTypeFields(typeof(LocalPrefabs), _contentPack.prefabs);
            LocalPrefabs.AllPrefabs = [.. _contentPack.prefabs];

            ScreenEffectDefs = [.. _contentPack.screenEffectDefs];

            args.progressReceiver.Report(1f);

            stopwatch.Stop();
            Log.Debug($"Loaded content in {stopwatch.Elapsed.TotalSeconds:0.#} second(s)");
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
