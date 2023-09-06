using RiskOfChaos.ConfigHandling;
using RiskOfChaos.EffectHandling;
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
using System.Linq;

namespace RiskOfChaos.EffectDefinitions.Character.Player.Items
{
    [ChaosEffect("unscrap_random_item")]
    public sealed class UnscrapRandomItem : BaseEffect
    {
        [EffectConfig]
        static readonly ConfigHolder<int> _unscrapItemCount =
            ConfigFactory<int>.CreateConfig("Unscrap Count", 1)
                              .Description("How many items should be unscrapped per player")
                              .OptionConfig(new IntSliderConfig
                              {
                                  min = 1,
                                  max = 10
                              })
                              .ValueConstrictor(CommonValueConstrictors.GreaterThanOrEqualTo(1))
                              .Build();

        [EffectConfig]
        static readonly ConfigHolder<string> _itemBlacklistConfig =
            ConfigFactory<string>.CreateConfig("Item Blacklist", string.Empty)
                                 .Description("A comma-separated list of items that should not be allowed to be unscrapped to, or any item scrap that should not be allowed to be unscrapped. Both internal and English display names are accepted, with spaces and commas removed.")
                                 .OptionConfig(new InputFieldConfig
                                 {
                                     submitOn = InputFieldConfig.SubmitEnum.OnSubmit
                                 })
                                 .Build();

        static readonly ParsedItemList _itemBlacklist = new ParsedItemList(ItemIndexComparer.Instance)
        {
            ConfigHolder = _itemBlacklistConfig
        };

        static Dictionary<ItemTier, ItemIndex[]> _printableItemsByTier = new Dictionary<ItemTier, ItemIndex[]>();

        [SystemInitializer(typeof(ItemCatalog))]
        static void Init()
        {
            _printableItemsByTier = ItemCatalog.allItemDefs.Where(i => !i.hidden
                                                                       && i.DoesNotContainTag(ItemTag.Scrap)
                                                                       && i.DoesNotContainTag(ItemTag.PriorityScrap)
                                                                       && i.DoesNotContainTag(ItemTag.WorldUnique)
                                                                       && i.DoesNotContainTag(ItemTag.CannotDuplicate))
                                                           .GroupBy(itemDef => itemDef.tier)
                                                           .ToDictionary(g => g.Key,
                                                                         g => g.Select(g => g.itemIndex).ToArray());
        }

        [EffectCanActivate]
        static bool CanActivate(in EffectCanActivateContext context)
        {
            return !context.IsNow || PlayerUtils.GetAllPlayerMasters(false).Any(m =>
            {
                return m && getAllScrapItems(m.inventory).Any(i =>
                {
                    return _printableItemsByTier.TryGetValue(i.tier, out ItemIndex[] printableItems) && printableItems.Any(canUnscrapToItem);
                });
            });
        }

        static IEnumerable<ItemDef> getAllScrapItems(Inventory inventory)
        {
            if (!inventory)
                yield break;

            foreach (ItemIndex item in inventory.itemAcquisitionOrder)
            {
                ItemDef itemDef = ItemCatalog.GetItemDef(item);
                if (itemDef.ContainsTag(ItemTag.Scrap) || itemDef.ContainsTag(ItemTag.PriorityScrap))
                {
                    if (_itemBlacklist.Contains(item))
                    {
#if DEBUG
                        Log.Debug($"Not including scrap {itemDef}: Blacklist");
#endif
                        continue;
                    }

                    yield return itemDef;
                }
            }
        }

        static bool canUnscrapToItem(ItemIndex item)
        {
            Run run = Run.instance;
            if (run && !run.IsItemAvailable(item))
                return false;

            if (_itemBlacklist.Contains(item))
                return false;

            return true;
        }

        public override void OnStart()
        {
            PlayerUtils.GetAllPlayerMasters(false).TryDo(tryUnscrapRandomItem, Util.GetBestMasterName);
        }

        void tryUnscrapRandomItem(CharacterMaster master)
        {
            if (!master)
                return;

            Inventory inventory = master.inventory;
            if (!inventory)
                return;

            List<ItemDef> availableScrapItems = getAllScrapItems(inventory).ToList();
            if (availableScrapItems.Count <= 0)
                return;

            int numScrapConverted = 0;
            while (availableScrapItems.Count > 0)
            {
                if (tryConvertScrap(master, inventory, availableScrapItems.GetAndRemoveRandom(RNG)))
                {
                    if (++numScrapConverted >= _unscrapItemCount.Value)
                        break;
                }
            }

            if (numScrapConverted == 0)
            {
                Log.Warning($"{Util.GetBestMasterName(master)} has scrap items, but none could be converted to an item");
            }
        }

        bool tryConvertScrap(CharacterMaster master, Inventory inventory, ItemDef scrapItem)
        {
            int scrapCount = inventory.GetItemCount(scrapItem);

            if (_printableItemsByTier.TryGetValue(scrapItem.tier, out ItemIndex[] availableItems))
            {
                List<ItemIndex> runAvailableItems = availableItems.Where(canUnscrapToItem).ToList();
                if (runAvailableItems.Count <= 0)
                    return false;

                ItemIndex newItem = RNG.NextElementUniform(runAvailableItems);
                inventory.GiveItem(newItem, scrapCount);

                CharacterMasterNotificationQueue.SendTransformNotification(master, scrapItem.itemIndex, newItem, CharacterMasterNotificationQueue.TransformationType.Default);
            }
            else
            {
                return false;
            }

            inventory.RemoveItem(scrapItem, scrapCount);

            if (scrapItem == DLC1Content.Items.RegeneratingScrap)
            {
                inventory.GiveItem(DLC1Content.Items.RegeneratingScrapConsumed, scrapCount);
                CharacterMasterNotificationQueue.SendTransformNotification(master, DLC1Content.Items.RegeneratingScrap.itemIndex, DLC1Content.Items.RegeneratingScrapConsumed.itemIndex, CharacterMasterNotificationQueue.TransformationType.Default);
            }

            return true;
        }
    }
}
