using HG;
using RiskOfChaos.Components;
using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Methods;
using RiskOfChaos.SaveHandling;
using RiskOfChaos.SaveHandling.DataContainers;
using RiskOfChaos.SaveHandling.DataContainers.Effects;
using RiskOfChaos.Utilities;
using RiskOfChaos.Utilities.Extensions;
using RoR2;
using RoR2.Items;
using System.Collections.Generic;
using System.Linq;

namespace RiskOfChaos.EffectDefinitions.World
{
    [ChaosEffect(EFFECT_IDENTIFIER)]
    public sealed class SuppressRandomItem : BaseEffect
    {
        public const string EFFECT_IDENTIFIER = "suppress_random_item";

        [SystemInitializer(typeof(ItemCatalog), typeof(ItemTierCatalog))]
        static void FixStrangeScrap()
        {
            HashSet<ItemDef> strangeScrapItems = [];

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
        }

        [SystemInitializer]
        static void Init()
        {
            if (SaveManager.UseSaveData)
            {
                SaveManager.CollectSaveData += SaveManager_CollectSaveData;
                SaveManager.LoadSaveData += SaveManager_LoadSaveData;
            }

            Run.onRunStartGlobal += _ =>
            {
                _suppressedItems.Clear();
            };

            Run.onRunDestroyGlobal += _ =>
            {
                _suppressedItems.Clear();
            };
        }

        static List<ItemDef> _suppressedItems = [];

        static void SaveManager_CollectSaveData(ref SaveContainer container)
        {
            if (container.Effects is null)
                return;

            container.Effects.SuppressRandomItem_Data = new SuppressRandomItem_Data
            {
                SuppressedItems = _suppressedItems.Select(i => i.name).ToArray()
            };
        }

        static void SaveManager_LoadSaveData(in SaveContainer container)
        {
            SuppressRandomItem_Data data = container.Effects?.SuppressRandomItem_Data;
            if (data is null)
            {
                _suppressedItems.Clear();
            }
            else
            {
                _suppressedItems = data.SuppressedItems.Select(ItemCatalog.FindItemIndex)
                                                       .Select(ItemCatalog.GetItemDef)
                                                       .Where(i => i)
                                                       .ToList();

                foreach (ItemDef item in _suppressedItems)
                {
                    SuppressedItemManager.SuppressItem(item.itemIndex, getTransformedItemIndex(item.itemIndex));
                }
            }
        }

        [EffectCanActivate]
        static bool CanActivate()
        {
            return ExpansionUtils.DLC1Enabled && getAllSuppressableItems().Any();
        }

        static IEnumerable<ItemIndex> getAllSuppressableItems()
        {
            if (!Run.instance || Run.instance.availableItems == null)
                return Enumerable.Empty<ItemIndex>();

            return ItemCatalog.allItems.Where(i => getTransformedItemIndex(i) != ItemIndex.None && Run.instance.availableItems.Contains(i));
        }

        static ItemIndex getTransformedItemIndex(ItemIndex suppressedItem)
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

        public override void OnStart()
        {
            List<ItemIndex> availableItems = getAllSuppressableItems().ToList();

            ItemIndex suppressedItemIndex;
            ItemIndex transformedItemIndex;
            do
            {
                if (availableItems.Count == 0)
                {
                    Log.Error("No suppressable items found");
                    return;
                }

                suppressedItemIndex = availableItems.GetAndRemoveRandom(RNG);
                transformedItemIndex = getTransformedItemIndex(suppressedItemIndex);
            } while (transformedItemIndex == ItemIndex.None || !SuppressedItemManager.SuppressItem(suppressedItemIndex, transformedItemIndex));

            ItemDef suppressedItem = ItemCatalog.GetItemDef(suppressedItemIndex);
            _suppressedItems.Add(suppressedItem);

            ItemTierDef itemTierDef = ItemTierCatalog.GetItemTierDef(suppressedItem.tier);
            Chat.SendBroadcastChat(new ColoredTokenChatMessage
            {
                subjectAsCharacterBody = ChaosInteractor.GetBody(),
                baseToken = "VOID_SUPPRESSOR_USE_MESSAGE",
                paramTokens = [ suppressedItem.nameToken ],
                paramColors = [ ColorCatalog.GetColor(itemTierDef.colorIndex) ]
            });
        }
    }
}
