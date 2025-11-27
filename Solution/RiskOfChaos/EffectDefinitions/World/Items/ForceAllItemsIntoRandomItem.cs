using RiskOfChaos.ConfigHandling;
using RiskOfChaos.EffectHandling;
using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Data;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Methods;
using RiskOfChaos.EffectHandling.EffectComponents;
using RiskOfChaos.EffectHandling.Formatting;
using RiskOfChaos.EffectUtils.World;
using RiskOfChaos.Networking;
using RiskOfChaos.SaveHandling;
using RiskOfChaos.Trackers;
using RiskOfChaos.Utilities;
using RiskOfChaos.Utilities.DropTables;
using RiskOfOptions.OptionConfigs;
using RoR2;
using RoR2.ExpansionManagement;
using System;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.Networking;

namespace RiskOfChaos.EffectDefinitions.World.Items
{
    [ChaosTimedEffect("force_all_items_into_random_item", TimedEffectType.UntilStageEnd, AllowDuplicates = false, ConfigName = "All Items Are A Random Item", DefaultSelectionWeight = 0.8f)]
    public sealed class ForceAllItemsIntoRandomItem : NetworkBehaviour
    {
        [InitEffectInfo]
        public static readonly TimedEffectInfo EffectInfo;

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

        static EffectIndex _recycleEffectIndex = EffectIndex.Invalid;

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
            _dropTable.RegisterDrop(DropType.PowerShape, 0.1f);
            _dropTable.RegisterDrop(DropType.FoodTier, 0.5f);

            _dropTable.CreateItemBlacklistConfig("Item Blacklist", "A comma-separated list of items and equipment that should not be included for the effect. Both internal and English display names are accepted, with spaces and commas removed.");

            _dropTable.AddDrops += (drops) =>
            {
                drops.Add(new ExplicitDrop(RoR2Content.Items.CaptainDefenseMatrix.itemIndex, DropType.Tier3, ExpansionIndex.None));
                drops.Add(new ExplicitDrop(RoR2Content.Items.Pearl.itemIndex, DropType.Boss, ExpansionIndex.None));
                drops.Add(new ExplicitDrop(RoR2Content.Items.ShinyPearl.itemIndex, DropType.Boss, ExpansionIndex.None));

                if (_allowEliteEquipments.Value)
                {
                    foreach (EliteIndex eliteIndex in EliteUtils.GetRunAvailableElites(true))
                    {
                        EliteDef eliteDef = EliteCatalog.GetEliteDef(eliteIndex);
                        if (!eliteDef)
                            continue;

                        EquipmentDef eliteEquipmentDef = eliteDef.eliteEquipmentDef;
                        if (!eliteEquipmentDef)
                            continue;

                        ExpansionIndex requiredExpansionIndex = ExpansionIndex.None;
                        if (eliteEquipmentDef.requiredExpansion)
                        {
                            requiredExpansionIndex = eliteEquipmentDef.requiredExpansion.expansionIndex;
                        }

                        drops.Add(new ExplicitDrop(eliteEquipmentDef.equipmentIndex, DropType.Equipment, requiredExpansionIndex));
                    }
                }
            };
        }

        [SystemInitializer(typeof(EffectCatalogUtils))]
        static void Init()
        {
            ForceAllItemsIntoRandomItemManager.OnNextOverridePickupChanged += onNextOverridePickupChanged;

            _recycleEffectIndex = EffectCatalogUtils.FindEffectIndex("OmniRecycleEffect");
            if (_recycleEffectIndex == EffectIndex.Invalid)
            {
                Log.Error($"Failed to find recycle effect index");
            }
        }

        static void onNextOverridePickupChanged()
        {
            if (NetworkServer.active)
            {
                EffectInfo?.RestoreStaticDisplayNameFormatter();
            }
        }

        public static UniquePickup GenerateOverridePickup(Xoroshiro128Plus rng)
        {
            _dropTable.RegenerateIfNeeded();
            return _dropTable.GeneratePickup(rng);
        }

        [EffectCanActivate]
        static bool CanActivate()
        {
            return ForceAllItemsIntoRandomItemManager.Instance && ForceAllItemsIntoRandomItemManager.Instance.NextOverridePickup.isValid;
        }

        [GetEffectNameFormatter]
        static EffectNameFormatter GetNameFormatter()
        {
            UniquePickup nextOverridePickupIndex = UniquePickup.none;
            if (ForceAllItemsIntoRandomItemManager.Instance)
            {
                nextOverridePickupIndex = ForceAllItemsIntoRandomItemManager.Instance.NextOverridePickup;
            }

            return new NameFormatter(nextOverridePickupIndex);
        }

        ChaosEffectComponent _effectComponent;
        ChaosEffectNameComponent _effectNameComponent;

        [SyncVar(hook = nameof(hookSetOverridePickupInternal))]
        UniquePickupWrapper _overridePickupInternal;

        [SerializedMember("p")]
        public UniquePickup OverridePickup
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _overridePickupInternal;
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private set => _overridePickupInternal = value;
        }

        void Awake()
        {
            _effectComponent = GetComponent<ChaosEffectComponent>();
            _effectNameComponent = GetComponent<ChaosEffectNameComponent>();
        }

        public override void OnStartServer()
        {
            base.OnStartServer();

            OverridePickup = ForceAllItemsIntoRandomItemManager.Instance.NextOverridePickup;
        }

        void OnDestroy()
        {
            if (NetworkServer.active)
            {
                rerollAllChests();

                if (ForceAllItemsIntoRandomItemManager.Instance)
                {
                    ForceAllItemsIntoRandomItemManager.Instance.RollNextOverridePickup();
                }
            }
        }

        void hookSetOverridePickupInternal(UniquePickupWrapper pickupWrapper)
        {
            UniquePickup pickup = pickupWrapper;

            bool changed = _overridePickupInternal != pickup;

            _overridePickupInternal = pickupWrapper;

            if (NetworkServer.active && changed)
            {
                _effectNameComponent.SetCustomNameFormatter(new NameFormatter(OverridePickup));

                if (OverridePickup.isValid)
                {
                    foreach (GenericPickupController pickupController in InstanceTracker.GetInstancesList<GenericPickupController>())
                    {
                        if (pickupController.pickup != OverridePickup)
                        {
                            pickupController.pickup = OverridePickup;

                            if (_recycleEffectIndex != EffectIndex.Invalid && pickupController.pickupDisplay)
                            {
                                EffectManager.SpawnEffect(_recycleEffectIndex, new EffectData
                                {
                                    origin = pickupController.pickupDisplay.transform.position
                                }, true);
                            }
                        }
                    }

                    foreach (OptionPickupTracker optionPickupTracker in InstanceTracker.GetInstancesList<OptionPickupTracker>())
                    {
                        PickupPickerController pickupPickerController = optionPickupTracker.PickupPickerController;
                        if (!pickupPickerController)
                            continue;

                        if (pickupPickerController.options != null)
                        {
                            Array.Fill(pickupPickerController.options, new PickupPickerController.Option
                            {
                                pickup = OverridePickup,
                                available = true
                            });
                        }
                    }
                }

                rerollAllChests();
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

                if (shopTerminalBehavior.CurrentPickup() == UniquePickup.none)
                {
                    Log.Debug($"Skipping reroll of {shopTerminalBehavior}, no current pickup");
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

            foreach (PickupDistributorBehaviorTracker pickupDistributorBehaviorTracker in InstanceTracker.GetInstancesList<PickupDistributorBehaviorTracker>())
            {
                PickupDistributorBehavior pickupDistributorBehavior = pickupDistributorBehaviorTracker.PickupDistributorBehavior;
                if (pickupDistributorBehavior)
                {
                    pickupDistributorBehavior.RerollPickup();
                }
            }
        }

        sealed class NameFormatter : EffectNameFormatter
        {
            UniquePickup _pickup;

            public NameFormatter()
            {
            }

            public NameFormatter(UniquePickup pickup)
            {
                _pickup = pickup;
            }

            public override void Serialize(NetworkWriter writer)
            {
                writer.Write(_pickup);
            }

            public override void Deserialize(NetworkReader reader)
            {
                _pickup = reader.ReadUniquePickup();
                invokeFormatterDirty();
            }

            public override object[] GetFormatArgs()
            {
                PickupDef pickupDef = PickupCatalog.GetPickupDef(_pickup.pickupIndex);
                if (pickupDef != null)
                {
                    string pickupDisplayName = Language.GetString(pickupDef.nameToken);

                    if (_pickup.isTempItem)
                    {
                        pickupDisplayName = Language.GetStringFormatted("ITEM_MODIFIER_TEMP", pickupDisplayName);
                    }

                    pickupDisplayName = Util.GenerateColoredString(pickupDisplayName, pickupDef.baseColor);

                    return [pickupDisplayName];
                }
                else
                {
                    return ["<color=red>[ERROR: PICKUP NOT ROLLED]</color>"];
                }
            }

            public override bool Equals(EffectNameFormatter other)
            {
                return other is NameFormatter nameFormatter &&
                       _pickup == nameFormatter._pickup;
            }
        }
    }
}
