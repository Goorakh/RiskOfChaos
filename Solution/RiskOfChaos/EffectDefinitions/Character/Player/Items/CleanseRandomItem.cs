using RiskOfChaos.Collections.ParsedValue;
using RiskOfChaos.ConfigHandling;
using RiskOfChaos.ConfigHandling.AcceptableValues;
using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Data;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Methods;
using RiskOfChaos.EffectHandling.EffectComponents;
using RiskOfChaos.Utilities;
using RiskOfChaos.Utilities.Comparers;
using RiskOfChaos.Utilities.Extensions;
using RiskOfChaos.Utilities.Pickup;
using RiskOfOptions.OptionConfigs;
using RoR2;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.AddressableAssets;
using UnityEngine.Networking;

namespace RiskOfChaos.EffectDefinitions.Character.Player.Items
{
    [ChaosEffect("cleanse_random_item", DefaultSelectionWeight = 0.6f)]
    public sealed class CleanseRandomItem : NetworkBehaviour
    {
        [EffectConfig]
        static readonly ConfigHolder<bool> _onlyCleanseLunar =
            ConfigFactory<bool>.CreateConfig("Only Cleanse Lunars", true)
                               .Description("Limits the effect to only cleanse lunar items")
                               .OptionConfig(new CheckBoxConfig())
                               .Build();

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

        static PickupDropTable _pearlDropTable;

        [SystemInitializer]
        static void Init()
        {
            Addressables.LoadAssetAsync<ExplicitPickupDropTable>("RoR2/Base/ShrineCleanse/dtPearls.asset").OnSuccess(dropTable => _pearlDropTable = dropTable);
        }

        static IEnumerable<PickupDef> getAllCleansablePickups()
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

                if (_onlyCleanseLunar.Value && !pickup.isLunar)
                {
                    isValidForCleanse = false;
                }

                if (!isValidForCleanse)
                    continue;

                if (_itemBlacklist.Contains(pickup.pickupIndex))
                {
                    Log.Debug($"Not allowing {pickup.internalName}: Blacklist");
                    continue;
                }

                yield return pickup;
            }
        }

        [EffectCanActivate]
        static bool CanActivate()
        {
            return _pearlDropTable && PlayerUtils.GetAllPlayerMasters(false).Any(m => getAllCleansablePickups().Any(pickup => m.inventory.GetPickupCount(pickup) > 0));
        }

        ChaosEffectComponent _effectComponent;

        ulong _cleanseRNGSeed;
        PickupIndex[,] _cleanseOrder;

        void Awake()
        {
            _effectComponent = GetComponent<ChaosEffectComponent>();
        }

        public override void OnStartServer()
        {
            base.OnStartServer();

            Xoroshiro128Plus rng = new Xoroshiro128Plus(_effectComponent.Rng.nextUlong);

            _cleanseRNGSeed = rng.nextUlong;

            List<PickupDef> allCleansablePickups = [.. getAllCleansablePickups()];

            List<PickupIndex> primaryCleansables = new List<PickupIndex>(allCleansablePickups.Count);
            List<PickupIndex> secondaryCleansables = new List<PickupIndex>(allCleansablePickups.Count);

            foreach (PickupDef cleansablePickup in allCleansablePickups)
            {
                List<PickupIndex> cleansablesList = cleansablePickup.isLunar ? primaryCleansables : secondaryCleansables;
                cleansablesList.Add(cleansablePickup.pickupIndex);
            }

            int cleanseCount = _cleanseCount.Value;

            _cleanseOrder = new PickupIndex[cleanseCount, primaryCleansables.Count + secondaryCleansables.Count];
            for (int i = 0; i < cleanseCount; i++)
            {
                Util.ShuffleList(primaryCleansables, rng.Branch());
                Util.ShuffleList(secondaryCleansables, rng.Branch());

                int addedCleansables = 0;

                for (int j = 0; j < primaryCleansables.Count; j++)
                {
                    _cleanseOrder[i, addedCleansables + j] = primaryCleansables[j];
                }

                addedCleansables += primaryCleansables.Count;

                for (int j = 0; j < secondaryCleansables.Count; j++)
                {
                    _cleanseOrder[i, addedCleansables + j] = secondaryCleansables[j];
                }
            }
        }

        void Start()
        {
            if (NetworkServer.active)
            {
                PlayerUtils.GetAllPlayerMasters(false).TryDo(m =>
                {
                    tryCleanseRandomItem(m, new Xoroshiro128Plus(_cleanseRNGSeed));
                }, Util.GetBestMasterName);
            }
        }

        void tryCleanseRandomItem(CharacterMaster master, Xoroshiro128Plus rng)
        {
            Inventory inventory = master.inventory;
            if (!inventory)
                return;

            int cleanseCount = _cleanseOrder.GetLength(0);
            int cleansablesCount = _cleanseOrder.GetLength(1);

            HashSet<PickupIndex> grantedPickups = new HashSet<PickupIndex>(_pearlDropTable.GetPickupCount());

            for (int i = 0; i < cleanseCount; i++)
            {
                PickupIndex pickupToCleanse = PickupIndex.none;

                for (int j = 0; j < cleansablesCount; j++)
                {
                    PickupIndex candidatePickup = _cleanseOrder[i, j];
                    if (inventory.GetPickupCount(candidatePickup) > 0)
                    {
                        pickupToCleanse = candidatePickup;
                        break;
                    }
                }

                if (!pickupToCleanse.isValid)
                    break;

                if (inventory.TryRemove(pickupToCleanse))
                {
                    PickupIndex pearlPickupIndex = _pearlDropTable.GenerateDrop(rng.Branch());

                    if (inventory.TryGrant(pearlPickupIndex, InventoryExtensions.EquipmentReplacementRule.DropExisting))
                    {
                        CharacterMasterNotificationQueueUtils.SendPickupTransformNotification(master, pickupToCleanse, pearlPickupIndex, CharacterMasterNotificationQueue.TransformationType.Default);

                        grantedPickups.Add(pearlPickupIndex);
                    }
                }
            }

            if (grantedPickups.Count > 0)
            {
                PickupUtils.QueuePickupsMessage(master, [.. grantedPickups], PickupNotificationFlags.SendChatMessage);
            }
        }
    }
}
