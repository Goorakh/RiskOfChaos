using HG;
using RiskOfChaos.ConfigHandling;
using RiskOfChaos.EffectHandling;
using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Data;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Methods;
using RiskOfChaos.EffectHandling.EffectComponents;
using RiskOfChaos.EffectHandling.Formatting;
using RiskOfChaos.EffectUtils.World;
using RiskOfChaos.SaveHandling;
using RiskOfChaos.Trackers;
using RiskOfChaos.Utilities;
using RiskOfChaos.Utilities.DropTables;
using RiskOfOptions.OptionConfigs;
using RoR2;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.Networking;

namespace RiskOfChaos.EffectDefinitions.World
{
    [ChaosTimedEffect("force_all_items_into_random_item", TimedEffectType.UntilStageEnd, AllowDuplicates = false, ConfigName = "All Items Are A Random Item", DefaultSelectionWeight = 0.8f)]
    public sealed class ForceAllItemsIntoRandomItem : NetworkBehaviour
    {
        [InitEffectInfo]
        public static readonly TimedEffectInfo EffectInfo;

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
                        drops.Add(new ExplicitDrop(eliteEquipmentIndex, DropType.Equipment, null));
                    }
                }
            };
        }

        [SystemInitializer]
        static void Init()
        {
            ForceAllItemsIntoRandomItemManager.OnNextOverridePickupChanged += onNextOverridePickupChanged;
        }

        static void onNextOverridePickupChanged()
        {
            EffectInfo?.MarkNameFormatterDirty();
        }

        public static PickupIndex GenerateOverridePickup(Xoroshiro128Plus rng)
        {
            _dropTable.RegenerateIfNeeded();
            return _dropTable.GenerateDrop(rng);
        }

        [EffectCanActivate]
        static bool CanActivate()
        {
            return ForceAllItemsIntoRandomItemManager.Instance && ForceAllItemsIntoRandomItemManager.Instance.NextOverridePickupIndex.isValid;
        }

        [GetEffectNameFormatter]
        static EffectNameFormatter GetNameFormatter()
        {
            return new NameFormatter(ForceAllItemsIntoRandomItemManager.Instance ? ForceAllItemsIntoRandomItemManager.Instance.NextOverridePickupIndex : PickupIndex.none);
        }

        ChaosEffectComponent _effectComponent;

        bool _addedHooks;

        [SyncVar(hook = nameof(hookSetOverridePickupIndex))]
        int _overridePickupIndexInternal;

        [SerializedMember("p")]
        PickupIndex overridePickupIndex
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => new PickupIndex(_overridePickupIndexInternal - 1);
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set => _overridePickupIndexInternal = value.value + 1;
        }

        void Awake()
        {
            _effectComponent = GetComponent<ChaosEffectComponent>();
        }

        public override void OnStartServer()
        {
            base.OnStartServer();

            overridePickupIndex = ForceAllItemsIntoRandomItemManager.Instance.NextOverridePickupIndex;
        }

        void Start()
        {
            if (!NetworkServer.active)
                return;

            On.RoR2.PickupDropTable.GenerateDrop += PickupDropTable_GenerateDrop;
            On.RoR2.PickupDropTable.GenerateUniqueDrops += PickupDropTable_GenerateUniqueDrops;

            On.RoR2.ChestBehavior.PickFromList += ChestBehavior_PickFromList;

            AllVoidPotentials.OverrideAllowChoices += AllVoidPotentials_OverrideAllowChoices;

            On.RoR2.PickupPickerController.GetOptionsFromPickupIndex += PickupPickerController_GetOptionsFromPickupIndex;

            _addedHooks = true;

            rerollAllChests();
        }

        void OnDestroy()
        {
            if (_addedHooks)
            {
                On.RoR2.PickupDropTable.GenerateDrop -= PickupDropTable_GenerateDrop;
                On.RoR2.PickupDropTable.GenerateUniqueDrops -= PickupDropTable_GenerateUniqueDrops;

                On.RoR2.ChestBehavior.PickFromList -= ChestBehavior_PickFromList;

                AllVoidPotentials.OverrideAllowChoices -= AllVoidPotentials_OverrideAllowChoices;

                On.RoR2.PickupPickerController.GetOptionsFromPickupIndex -= PickupPickerController_GetOptionsFromPickupIndex;

                _addedHooks = false;
            }

            if (NetworkServer.active)
            {
                rerollAllChests();

                if (ForceAllItemsIntoRandomItemManager.Instance)
                {
                    ForceAllItemsIntoRandomItemManager.Instance.RollNextOverridePickup();
                }
            }
        }

        void hookSetOverridePickupIndex(int pickupIndexInt)
        {
            _overridePickupIndexInternal = pickupIndexInt;

            if (NetworkServer.active)
            {
                _effectComponent.EffectNameFormatter = new NameFormatter(overridePickupIndex);
            }
        }

        static void rerollAllChests()
        {
            foreach (ChestBehavior chestBehavior in InstanceTracker.GetInstancesList<ChestBehavior>())
            {
                chestBehavior.Roll();
            }

            foreach (OptionChestBehaviorTracker optionChestBehaviorTracker in InstanceTracker.GetInstancesList<OptionChestBehaviorTracker>())
            {
                OptionChestBehavior optionChestBehavior = optionChestBehaviorTracker.OptionChestBehavior;
                if (optionChestBehavior)
                {
                    optionChestBehavior.Roll();
                }
            }

            foreach (ShopTerminalBehaviorTracker shopTerminalBehaviorTracker in InstanceTracker.GetInstancesList<ShopTerminalBehaviorTracker>())
            {
                ShopTerminalBehavior shopTerminalBehavior = shopTerminalBehaviorTracker.ShopTerminalBehavior;
                if (!shopTerminalBehavior)
                    continue;

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

            foreach (VoidSuppressorBehaviorTracker voidSuppressorBehaviorTracker in InstanceTracker.GetInstancesList<VoidSuppressorBehaviorTracker>())
            {
                VoidSuppressorBehavior voidSuppressorBehavior = voidSuppressorBehaviorTracker.VoidSuppressorBehavior;
                if (voidSuppressorBehavior)
                {
                    voidSuppressorBehavior.RefreshItems();
                }
            }
        }

        PickupIndex PickupDropTable_GenerateDrop(On.RoR2.PickupDropTable.orig_GenerateDrop orig, PickupDropTable self, Xoroshiro128Plus rng)
        {
            orig(self, rng);
            return overridePickupIndex;
        }

        PickupIndex[] PickupDropTable_GenerateUniqueDrops(On.RoR2.PickupDropTable.orig_GenerateUniqueDrops orig, PickupDropTable self, int maxDrops, Xoroshiro128Plus rng)
        {
            PickupIndex[] result = orig(self, maxDrops, rng);
            ArrayUtils.SetAll(result, overridePickupIndex);
            return result;
        }

        void ChestBehavior_PickFromList(On.RoR2.ChestBehavior.orig_PickFromList orig, ChestBehavior self, List<PickupIndex> dropList)
        {
            dropList.Clear();
            dropList.Add(overridePickupIndex);

            orig(self, dropList);
        }

        void AllVoidPotentials_OverrideAllowChoices(PickupIndex originalPickup, ref bool allowChoices)
        {
            if (originalPickup == overridePickupIndex)
            {
                allowChoices = false;
            }
        }

        PickupPickerController.Option[] PickupPickerController_GetOptionsFromPickupIndex(On.RoR2.PickupPickerController.orig_GetOptionsFromPickupIndex orig, PickupIndex pickupIndex)
        {
            PickupPickerController.Option[] options = orig(pickupIndex);

            if (pickupIndex == overridePickupIndex)
            {
                ArrayUtils.SetAll(options, new PickupPickerController.Option
                {
                    pickupIndex = overridePickupIndex,
                    available = true
                });
            }

            return options;
        }
    }
}
