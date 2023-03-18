using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Methods;
using RiskOfChaos.Utilities;
using RoR2;
using RoR2.ExpansionManagement;
using System;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace RiskOfChaos.EffectDefinitions.World
{
    [ChaosEffect("random_family_event", DefaultSelectionWeight = 0.4f)]
    public sealed class RandomFamilyEvent : BaseEffect
    {
        static readonly DccsPool _allFamilyEventsPool;

        static RandomFamilyEvent()
        {
            _allFamilyEventsPool = ScriptableObject.CreateInstance<DccsPool>();

            ExpansionDef dlc1 = ExpansionUtils.DLC1;
            ExpansionDef[] dlc1Expansions = new ExpansionDef[] { dlc1 };

            DccsPool.Category category = new DccsPool.Category
            {
                name = "AllFamilies",
                categoryWeight = 1f,
                alwaysIncluded = new DccsPool.PoolEntry[]
                {
                    new DccsPool.PoolEntry
                    {
                        dccs = Addressables.LoadAssetAsync<FamilyDirectorCardCategorySelection>("RoR2/Base/Common/dccsBeetleFamily.asset").WaitForCompletion(),
                        weight = 1f
                    },
                    new DccsPool.PoolEntry
                    {
                        dccs = Addressables.LoadAssetAsync<FamilyDirectorCardCategorySelection>("RoR2/Base/Common/dccsGolemFamily.asset").WaitForCompletion(),
                        weight = 1f
                    },
                    new DccsPool.PoolEntry
                    {
                        dccs = Addressables.LoadAssetAsync<FamilyDirectorCardCategorySelection>("RoR2/Base/Common/dccsImpFamily.asset").WaitForCompletion(),
                        weight = 1f
                    },
                    new DccsPool.PoolEntry
                    {
                        dccs = Addressables.LoadAssetAsync<FamilyDirectorCardCategorySelection>("RoR2/Base/Common/dccsJellyfishFamily.asset").WaitForCompletion(),
                        weight = 1f
                    },
                    new DccsPool.PoolEntry
                    {
                        dccs = Addressables.LoadAssetAsync<FamilyDirectorCardCategorySelection>("RoR2/Base/Common/dccsLemurianFamily.asset").WaitForCompletion(),
                        weight = 1f
                    },
                    new DccsPool.PoolEntry
                    {
                        dccs = Addressables.LoadAssetAsync<FamilyDirectorCardCategorySelection>("RoR2/Base/Common/dccsLunarFamily.asset").WaitForCompletion(),
                        weight = 1f
                    },
                    new DccsPool.PoolEntry
                    {
                        dccs = Addressables.LoadAssetAsync<FamilyDirectorCardCategorySelection>("RoR2/Base/Common/dccsMushroomFamily.asset").WaitForCompletion(),
                        weight = 1f
                    },
                    new DccsPool.PoolEntry
                    {
                        dccs = Addressables.LoadAssetAsync<FamilyDirectorCardCategorySelection>("RoR2/Base/Common/dccsParentFamily.asset").WaitForCompletion(),
                        weight = 1f
                    },
                    new DccsPool.PoolEntry
                    {
                        dccs = Addressables.LoadAssetAsync<FamilyDirectorCardCategorySelection>("RoR2/Base/Common/dccsWispFamily.asset").WaitForCompletion(),
                        weight = 1f
                    }
                },
                includedIfConditionsMet = new DccsPool.ConditionalPoolEntry[]
                {
                    new DccsPool.ConditionalPoolEntry
                    {
                        dccs = Addressables.LoadAssetAsync<FamilyDirectorCardCategorySelection>("RoR2/Base/Common/dccsGupFamily.asset").WaitForCompletion(),
                        weight = 1f,
                        requiredExpansions = dlc1Expansions
                    },
                    new DccsPool.ConditionalPoolEntry
                    {
                        dccs = Addressables.LoadAssetAsync<FamilyDirectorCardCategorySelection>("RoR2/DLC1/Common/dccsAcidLarvaFamily.asset").WaitForCompletion(),
                        weight = 1f,
                        requiredExpansions = dlc1Expansions
                    },
                    new DccsPool.ConditionalPoolEntry
                    {
                        dccs = Addressables.LoadAssetAsync<FamilyDirectorCardCategorySelection>("RoR2/DLC1/Common/dccsConstructFamily.asset").WaitForCompletion(),
                        weight = 1f,
                        requiredExpansions = dlc1Expansions
                    },
                    new DccsPool.ConditionalPoolEntry
                    {
                        dccs = Addressables.LoadAssetAsync<FamilyDirectorCardCategorySelection>("RoR2/DLC1/Common/dccsVoidFamily.asset").WaitForCompletion(),
                        weight = 1f,
                        requiredExpansions = dlc1Expansions
                    }
                },
                includedIfNoConditionsMet = Array.Empty<DccsPool.PoolEntry>()
            };

#pragma warning disable Publicizer001 // Accessing a member that was not originally public
            _allFamilyEventsPool.poolCategories = new DccsPool.Category[] { category };
#pragma warning restore Publicizer001 // Accessing a member that was not originally public
        }

        static bool _appliedPatches = false;
        static bool _forceAllFamilyEventsAvailable = false;

        static void applyPatchesIfNeeded()
        {
            if (_appliedPatches)
                return;

            On.RoR2.FamilyDirectorCardCategorySelection.IsAvailable += static (orig, self) =>
            {
                return orig(self) || _forceAllFamilyEventsAvailable;
            };

            _appliedPatches = true;
        }

        [EffectCanActivate]
        static bool CanActivate()
        {
            ClassicStageInfo stageInfo = ClassicStageInfo.instance;
#pragma warning disable Publicizer001 // Accessing a member that was not originally public
            return stageInfo && stageInfo.modifiableMonsterCategories is not FamilyDirectorCardCategorySelection;
#pragma warning restore Publicizer001 // Accessing a member that was not originally public
        }

        public override void OnStart()
        {
            applyPatchesIfNeeded();

            ClassicStageInfo stageInfo = ClassicStageInfo.instance;

#pragma warning disable Publicizer001 // Accessing a member that was not originally public
            ref DccsPool monsterDccsPool = ref stageInfo.monsterDccsPool;
#pragma warning restore Publicizer001 // Accessing a member that was not originally public

            DccsPool originalDccsPool = monsterDccsPool;

            monsterDccsPool = _allFamilyEventsPool;

            _forceAllFamilyEventsAvailable = true;
#pragma warning disable Publicizer001 // Accessing a member that was not originally public
            stageInfo.RebuildCards();
#pragma warning restore Publicizer001 // Accessing a member that was not originally public
            _forceAllFamilyEventsAvailable = false;

            monsterDccsPool = originalDccsPool;
        }
    }
}
