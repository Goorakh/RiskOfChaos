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
using RiskOfChaos.Utilities.Extensions;
using RiskOfOptions.OptionConfigs;
using RoR2;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.AddressableAssets;
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

        static GameObject _recycleEffectPrefab;

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

            _dropTable.AddDrops += (drops) =>
            {
                drops.Add(new ExplicitDrop(RoR2Content.Items.CaptainDefenseMatrix.itemIndex, DropType.Tier3, null));
                drops.Add(new ExplicitDrop(RoR2Content.Items.Pearl.itemIndex, DropType.Boss, null));
                drops.Add(new ExplicitDrop(RoR2Content.Items.ShinyPearl.itemIndex, DropType.Boss, null));

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

                        drops.Add(new ExplicitDrop(eliteEquipmentDef.equipmentIndex, DropType.Equipment, eliteEquipmentDef.requiredExpansion));
                    }
                }
            };
        }

        [SystemInitializer]
        static void Init()
        {
            ForceAllItemsIntoRandomItemManager.OnNextOverridePickupChanged += onNextOverridePickupChanged;

            Addressables.LoadAssetAsync<GameObject>("RoR2/Base/Recycle/OmniRecycleEffect.prefab").OnSuccess(recycleEffect => _recycleEffectPrefab = recycleEffect);
        }

        static void onNextOverridePickupChanged()
        {
            if (NetworkServer.active)
            {
                EffectInfo?.RestoreStaticDisplayNameFormatter();
            }
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
            PickupIndex nextOverridePickupIndex = PickupIndex.none;
            if (ForceAllItemsIntoRandomItemManager.Instance)
            {
                nextOverridePickupIndex = ForceAllItemsIntoRandomItemManager.Instance.NextOverridePickupIndex;
            }

            return new NameFormatter(nextOverridePickupIndex);
        }

        ChaosEffectComponent _effectComponent;
        ChaosEffectNameComponent _effectNameComponent;

        [SyncVar(hook = nameof(hookSetOverridePickupIndex))]
        int _overridePickupIndexInternal;

        [SerializedMember("p")]
        public PickupIndex OverridePickupIndex
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => new PickupIndex(_overridePickupIndexInternal - 1);
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private set => _overridePickupIndexInternal = value.@value + 1;
        }

        void Awake()
        {
            _effectComponent = GetComponent<ChaosEffectComponent>();
            _effectNameComponent = GetComponent<ChaosEffectNameComponent>();
        }

        public override void OnStartServer()
        {
            base.OnStartServer();

            OverridePickupIndex = ForceAllItemsIntoRandomItemManager.Instance.NextOverridePickupIndex;
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

        void hookSetOverridePickupIndex(int pickupIndexInt)
        {
            bool changed = _overridePickupIndexInternal != pickupIndexInt;

            _overridePickupIndexInternal = pickupIndexInt;

            if (NetworkServer.active && changed)
            {
                _effectNameComponent.SetCustomNameFormatter(new NameFormatter(OverridePickupIndex));

                if (OverridePickupIndex.isValid)
                {
                    foreach (GenericPickupController pickupController in InstanceTracker.GetInstancesList<GenericPickupController>())
                    {
                        if (pickupController.pickupIndex != OverridePickupIndex)
                        {
                            pickupController.NetworkpickupIndex = OverridePickupIndex;

                            if (_recycleEffectPrefab && pickupController.pickupDisplay)
                            {
                                EffectManager.SimpleEffect(_recycleEffectPrefab, pickupController.pickupDisplay.transform.position, Quaternion.identity, true);
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
                            ArrayUtils.SetAll(pickupPickerController.options, new PickupPickerController.Option
                            {
                                pickupIndex = OverridePickupIndex,
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

                if (shopTerminalBehavior.CurrentPickupIndex() == PickupIndex.none)
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
        }

        sealed class NameFormatter : EffectNameFormatter
        {
            PickupIndex _pickupIndex;

            public NameFormatter()
            {
            }

            public NameFormatter(PickupIndex pickup)
            {
                _pickupIndex = pickup;
            }

            public override void Serialize(NetworkWriter writer)
            {
                writer.Write(_pickupIndex);
            }

            public override void Deserialize(NetworkReader reader)
            {
                _pickupIndex = reader.ReadPickupIndex();
                invokeFormatterDirty();
            }

            public override object[] GetFormatArgs()
            {
                PickupDef pickupDef = PickupCatalog.GetPickupDef(_pickupIndex);
                if (pickupDef != null)
                {
                    return [Util.GenerateColoredString(Language.GetString(pickupDef.nameToken), pickupDef.baseColor)];
                }
                else
                {
                    return ["<color=red>[ERROR: PICKUP NOT ROLLED]</color>"];
                }
            }

            public override bool Equals(EffectNameFormatter other)
            {
                return other is NameFormatter nameFormatter &&
                       _pickupIndex == nameFormatter._pickupIndex;
            }
        }
    }
}
