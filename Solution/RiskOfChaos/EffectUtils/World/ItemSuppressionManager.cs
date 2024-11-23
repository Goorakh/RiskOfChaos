using HG;
using RoR2;
using RoR2.Items;
using System.Collections.Generic;
using UnityEngine.Networking;

namespace RiskOfChaos.EffectUtils.World
{
    public static class ItemSuppressionManager
    {
        static HashSet<ItemIndex> _suppressedItems = [];

        [SystemInitializer(typeof(ItemCatalog), typeof(ItemTierCatalog))]
        static void Init()
        {
            HashSet<ItemDef> strangeScrapItems = new HashSet<ItemDef>(3);

            void fixScrapItem(ItemDef item, ItemTier itemTier)
            {
                if (!item)
                {
                    Log.Warning($"{nameof(item)} is null! Was DLC1Content not initialized?");
                    return;
                }

                item.tier = itemTier;

                if (item.DoesNotContainTag(ItemTag.Scrap))
                {
                    ArrayUtils.ArrayAppend(ref item.tags, ItemTag.Scrap);
                }

                strangeScrapItems.Add(item);
            }

            fixScrapItem(DLC1Content.Items.ScrapWhiteSuppressed, ItemTier.Tier1);
            fixScrapItem(DLC1Content.Items.ScrapGreenSuppressed, ItemTier.Tier2);
            fixScrapItem(DLC1Content.Items.ScrapRedSuppressed, ItemTier.Tier3);

            // Hide strange scrap from the logbook if RFTV isn't installed
            if (!BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey("com.Anreol.ReleasedFromTheVoid"))
            {
                On.RoR2.UI.LogBook.LogBookController.CanSelectItemEntry += (orig, itemDef, expansionAvailability) =>
                {
                    return orig(itemDef, expansionAvailability) && !strangeScrapItems.Contains(itemDef);
                };

                On.RoR2.GameCompletionStatsHelper.ctor += (orig, self) =>
                {
                    orig(self);

                    foreach (ItemDef item in strangeScrapItems)
                    {
                        PickupDef scrapPickupDef = PickupCatalog.GetPickupDef(PickupCatalog.FindPickupIndex(item.itemIndex));
                        if (scrapPickupDef != null)
                        {
                            self.encounterablePickups.Remove(scrapPickupDef);
                        }
                    }
                };
            }

            Run.onRunStartGlobal += Run_onRunStartGlobal;
        }

        static void Run_onRunStartGlobal(Run obj)
        {
            _suppressedItems = [];
        }

        public static ItemIndex GetSuppressedScrapItemIndex(ItemIndex suppressedItem)
        {
            if (suppressedItem == ItemIndex.None)
                return ItemIndex.None;

            ItemDef itemDef = ItemCatalog.GetItemDef(suppressedItem);
            if (!itemDef)
                return ItemIndex.None;

            return itemDef.tier switch
            {
                ItemTier.Tier1 => DLC1Content.Items.ScrapWhiteSuppressed.itemIndex,
                ItemTier.Tier2 => DLC1Content.Items.ScrapGreenSuppressed.itemIndex,
                ItemTier.Tier3 => DLC1Content.Items.ScrapRedSuppressed.itemIndex,
                _ => ItemIndex.None,
            };
        }

        public static bool CanSuppressItem(ItemIndex itemIndex)
        {
            return GetSuppressedScrapItemIndex(itemIndex) != ItemIndex.None && !SuppressedItemManager.HasItemBeenSuppressed(itemIndex);
        }

        public static bool SuppressItem(ItemIndex itemIndex)
        {
            return _suppressedItems.Add(itemIndex) && suppressItemInternal(itemIndex);
        }

        static bool suppressItemInternal(ItemIndex itemIndex)
        {
            if (!NetworkServer.active)
            {
                Log.Warning("Called on client");
                return false;
            }

            bool success = SuppressedItemManager.SuppressItem(itemIndex, GetSuppressedScrapItemIndex(itemIndex));

            if (success)
            {
                Log.Debug($"Suppressed item: {ItemCatalog.GetItemDef(itemIndex)}");
            }

            return success;
        }

        public static bool RemoveSuppressedItem(ItemIndex itemIndex)
        {
            if (!NetworkServer.active)
            {
                Log.Warning("Called on client");
                return false;
            }

            bool success = false;

            if (_suppressedItems.Remove(itemIndex))
            {
                if (SuppressedItemManager.transformationMap != null)
                {
                    success |= SuppressedItemManager.transformationMap.Remove(itemIndex);
                }

                if (SuppressedItemManager.suppressedInventory)
                {
                    SuppressedItemManager.suppressedInventory.RemoveItem(itemIndex);
                }
            }

            if (success)
            {
                Log.Debug($"Removed suppressed item: {ItemCatalog.GetItemDef(itemIndex)}");
            }

            return success;
        }
    }
}
