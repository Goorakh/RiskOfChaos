using HG;
using RiskOfChaos.ConfigHandling;
using RiskOfChaos.EffectHandling;
using RiskOfChaos.EffectHandling.Controllers;
using RiskOfChaos.EffectHandling.Controllers.ChatVoting;
using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Data;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Methods;
using RiskOfChaos.EffectHandling.Formatting;
using RiskOfChaos.SaveHandling;
using RiskOfChaos.SaveHandling.DataContainers;
using RiskOfChaos.SaveHandling.DataContainers.Effects;
using RiskOfChaos.Utilities;
using RiskOfChaos.Utilities.DropTables;
using RiskOfOptions.OptionConfigs;
using RoR2;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

namespace RiskOfChaos.EffectDefinitions.World
{
    [ChaosTimedEffect(EFFECT_IDENTIFIER, TimedEffectType.UntilStageEnd, AllowDuplicates = false, ConfigName = "All Items Are A Random Item", DefaultSelectionWeight = 0.8f)]
    public sealed class ForceAllItemsIntoRandomItem : TimedEffect
    {
        public const string EFFECT_IDENTIFIER = "force_all_items_into_random_item";

        [InitEffectInfo]
        static readonly TimedEffectInfo _effectInfo;

        sealed class NameFormatter : EffectNameFormatter
        {
            public PickupIndex Pickup { get; private set; }

            public NameFormatter()
            {
            }

            public NameFormatter(PickupIndex pickup)
            {
                Pickup = pickup;
            }

            public override void Serialize(NetworkWriter writer)
            {
                writer.Write(Pickup);
            }

            public override void Deserialize(NetworkReader reader)
            {
                Pickup = reader.ReadPickupIndex();
            }

            public override object[] GetFormatArgs()
            {
                PickupDef pickupDef = PickupCatalog.GetPickupDef(Pickup);
                if (pickupDef != null)
                {
                    return [ Util.GenerateColoredString(Language.GetString(pickupDef.nameToken), pickupDef.baseColor) ];
                }
                else
                {
                    return [ "<color=red>[ERROR: PICKUP NOT ROLLED]</color>" ];
                }
            }

            public override bool Equals(EffectNameFormatter other)
            {
                return other is NameFormatter nameFormatter && Pickup == nameFormatter.Pickup;
            }
        }

        [EffectConfig]
        static readonly ConfigHolder<bool> _allowEliteEquipments =
            ConfigFactory<bool>.CreateConfig("Allow Elite Aspects", true)
                               .Description("If elite aspects can be picked as the forced item")
                               .OptionConfig(new CheckBoxConfig())
                               .OnValueChanged(markDropTableDirty)
                               .Build();

        static void markDropTableDirty()
        {
            _dropTable.MarkDirty();
        }

        [EffectConfig]
        static readonly ConfigurableDropTable _dropTable;

        static ForceAllItemsIntoRandomItem()
        {
            _dropTable = ScriptableObject.CreateInstance<ConfigurableDropTable>();
            _dropTable.name = $"dt{nameof(ForceAllItemsIntoRandomItem)}";
            _dropTable.canDropBeReplaced = false;

            _dropTable.RegisterDrop(DropType.Tier1, 1f);
            _dropTable.RegisterDrop(DropType.Tier2, 0.9f);
            _dropTable.RegisterDrop(DropType.Tier3, 0.7f);
            _dropTable.RegisterDrop(DropType.Boss, 0.7f);
            _dropTable.RegisterDrop(DropType.LunarEquipment, 0.1f);
            _dropTable.RegisterDrop(DropType.LunarItem, 0.6f);
            _dropTable.RegisterDrop(DropType.Equipment, 0.15f);
            _dropTable.RegisterDrop(DropType.VoidTier1, 0.6f);
            _dropTable.RegisterDrop(DropType.VoidTier2, 0.6f);
            _dropTable.RegisterDrop(DropType.VoidTier3, 0.5f);
            _dropTable.RegisterDrop(DropType.VoidBoss, 0.3f);

            _dropTable.CreateItemBlacklistConfig("Item Blacklist", "A comma-separated list of items and equipment that should not be included for the effect. Both internal and English display names are accepted, with spaces and commas removed.");

            _dropTable.AddDrops += (List<ExplicitDrop> drops) =>
            {
                drops.Add(new ExplicitDrop(RoR2Content.Items.CaptainDefenseMatrix.itemIndex, DropType.Tier3, null));
                drops.Add(new ExplicitDrop(RoR2Content.Items.Pearl.itemIndex, DropType.Boss, null));
                drops.Add(new ExplicitDrop(RoR2Content.Items.ShinyPearl.itemIndex, DropType.Boss, null));

                if (_allowEliteEquipments.Value)
                {
                    foreach (EquipmentIndex eliteEquipmentIndex in EliteUtils.RunAvailableEliteEquipments)
                    {
                        EquipmentDef eliteEquipment = EquipmentCatalog.GetEquipmentDef(eliteEquipmentIndex);
                        if (!eliteEquipment)
                            continue;

                        if (eliteEquipment.requiredExpansion && !Run.instance.IsExpansionEnabled(eliteEquipment.requiredExpansion))
                            continue;

                        drops.Add(new ExplicitDrop(eliteEquipmentIndex, DropType.Equipment, null));
                    }
                }
            };
        }

        [SystemInitializer]
        static void InitHooks()
        {
            ChaosEffectActivationSignaler_ChatVote.OnEffectVotingFinishedServer += (in EffectVoteResult result) =>
            {
                if (ChaosEffectTracker.Instance &&
                    ChaosEffectTracker.Instance.IsTimedEffectActive(_effectInfo))
                {
                    return;
                }

                // If the effect was in this vote, but *didn't* win, reroll for next time
                EffectVoteInfo[] voteOptions = result.VoteSelection.GetVoteOptions();
                if (result.WinningOption.EffectInfo != _effectInfo && Array.Exists(voteOptions, v => v.EffectInfo == _effectInfo))
                {
                    rerollCurrentOverridePickup();
                }
            };

            Run.onRunStartGlobal += run =>
            {
                if (NetworkServer.active)
                {
                    _pickNextItemRNG = new Xoroshiro128Plus(run.seed);
                    rerollCurrentOverridePickup();
                }
            };

            Stage.onServerStageBegin += _ =>
            {
                if (Configs.EffectSelection.PerStageEffectListEnabled.Value &&
                    Configs.ChatVoting.VotingMode.Value == Configs.ChatVoting.ChatVotingMode.Disabled)
                {
                    _pickNextItemRNG = new Xoroshiro128Plus(Run.instance.stageRng);
                    rerollCurrentOverridePickup();
                }
            };

            if (SaveManager.UseSaveData)
            {
                SaveManager.CollectSaveData += (ref SaveContainer container) =>
                {
                    container.Effects.ForceAllItemsIntoRandomItem_Data = new ForceAllItemsIntoRandomItem_Data
                    {
                        PickNextItemRNG = new SerializableRng(_pickNextItemRNG),
                        CurrentPickupName = CurrentOverridePickupIndex.isValid ? PickupCatalog.GetPickupDef(CurrentOverridePickupIndex).internalName : string.Empty
                    };
                };

                SaveManager.LoadSaveData += (in SaveContainer container) =>
                {
                    ForceAllItemsIntoRandomItem_Data data = container.Effects?.ForceAllItemsIntoRandomItem_Data;
                    if (data is null)
                        return;

                    _pickNextItemRNG = data.PickNextItemRNG;
                    CurrentOverridePickupIndex = PickupCatalog.FindPickupIndex(data.CurrentPickupName);

                    if (CurrentOverridePickupIndex.isValid)
                    {
#if DEBUG
                        Log.Debug($"Loaded current pickup ({CurrentOverridePickupIndex}) from save data");
#endif
                    }
                    else
                    {
                        Log.Warning($"Unable to load pickup from save data. No pickup found with name \"{data.CurrentPickupName}\". Rerolling.");
                        rerollCurrentOverridePickup();
                    }
                };
            }
        }

        static Xoroshiro128Plus _pickNextItemRNG;

        static PickupIndex _currentOverridePickupIndex = PickupIndex.none;
        public static PickupIndex CurrentOverridePickupIndex
        {
            get
            {
                return _currentOverridePickupIndex;
            }
            private set
            {
                _currentOverridePickupIndex = value;
                _effectInfo?.MarkNameFormatterDirty();
            }
        }

        static void rerollCurrentOverridePickup()
        {
            if (_pickNextItemRNG == null)
            {
                Log.Error("Unable to roll pickup, no RNG instance");
                return;
            }

            _dropTable.RegenerateIfNeeded();

            CurrentOverridePickupIndex = _dropTable.GenerateDrop(_pickNextItemRNG);

#if DEBUG
            Log.Debug($"Rolled {CurrentOverridePickupIndex}");
#endif
        }

        [EffectCanActivate]
        static bool CanActivate()
        {
            return CurrentOverridePickupIndex.isValid;
        }

        [GetEffectNameFormatter]
        static EffectNameFormatter GetNameFormatter()
        {
            return new NameFormatter(CurrentOverridePickupIndex);
        }

        public override void OnStart()
        {
            On.RoR2.PickupDropTable.GenerateDrop += PickupDropTable_GenerateDrop;
            On.RoR2.PickupDropTable.GenerateUniqueDrops += PickupDropTable_GenerateUniqueDrops;

            On.RoR2.ChestBehavior.PickFromList += ChestBehavior_PickFromList;

            AllVoidPotentials.OverrideAllowChoices += AllVoidPotentials_OverrideAllowChoices;

            On.RoR2.PickupPickerController.GetOptionsFromPickupIndex += PickupPickerController_GetOptionsFromPickupIndex;

            rerollAllChests();
        }

        public override void OnEnd()
        {
            On.RoR2.PickupDropTable.GenerateDrop -= PickupDropTable_GenerateDrop;
            On.RoR2.PickupDropTable.GenerateUniqueDrops -= PickupDropTable_GenerateUniqueDrops;

            On.RoR2.ChestBehavior.PickFromList -= ChestBehavior_PickFromList;

            AllVoidPotentials.OverrideAllowChoices -= AllVoidPotentials_OverrideAllowChoices;

            On.RoR2.PickupPickerController.GetOptionsFromPickupIndex -= PickupPickerController_GetOptionsFromPickupIndex;

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
                if (shopTerminalBehavior.CurrentPickupIndex() == PickupIndex.none)
                {
#if DEBUG
                    Log.Debug($"Skipping reroll of {shopTerminalBehavior}, no current pickup");
#endif
                    continue;
                }

                bool originalHasBeenPurchased = shopTerminalBehavior.hasBeenPurchased;

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
            return CurrentOverridePickupIndex;
        }

        static PickupIndex[] PickupDropTable_GenerateUniqueDrops(On.RoR2.PickupDropTable.orig_GenerateUniqueDrops orig, PickupDropTable self, int maxDrops, Xoroshiro128Plus rng)
        {
            PickupIndex[] result = orig(self, maxDrops, rng);
            ArrayUtils.SetAll(result, CurrentOverridePickupIndex);
            return result;
        }

        static void ChestBehavior_PickFromList(On.RoR2.ChestBehavior.orig_PickFromList orig, ChestBehavior self, List<PickupIndex> dropList)
        {
            dropList.Clear();
            dropList.Add(CurrentOverridePickupIndex);

            orig(self, dropList);
        }

        static void AllVoidPotentials_OverrideAllowChoices(PickupIndex originalPickup, ref bool allowChoices)
        {
            if (originalPickup == CurrentOverridePickupIndex)
            {
                allowChoices = false;
            }
        }

        static PickupPickerController.Option[] PickupPickerController_GetOptionsFromPickupIndex(On.RoR2.PickupPickerController.orig_GetOptionsFromPickupIndex orig, PickupIndex pickupIndex)
        {
            PickupPickerController.Option[] options = orig(pickupIndex);

            if (pickupIndex == CurrentOverridePickupIndex)
            {
                ArrayUtils.SetAll(options, new PickupPickerController.Option
                {
                    pickupIndex = CurrentOverridePickupIndex,
                    available = true
                });
            }

            return options;
        }
    }
}
