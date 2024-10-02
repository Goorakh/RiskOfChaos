using RiskOfChaos.Collections.ParsedValue;
using RiskOfChaos.ConfigHandling;
using RiskOfChaos.ConfigHandling.AcceptableValues;
using RiskOfChaos.EffectHandling;
using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Data;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Methods;
using RiskOfChaos.Utilities;
using RiskOfChaos.Utilities.Comparers;
using RiskOfChaos.Utilities.Extensions;
using RiskOfOptions.OptionConfigs;
using RoR2;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.AddressableAssets;

namespace RiskOfChaos.EffectDefinitions.Character.Player.Items
{
    [ChaosEffect("cleanse_random_item", DefaultSelectionWeight = 0.5f)]
    public sealed class CleanseRandomItem : BaseEffect
    {
        static PickupDropTable _pearlDropTable;

        [EffectConfig]
        static readonly ConfigHolder<int> _cleanseCount =
            ConfigFactory<int>.CreateConfig("Cleanse Count", 1)
                              .Description("How many items should be cleansed per player")
                              .AcceptableValues(new AcceptableValueMin<int>(1))
                              .OptionConfig(new IntFieldConfig { Min = 1 })
                              .Build();

        [EffectConfig]
        static readonly ConfigHolder<string> _itemBlacklistConfig =
            ConfigFactory<string>.CreateConfig("Cleanse Blacklist", "Pearl,ShinyPearl")
                                 .Description("A comma-separated list of items and equipment that should not be allowed to be cleansed. Both internal and English display names are accepted, with spaces and commas removed.")
                                 .OptionConfig(new InputFieldConfig
                                 {
                                     lineType = TMPro.TMP_InputField.LineType.SingleLine,
                                     submitOn = InputFieldConfig.SubmitEnum.OnExitOrSubmit
                                 })
                                 .Build();

        static readonly ParsedPickupList _itemBlacklist = new ParsedPickupList(PickupIndexComparer.Instance)
        {
            ConfigHolder = _itemBlacklistConfig
        };

        [SystemInitializer]
        static void Init()
        {
            _pearlDropTable = Addressables.LoadAssetAsync<ExplicitPickupDropTable>("RoR2/Base/ShrineCleanse/dtPearls.asset").WaitForCompletion();

            if (!_pearlDropTable)
            {
                Log.Error("Failed to load pearl drop table");
            }
        }

        static IEnumerable<PickupDef> getAllCleansableItems()
        {
            foreach (PickupDef pickup in PickupCatalog.allPickups)
            {
                bool isValidForCleanse = false;
                if (pickup.itemIndex != ItemIndex.None)
                {
                    ItemDef itemDef = ItemCatalog.GetItemDef(pickup.itemIndex);
                    if (itemDef && !itemDef.hidden && itemDef.canRemove)
                    {
                        isValidForCleanse = true;
                    }
                }
                else if (pickup.equipmentIndex != EquipmentIndex.None)
                {
                    EquipmentDef equipmentDef = EquipmentCatalog.GetEquipmentDef(pickup.equipmentIndex);
                    if (equipmentDef)
                    {
                        isValidForCleanse = true;
                    }
                }

                if (!isValidForCleanse)
                    continue;

                if (_itemBlacklist.Contains(pickup.pickupIndex))
                {
#if DEBUG
                    Log.Debug($"Not allowing {pickup.internalName}: Blacklist");
#endif
                    continue;
                }

                yield return pickup;
            }
        }

        static bool isPrimaryCleansable(PickupDef pickup)
        {
            if (pickup.itemIndex != ItemIndex.None)
            {
                ItemDef item = ItemCatalog.GetItemDef(pickup.itemIndex);
                return item.tier == ItemTier.Lunar;
            }
            else if (pickup.equipmentIndex != EquipmentIndex.None)
            {
                EquipmentDef equipment = EquipmentCatalog.GetEquipmentDef(pickup.equipmentIndex);
                return equipment.isLunar;
            }
            else
            {
                return false;
            }
        }

        [EffectCanActivate]
        static bool CanActivate(in EffectCanActivateContext context)
        {
            return _pearlDropTable && (!context.IsNow || PlayerUtils.GetAllPlayerMasters(false).Any(m => getAllCleansableItems().Any(pickup => m.inventory.GetPickupCount(pickup) > 0)));
        }

        ulong _cleanseRNGSeed;
        PickupIndex[,] _cleanseOrder;

        public override void OnPreStartServer()
        {
            base.OnPreStartServer();

            _cleanseRNGSeed = RNG.nextUlong;

            List<PickupIndex> primaryCleansables = [];
            List<PickupIndex> secondaryCleansables = [];

            foreach (PickupDef cleansablePickup in getAllCleansableItems())
            {
                if (isPrimaryCleansable(cleansablePickup))
                {
                    primaryCleansables.Add(cleansablePickup.pickupIndex);
                }
                else
                {
                    secondaryCleansables.Add(cleansablePickup.pickupIndex);
                }
            }

            int cleanseCount = _cleanseCount.Value;

            _cleanseOrder = new PickupIndex[cleanseCount, primaryCleansables.Count + secondaryCleansables.Count];
            for (int i = 0; i < cleanseCount; i++)
            {
                Util.ShuffleList(primaryCleansables, RNG.Branch());
                Util.ShuffleList(secondaryCleansables, RNG.Branch());

                for (int j = 0; j < primaryCleansables.Count; j++)
                {
                    _cleanseOrder[i, j] = primaryCleansables[j];
                }

                for (int j = 0; j < secondaryCleansables.Count; j++)
                {
                    _cleanseOrder[i, primaryCleansables.Count + j] = secondaryCleansables[j];
                }
            }
        }

        public override void OnStart()
        {
            PlayerUtils.GetAllPlayerMasters(false).TryDo(m =>
            {
                tryCleanseRandomItem(m, new Xoroshiro128Plus(_cleanseRNGSeed));
            }, Util.GetBestMasterName);
        }

        void tryCleanseRandomItem(CharacterMaster master, Xoroshiro128Plus rng)
        {
            Inventory inventory = master.inventory;
            if (!inventory)
                return;

            int cleanseCount = _cleanseOrder.GetLength(0);
            int cleansablesCount = _cleanseOrder.GetLength(1);

            HashSet<PickupIndex> givenPearlItems = [];

            for (int i = 0; i < cleanseCount; i++)
            {
                PickupDef pickupToCleanse = null;

                for (int j = 0; j < cleansablesCount; j++)
                {
                    PickupDef candidatePickup = PickupCatalog.GetPickupDef(_cleanseOrder[i, j]);
                    if (inventory.GetPickupCount(candidatePickup) > 0)
                    {
                        pickupToCleanse = candidatePickup;
                        break;
                    }
                }

                if (pickupToCleanse == null)
                    break;

                if (inventory.TryRemove(pickupToCleanse))
                {
                    PickupIndex pearlPickupIndex = _pearlDropTable.GenerateDrop(rng.Branch());
                    givenPearlItems.Add(pearlPickupIndex);

                    PickupDef pearlPickup = PickupCatalog.GetPickupDef(pearlPickupIndex);

                    inventory.TryGrant(pearlPickup, false);

                    CharacterMasterNotificationQueueUtils.SendPickupTransformNotification(master, pickupToCleanse.pickupIndex, pearlPickupIndex, CharacterMasterNotificationQueue.TransformationType.Default);
                }
            }

            if (givenPearlItems.Count > 0)
            {
                RoR2Application.onNextUpdate += () =>
                {
                    if (!master || !inventory)
                        return;

                    foreach (PickupIndex pearlPickupIndex in givenPearlItems)
                    {
                        PickupDef pearlPickup = PickupCatalog.GetPickupDef(pearlPickupIndex);
                        if (pearlPickup == null)
                            continue;

                        Chat.AddPickupMessage(master.GetBody(), pearlPickup.nameToken, pearlPickup.baseColor, (uint)inventory.GetPickupCount(pearlPickup));
                    }
                };
            }
        }
    }
}
