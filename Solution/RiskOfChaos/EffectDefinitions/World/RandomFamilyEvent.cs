using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.EffectHandling.EffectComponents;
using RiskOfChaos.Utilities;
using RiskOfChaos.Utilities.Extensions;
using RoR2;
using RoR2.ContentManagement;
using RoR2.ExpansionManagement;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace RiskOfChaos.EffectDefinitions.World
{
    [ChaosEffect("random_family_event", DefaultSelectionWeight = 0.7f)]
    public sealed class RandomFamilyEvent : NetworkBehaviour
    {
        static readonly HashSet<FamilyDirectorCardCategorySelection> _forcedFamilyCardSelections = [];

        static DccsPool _allFamilyEventsPool;

        [SystemInitializer(typeof(ExpansionUtils))]
        static void Init()
        {
            _allFamilyEventsPool = ScriptableObject.CreateInstance<DccsPool>();
            _allFamilyEventsPool.name = "dpAllMonsterFamilies";

            ExpansionDef dlc1 = ExpansionUtils.GetExpansionDef(ExpansionUtils.DLC1);
            ExpansionDef[] dlc1Expansions = [dlc1];

            static void loadAndSetPoolEntryDccs(DccsPool.PoolEntry poolEntry, string assetGuid)
            {
                AsyncOperationHandle<FamilyDirectorCardCategorySelection> loadAssetHandle = AddressableUtil.LoadAssetAsync<FamilyDirectorCardCategorySelection>(assetGuid, AsyncReferenceHandleUnloadType.Preload);
                loadAssetHandle.OnSuccess(familyDccs =>
                {
                    string name = familyDccs.name;
                    familyDccs = Instantiate(familyDccs);
                    familyDccs.name = name + "Forced";

                    familyDccs.minimumStageCompletion = 0;
                    familyDccs.maximumStageCompletion = int.MaxValue;

                    poolEntry.dccs = familyDccs;

                    _forcedFamilyCardSelections.Add(familyDccs);
                });
            }

            static DccsPool.PoolEntry getPoolEntry(string assetGuid, float weight = 1f)
            {
                DccsPool.PoolEntry poolEntry = new DccsPool.PoolEntry
                {
                    weight = weight
                };

                loadAndSetPoolEntryDccs(poolEntry, assetGuid);

                return poolEntry;
            }

            static DccsPool.PoolEntry[] getGroupedPoolEntries(string[] assetGuids, float totalWeight = 1f)
            {
                int entryCount = assetGuids.Length;
                float entryWeight = totalWeight / entryCount;

                DccsPool.PoolEntry[] entries = new DccsPool.PoolEntry[entryCount];
                for (int i = 0; i < entryCount; i++)
                {
                    entries[i] = getPoolEntry(assetGuids[i], entryWeight);
                }

                return entries;
            }

            static DccsPool.ConditionalPoolEntry getConditionalPoolEntry(string assetGuid, ExpansionDef[] requiredExpansions, float weight = 1f)
            {
                DccsPool.ConditionalPoolEntry poolEntry = new DccsPool.ConditionalPoolEntry
                {
                    weight = weight,
                    requiredExpansions = requiredExpansions
                };

                loadAndSetPoolEntryDccs(poolEntry, assetGuid);

                return poolEntry;
            }

            DccsPool.Category category = new DccsPool.Category
            {
                name = "AllFamilies",
                categoryWeight = 1f,
                alwaysIncluded = [
                    .. getGroupedPoolEntries([
                        AddressableGuids.RoR2_Base_Common_dccsBeetleFamily_asset,
                        AddressableGuids.RoR2_Base_Common_dccsBeetleFamilySulfur_asset
                    ]),
                    .. getGroupedPoolEntries([
                        AddressableGuids.RoR2_Base_Common_dccsGolemFamily_asset,
                        AddressableGuids.RoR2_Base_Common_dccsGolemFamilyNature_asset,
                        AddressableGuids.RoR2_Base_Common_dccsGolemFamilySandy_asset,
                        AddressableGuids.RoR2_Base_Common_dccsGolemFamilySnowy_asset
                    ]),
                    getPoolEntry(AddressableGuids.RoR2_Base_Common_dccsImpFamily_asset),
                    getPoolEntry(AddressableGuids.RoR2_Base_Common_dccsJellyfishFamily_asset),
                    getPoolEntry(AddressableGuids.RoR2_Base_Common_dccsLemurianFamily_asset),
                    getPoolEntry(AddressableGuids.RoR2_Base_Common_dccsLunarFamily_asset),
                    getPoolEntry(AddressableGuids.RoR2_Base_Common_dccsMushroomFamily_asset),
                    getPoolEntry(AddressableGuids.RoR2_Base_Common_dccsParentFamily_asset),
                    getPoolEntry(AddressableGuids.RoR2_Base_Common_dccsWispFamily_asset)
                ],
                includedIfConditionsMet = [
                    getConditionalPoolEntry(AddressableGuids.RoR2_Base_Common_dccsGupFamily_asset, dlc1Expansions),
                    getConditionalPoolEntry(AddressableGuids.RoR2_DLC1_Common_dccsAcidLarvaFamily_asset, dlc1Expansions),
                    getConditionalPoolEntry(AddressableGuids.RoR2_DLC1_Common_dccsConstructFamily_asset, dlc1Expansions),
                    getConditionalPoolEntry(AddressableGuids.RoR2_DLC1_Common_dccsVoidFamily_asset, dlc1Expansions)
                ],
                includedIfNoConditionsMet = []
            };

            _allFamilyEventsPool.poolCategories = [category];

            if (_forcedFamilyCardSelections.Count > 0)
            {
                On.RoR2.FamilyDirectorCardCategorySelection.IsAvailable += overrideIsSelectionAvailable;
                static bool overrideIsSelectionAvailable(On.RoR2.FamilyDirectorCardCategorySelection.orig_IsAvailable orig, FamilyDirectorCardCategorySelection self)
                {
                    return orig(self) || _forcedFamilyCardSelections.Contains(self);
                }
            }
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
            
            ClassicStageInfo stageInfo = ClassicStageInfo.instance;
            if (stageInfo)
            {
                stageInfo.seedServer = _rng.nextUlong;
                stageInfo.monsterDccsPool = _allFamilyEventsPool;
                stageInfo.RebuildCards();
            }
        }
    }
}
