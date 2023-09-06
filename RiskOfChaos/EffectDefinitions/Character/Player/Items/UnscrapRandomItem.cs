using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Methods;
using RiskOfChaos.Utilities;
using RiskOfChaos.Utilities.Extensions;
using RoR2;
using System.Collections.Generic;
using System.Linq;

namespace RiskOfChaos.EffectDefinitions.Character.Player.Items
{
    [ChaosEffect("unscrap_random_item")]
    public sealed class UnscrapRandomItem : BaseEffect
    {
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
        static bool CanActivate()
        {
            return PlayerUtils.GetAllPlayerMasters(false).Any(m => m && getAllScrapItems(m.inventory).Any());
        }

        static IEnumerable<ItemDef> getAllScrapItems(Inventory inventory)
        {
            if (!inventory)
                yield break;

            foreach (ItemIndex item in inventory.itemAcquisitionOrder)
            {
                ItemDef itemDef = ItemCatalog.GetItemDef(item);
                if (itemDef.ContainsTag(ItemTag.Scrap) || itemDef.ContainsTag(ItemTag.PriorityScrap))
                    yield return itemDef;
            }
        }

        public override void OnStart()
        {
            PlayerUtils.GetAllPlayerMasters(false).TryDo(master =>
            {
                if (!master)
                    return;

                Inventory inventory = master.inventory;
                if (!inventory)
                    return;

                List<ItemDef> availableScrapItems = getAllScrapItems(inventory).ToList();
                if (availableScrapItems.Count <= 0)
                    return;

                bool tryConvertScrap(ItemDef scrapItem)
                {
                    int scrapCount = inventory.GetItemCount(scrapItem);

                    if (_printableItemsByTier.TryGetValue(scrapItem.tier, out ItemIndex[] availableItems))
                    {
                        List<ItemIndex> runAvailableItems = availableItems.Where(i => Run.instance.IsItemAvailable(i) && !Run.instance.IsItemExpansionLocked(i)).ToList();

                        if (runAvailableItems.Count <= 0)
                        {
                            return false;
                        }

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

                while (availableScrapItems.Count > 0)
                {
                    if (tryConvertScrap(availableScrapItems.GetAndRemoveRandom(RNG)))
                        return;
                }

                Log.Warning($"{Util.GetBestMasterName(master)} has scrap items, but none could be converted to an item");
            }, Util.GetBestMasterName);
        }
    }
}
