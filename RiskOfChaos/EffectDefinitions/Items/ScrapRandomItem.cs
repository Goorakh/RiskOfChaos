using HG;
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
        static PickupIndex[] _scrapPickupByItemTier;

        [SystemInitializer(typeof(PickupCatalog), typeof(ItemTierCatalog))]
        static void InitItemScrapDict()
        {
            int itemTierCount = ItemTierCatalog.allItemTierDefs.Max(itd => (int)itd.tier) + 1;

            _scrapPickupByItemTier = new PickupIndex[itemTierCount];
            for (ItemTier i = 0; i < (ItemTier)itemTierCount; i++)
            {
                _scrapPickupByItemTier[(int)i] = i switch
                {
                    ItemTier.Tier1 => PickupCatalog.FindPickupIndex(RoR2Content.Items.ScrapWhite.itemIndex),
                    ItemTier.Tier2 => PickupCatalog.FindPickupIndex(RoR2Content.Items.ScrapGreen.itemIndex),
                    ItemTier.Tier3 => PickupCatalog.FindPickupIndex(RoR2Content.Items.ScrapRed.itemIndex),
                    ItemTier.Boss => PickupCatalog.FindPickupIndex(RoR2Content.Items.ScrapYellow.itemIndex),
                    _ => PickupIndex.none,
                };
            }
        }

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
            return _scrapPickupByItemTier != null && PlayerUtils.GetAllPlayerMasters(false).Any(cm => getAllScrappableItems(cm.inventory).Any());
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

            GenericPickupController.SendPickupMessage(characterMaster, scrapPickup.pickupIndex);
        }

        static PickupDef getScrapPickupForItemTier(ItemTier tier)
        {
            return PickupCatalog.GetPickupDef(ArrayUtils.GetSafe(_scrapPickupByItemTier, (int)tier, PickupIndex.none));
        }
    }
}
