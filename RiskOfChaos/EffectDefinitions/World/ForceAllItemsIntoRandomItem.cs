﻿using HG;
using RiskOfChaos.ConfigHandling;
using RiskOfChaos.EffectHandling;
using RiskOfChaos.EffectHandling.Controllers;
using RiskOfChaos.EffectHandling.Controllers.ChatVoting;
using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Data;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Methods;
using RiskOfChaos.Utilities;
using RiskOfChaos.Utilities.Comparers;
using RiskOfChaos.Utilities.Extensions;
using RiskOfChaos.Utilities.ParsedValueHolders.ParsedList;
using RiskOfOptions.OptionConfigs;
using RoR2;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

namespace RiskOfChaos.EffectDefinitions.World
{
    [ChaosTimedEffect("force_all_items_into_random_item", TimedEffectType.UntilStageEnd, AllowDuplicates = false, ConfigName = "All Items Are A Random Item", DefaultSelectionWeight = 0.8f)]
    public sealed class ForceAllItemsIntoRandomItem : TimedEffect
    {
        [InitEffectInfo]
        static readonly TimedEffectInfo _effectInfo;

        static bool _dropTableDirty = false;
        static BasicPickupDropTable _dropTable;

        static void onDropTableConfigChanged()
        {
            _dropTableDirty = true;
        }

        [EffectConfig]
        static readonly ConfigHolder<bool> _allowEliteEquipments =
            ConfigFactory<bool>.CreateConfig("Allow Elite Aspects", true)
                               .Description("If elite aspects can be picked as the forced item")
                               .OptionConfig(new CheckBoxConfig())
                               .OnValueChanged(onDropTableConfigChanged)
                               .Build();

        [EffectConfig]
        static readonly ConfigHolder<string> _itemBlacklistConfig =
            ConfigFactory<string>.CreateConfig("Item Blacklist", string.Empty)
                                 .Description("A comma-separated list of items and equipment that should not be included for the effect. Both internal and English display names are accepted, with spaces and commas removed.")
                                 .OptionConfig(new InputFieldConfig
                                 {
                                     submitOn = InputFieldConfig.SubmitEnum.OnSubmit
                                 })
                                 .OnValueChanged(onDropTableConfigChanged)
                                 .Build();

        static readonly ParsedPickupList _itemBlacklist = new ParsedPickupList(PickupIndexComparer.Instance)
        {
            ConfigHolder = _itemBlacklistConfig
        };

        static ConfigHolder<float> createItemTierWeightConfig(string name, float defaultWeight)
        {
            return ConfigFactory<float>.CreateConfig($"Weight: {name}", defaultWeight)
                                       .Description($"Controls how likely {name} are to be given\n\nA value of 0 means items from this tier will never be given")
                                       .OptionConfig(new StepSliderConfig
                                       {
                                           formatString = "{0:F2}",
                                           min = 0f,
                                           max = 1f,
                                           increment = 0.05f
                                       })
                                       .ValueConstrictor(CommonValueConstrictors.GreaterThanOrEqualTo(0f))
                                       .OnValueChanged(onDropTableConfigChanged)
                                       .Build();
        }

        [EffectConfig] static readonly ConfigHolder<float> _tier1Weight = createItemTierWeightConfig("Common Items", 0.9f);
        [EffectConfig] static readonly ConfigHolder<float> _tier2Weight = createItemTierWeightConfig("Uncommon Items", 1f);
        [EffectConfig] static readonly ConfigHolder<float> _tier3Weight = createItemTierWeightConfig("Legendary Items", 0.7f);
        [EffectConfig] static readonly ConfigHolder<float> _bossWeight = createItemTierWeightConfig("Boss Items", 0.7f);
        [EffectConfig] static readonly ConfigHolder<float> _lunarEquipmentWeight = createItemTierWeightConfig("Lunar Equipments", 0.2f);
        [EffectConfig] static readonly ConfigHolder<float> _lunarItemWeight = createItemTierWeightConfig("Lunar Items", 0.6f);
        [EffectConfig] static readonly ConfigHolder<float> _equipmentWeight = createItemTierWeightConfig("Equipments", 0.3f);
        [EffectConfig] static readonly ConfigHolder<float> _voidTier1Weight = createItemTierWeightConfig("Common Void Items", 0.6f);
        [EffectConfig] static readonly ConfigHolder<float> _voidTier2Weight = createItemTierWeightConfig("Uncommon Void Items", 0.6f);
        [EffectConfig] static readonly ConfigHolder<float> _voidTier3Weight = createItemTierWeightConfig("Legendary Void Items", 0.5f);
        [EffectConfig] static readonly ConfigHolder<float> _voidBossWeight = createItemTierWeightConfig("Void Boss Items", 0.3f);

        static void regenerateDropTable()
        {
#if DEBUG
            Log.Debug("regenerating drop table...");
#endif

            if (!_dropTable)
            {
                _dropTable = ScriptableObject.CreateInstance<BasicPickupDropTable>();
                _dropTable.name = $"dt{nameof(ForceAllItemsIntoRandomItem)}";
            }

            _dropTable.tier1Weight = _tier1Weight.Value;
            _dropTable.tier2Weight = _tier2Weight.Value;
            _dropTable.tier3Weight = _tier3Weight.Value;
            _dropTable.bossWeight = _bossWeight.Value;
            _dropTable.lunarEquipmentWeight = _lunarEquipmentWeight.Value;
            _dropTable.lunarItemWeight = _lunarItemWeight.Value;
            _dropTable.lunarCombinedWeight = 0f;
            _dropTable.equipmentWeight = _equipmentWeight.Value;
            _dropTable.voidTier1Weight = _voidTier1Weight.Value;
            _dropTable.voidTier2Weight = _voidTier2Weight.Value;
            _dropTable.voidTier3Weight = _voidTier3Weight.Value;
            _dropTable.voidBossWeight = _voidBossWeight.Value;

            // If this is done mid-run, Regenerate has to be called, since it's only done by the game on run start
            Run run = Run.instance;
            if (run)
            {
#pragma warning disable Publicizer001 // Accessing a member that was not originally public
                _dropTable.Regenerate(run);
#pragma warning restore Publicizer001 // Accessing a member that was not originally public
            }

            _dropTableDirty = false;
        }

        [SystemInitializer]
        static void InitHooks()
        {
            On.RoR2.BasicPickupDropTable.GenerateWeightedSelection += (orig, self, run) =>
            {
                orig(self, run);

                if (!_dropTable || self != _dropTable)
                    return;

#pragma warning disable Publicizer001 // Accessing a member that was not originally public
                WeightedSelection<PickupIndex> selector = self.selector;
#pragma warning restore Publicizer001 // Accessing a member that was not originally public

                for (int i = selector.Count - 1; i >= 0; i--)
                {
                    PickupIndex pickupIndex = selector.GetChoice(i).value;
                    if (_itemBlacklist.Contains(pickupIndex))
                    {
#if DEBUG
                        Log.Debug($"Removing {pickupIndex} from droptable: Blacklist");
#endif
                        selector.RemoveChoice(i);
                    }
                }

                void tryAddPickup(PickupIndex pickup, float weight)
                {
                    if (!_itemBlacklist.Contains(pickup))
                    {
                        self.AddPickupIfMissing(pickup, weight);
                    }
                    else
                    {
#if DEBUG
                        Log.Debug($"Not adding {pickup} to droptable: Blacklist");
#endif
                    }
                }

                tryAddPickup(PickupCatalog.FindPickupIndex(RoR2Content.Items.CaptainDefenseMatrix.itemIndex), _tier3Weight.Value);
                tryAddPickup(PickupCatalog.FindPickupIndex(RoR2Content.Items.Pearl.itemIndex), _bossWeight.Value);
                tryAddPickup(PickupCatalog.FindPickupIndex(RoR2Content.Items.ShinyPearl.itemIndex), _bossWeight.Value);

                if (_allowEliteEquipments.Value)
                {
                    foreach (EquipmentIndex eliteEquipmentIndex in EliteUtils.RunAvailableEliteEquipments)
                    {
                        EquipmentDef eliteEquipment = EquipmentCatalog.GetEquipmentDef(eliteEquipmentIndex);
                        if (!eliteEquipment)
                            continue;

                        if (eliteEquipment.requiredExpansion && !run.IsExpansionEnabled(eliteEquipment.requiredExpansion))
                            continue;

                        tryAddPickup(PickupCatalog.FindPickupIndex(eliteEquipmentIndex), _equipmentWeight.Value);
                    }
                }

                if (run.IsExpansionEnabled(ExpansionUtils.DLC1))
                {
                }
            };

            Stage.onServerStageBegin += _ =>
            {
                if (ChaosEffectActivationSignaler_ChatVote.Instance &&
                    ChaosEffectActivationSignaler_ChatVote.Instance.enabled &&
                    ChaosEffectActivationSignaler_ChatVote.Instance.CurrentVoteContains(_effectInfo))
                {
                    return;
                }

                if (TimedChaosEffectHandler.Instance &&
                    TimedChaosEffectHandler.Instance.IsTimedEffectActive(_effectInfo))
                {
                    return;
                }

                rerollCurrentOverridePickup();
            };

            Run.onRunStartGlobal += _ =>
            {
                if (NetworkServer.active)
                {
                    rerollCurrentOverridePickup();
                }
            };
        }

        static PickupIndex _currentOverridePickupIndex;
        static void rerollCurrentOverridePickup()
        {
            if (!_dropTable || _dropTableDirty)
            {
                regenerateDropTable();
            }

            _currentOverridePickupIndex = _dropTable.GenerateDrop(RoR2Application.rng);

#if DEBUG
            Log.Debug($"Rolled {_currentOverridePickupIndex}");
#endif
        }

        [EffectCanActivate]
        static bool CanActivate()
        {
            return _currentOverridePickupIndex.isValid;
        }

        [EffectNameFormatArgs]
        static object[] GetEffectNameFormatArgs()
        {
            PickupDef pickupDef = PickupCatalog.GetPickupDef(_currentOverridePickupIndex);
            if (pickupDef != null)
            {
                return new object[] { Util.GenerateColoredString(Language.GetString(pickupDef.nameToken), pickupDef.baseColor) };
            }
            else
            {
                return new object[] { "<color=red>[ERROR: PICKUP NOT ROLLED]</color>" };
            }
        }

        public override void OnStart()
        {
            On.RoR2.PickupDropTable.GenerateDrop += PickupDropTable_GenerateDrop;
            On.RoR2.PickupDropTable.GenerateUniqueDrops += PickupDropTable_GenerateUniqueDrops;

            On.RoR2.ChestBehavior.PickFromList += ChestBehavior_PickFromList;

            AllVoidPotentials.OverrideAllowChoices += AllVoidPotentials_OverrideAllowChoices;

            rerollAllChests();
        }

        public override void OnEnd()
        {
            On.RoR2.PickupDropTable.GenerateDrop -= PickupDropTable_GenerateDrop;
            On.RoR2.PickupDropTable.GenerateUniqueDrops -= PickupDropTable_GenerateUniqueDrops;

            On.RoR2.ChestBehavior.PickFromList -= ChestBehavior_PickFromList;

            AllVoidPotentials.OverrideAllowChoices -= AllVoidPotentials_OverrideAllowChoices;

            rerollAllChests();

            rerollCurrentOverridePickup();
        }

        static void rerollAllChests()
        {
            foreach (ChestBehavior chestBehavior in GameObject.FindObjectsOfType<ChestBehavior>())
            {
                chestBehavior.Roll();
            }

            foreach (OptionChestBehavior optionChestBehavior in GameObject.FindObjectsOfType<OptionChestBehavior>())
            {
                optionChestBehavior.Roll();
            }

            foreach (ShopTerminalBehavior shopTerminalBehavior in GameObject.FindObjectsOfType<ShopTerminalBehavior>())
            {
#pragma warning disable Publicizer001 // Accessing a member that was not originally public
                bool originalHasBeenPurchased = shopTerminalBehavior.hasBeenPurchased;
#pragma warning restore Publicizer001 // Accessing a member that was not originally public

                shopTerminalBehavior.SetHasBeenPurchased(false);
                shopTerminalBehavior.GenerateNewPickupServer();

                shopTerminalBehavior.SetHasBeenPurchased(originalHasBeenPurchased);
            }

            foreach (VoidSuppressorBehavior voidSuppressorBehavior in GameObject.FindObjectsOfType<VoidSuppressorBehavior>())
            {
                voidSuppressorBehavior.RefreshItems();
            }
        }

        static PickupIndex PickupDropTable_GenerateDrop(On.RoR2.PickupDropTable.orig_GenerateDrop orig, PickupDropTable self, Xoroshiro128Plus rng)
        {
            orig(self, rng);
            return _currentOverridePickupIndex;
        }

        static PickupIndex[] PickupDropTable_GenerateUniqueDrops(On.RoR2.PickupDropTable.orig_GenerateUniqueDrops orig, PickupDropTable self, int maxDrops, Xoroshiro128Plus rng)
        {
            PickupIndex[] result = orig(self, maxDrops, rng);
            ArrayUtils.SetAll(result, _currentOverridePickupIndex);
            return result;
        }
        
        static void ChestBehavior_PickFromList(On.RoR2.ChestBehavior.orig_PickFromList orig, ChestBehavior self, List<PickupIndex> dropList)
        {
            dropList.Clear();
            dropList.Add(_currentOverridePickupIndex);

            orig(self, dropList);
        }

        static void AllVoidPotentials_OverrideAllowChoices(PickupIndex originalPickup, ref bool allowChoices)
        {
            if (originalPickup == _currentOverridePickupIndex)
            {
                allowChoices = false;
            }
        }
    }
}