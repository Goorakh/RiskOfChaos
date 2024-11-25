using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.EffectHandling.EffectComponents;
using RiskOfChaos.Utilities;
using RiskOfChaos.Utilities.Extensions;
using RoR2;
using RoR2.ExpansionManagement;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
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

            ExpansionDef dlc1 = ExpansionUtils.DLC1;
            ExpansionDef[] dlc1Expansions = [dlc1];

            static void loadAndSetPoolEntryDccs(DccsPool.PoolEntry poolEntry, string assetKey)
            {
                AsyncOperationHandle<FamilyDirectorCardCategorySelection> loadAssetHandle = Addressables.LoadAssetAsync<FamilyDirectorCardCategorySelection>(assetKey);
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

            static DccsPool.PoolEntry getPoolEntry(string assetKey, float weight = 1f)
            {
                DccsPool.PoolEntry poolEntry = new DccsPool.PoolEntry
                {
                    weight = weight
                };

                loadAndSetPoolEntryDccs(poolEntry, assetKey);

                return poolEntry;
            }

            static DccsPool.PoolEntry[] getGroupedPoolEntries(string[] assetKeys, float totalWeight = 1f)
            {
                int entryCount = assetKeys.Length;
                float entryWeight = totalWeight / entryCount;

                DccsPool.PoolEntry[] entries = new DccsPool.PoolEntry[entryCount];
                for (int i = 0; i < entryCount; i++)
                {
                    entries[i] = getPoolEntry(assetKeys[i], entryWeight);
                }

                return entries;
            }

            static DccsPool.ConditionalPoolEntry getConditionalPoolEntry(string assetKey, ExpansionDef[] requiredExpansions, float weight = 1f)
            {
                DccsPool.ConditionalPoolEntry poolEntry = new DccsPool.ConditionalPoolEntry
                {
                    weight = weight,
                    requiredExpansions = requiredExpansions
                };

                loadAndSetPoolEntryDccs(poolEntry, assetKey);

                return poolEntry;
            }

            DccsPool.Category category = new DccsPool.Category
            {
                name = "AllFamilies",
                categoryWeight = 1f,
                alwaysIncluded = [
                    .. getGroupedPoolEntries(["RoR2/Base/Common/dccsBeetleFamily.asset", "RoR2/Base/Common/dccsBeetleFamilySulfur.asset"]),
                    .. getGroupedPoolEntries(["RoR2/Base/Common/dccsGolemFamily.asset", "RoR2/Base/Common/dccsGolemFamilyNature.asset", "RoR2/Base/Common/dccsGolemFamilySandy.asset", "RoR2/Base/Common/dccsGolemFamilySnowy.asset"]),
                    getPoolEntry("RoR2/Base/Common/dccsImpFamily.asset"),
                    getPoolEntry("RoR2/Base/Common/dccsJellyfishFamily.asset"),
                    getPoolEntry("RoR2/Base/Common/dccsLemurianFamily.asset"),
                    getPoolEntry("RoR2/Base/Common/dccsLunarFamily.asset"),
                    getPoolEntry("RoR2/Base/Common/dccsMushroomFamily.asset"),
                    getPoolEntry("RoR2/Base/Common/dccsParentFamily.asset"),
                    getPoolEntry("RoR2/Base/Common/dccsWispFamily.asset")
                ],
                includedIfConditionsMet = [
                    getConditionalPoolEntry("RoR2/Base/Common/dccsGupFamily.asset", dlc1Expansions),
                    getConditionalPoolEntry("RoR2/DLC1/Common/dccsAcidLarvaFamily.asset", dlc1Expansions),
                    getConditionalPoolEntry("RoR2/DLC1/Common/dccsConstructFamily.asset", dlc1Expansions),
                    getConditionalPoolEntry("RoR2/DLC1/Common/dccsVoidFamily.asset", dlc1Expansions)
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
