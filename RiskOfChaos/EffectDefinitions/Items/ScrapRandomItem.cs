using RiskOfChaos.EffectHandling;
using RiskOfChaos.Utilities;
using RoR2;
using System.Collections.Generic;
using System.Linq;

namespace RiskOfChaos.EffectDefinitions.Items
{
    [ChaosEffect("ScrapRandomItem", DefaultSelectionWeight = 0.8f, EffectRepetitionWeightExponent = 15f)]
    public class ScrapRandomItem : BaseEffect
    {
        static IEnumerable<ItemIndex> getAllScrappableItems(Inventory inventory)
        {
            if (!inventory)
                yield break;

            foreach (ItemIndex itemIndex in ItemCatalog.allItems)
            {
                int itemCount = inventory.GetItemCount(itemIndex);
                if (itemCount > 0)
                {
                    ItemDef itemDef = ItemCatalog.GetItemDef(itemIndex);
                    if (itemDef)
                    {
                        ItemTierDef itemTierDef = ItemTierCatalog.GetItemTierDef(itemDef.tier);
                        if (!itemDef.hidden &&
                            itemDef.canRemove &&
                            itemTierDef &&
                            itemTierDef.canScrap &&
                            itemDef.DoesNotContainTag(ItemTag.Scrap))
                        {
                            for (int i = 0; i < itemCount; i++)
                            {
                                yield return itemIndex;
                            }
                        }
                    }
                }
            }
        }

        [EffectCanActivate]
        static bool CanActivate()
        {
            return PlayerUtils.GetAllPlayerMasters(false).Any(cm => getAllScrappableItems(cm.inventory).Any());
        }

        public override void OnStart()
        {
            foreach (CharacterMaster playerMaster in PlayerUtils.GetAllPlayerMasters(false))
            {
                tryScrapRandomItem(playerMaster);
            }
        }

        void tryScrapRandomItem(CharacterMaster characterMaster)
        {
            Inventory inventory = characterMaster.inventory;
            if (!inventory)
                return;

            IEnumerable<ItemIndex> scrappableItems = getAllScrappableItems(inventory);
            if (!scrappableItems.Any())
                return;

            ItemDef itemToScrap = ItemCatalog.GetItemDef(RNG.NextElementUniform(scrappableItems.ToArray()));
            scrapItem(characterMaster, inventory, itemToScrap);
        }

        static void scrapItem(CharacterMaster characterMaster, Inventory inventory, ItemDef itemToScrap)
        {
            PickupDef scrapPickup = getScrapPickupForItemTier(itemToScrap.tier);
            if (scrapPickup == null)
                return;

            inventory.RemoveItem(itemToScrap, 1);
            inventory.GiveItem(scrapPickup.itemIndex, 1);

            CharacterMasterNotificationQueue.PushItemTransformNotification(characterMaster, itemToScrap.itemIndex, scrapPickup.itemIndex, CharacterMasterNotificationQueue.TransformationType.Default);
        }

        static PickupDef getScrapPickupForItemTier(ItemTier tier)
        {
            PickupIndex scrapPickupIndex;
            switch (tier)
            {
                case ItemTier.Tier1:
                    scrapPickupIndex = PickupCatalog.FindPickupIndex("ItemIndex.ScrapWhite");
                    break;
                case ItemTier.Tier2:
                    scrapPickupIndex = PickupCatalog.FindPickupIndex("ItemIndex.ScrapGreen");
                    break;
                case ItemTier.Tier3:
                    scrapPickupIndex = PickupCatalog.FindPickupIndex("ItemIndex.ScrapRed");
                    break;
                case ItemTier.Boss:
                    scrapPickupIndex = PickupCatalog.FindPickupIndex("ItemIndex.ScrapYellow");
                    break;
                default:
                    return null;
            }

            return PickupCatalog.GetPickupDef(scrapPickupIndex);
        }
    }
}
