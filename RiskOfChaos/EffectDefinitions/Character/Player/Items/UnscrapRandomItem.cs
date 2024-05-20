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
using System;
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
                              .AcceptableValues(new AcceptableValueMin<int>(1))
                              .OptionConfig(new IntFieldConfig { Min = 1})
                              .Build();

        [EffectConfig]
        static readonly ConfigHolder<string> _itemBlacklistConfig =
            ConfigFactory<string>.CreateConfig("Item Blacklist", string.Empty)
                                 .Description("A comma-separated list of items that should not be allowed to be unscrapped to, or any item scrap that should not be allowed to be unscrapped. Both internal and English display names are accepted, with spaces and commas removed.")
                                 .OptionConfig(new InputFieldConfig
                                 {
                                     lineType = TMPro.TMP_InputField.LineType.SingleLine,
                                     submitOn = InputFieldConfig.SubmitEnum.OnExitOrSubmit
                                 })
                                 .Build();

        static readonly ParsedItemList _itemBlacklist = new ParsedItemList(ItemIndexComparer.Instance)
        {
            ConfigHolder = _itemBlacklistConfig
        };

        static Dictionary<ItemTier, ItemIndex[]> _printableItemsByTier = [];
        static Dictionary<ItemTier, ItemIndex[]> _scrapItemsByTier = [];

        [SystemInitializer(typeof(ItemCatalog), typeof(ItemTierCatalog))]
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

            _scrapItemsByTier = ItemCatalog.allItemDefs.Where(i => !i.hidden && (i.ContainsTag(ItemTag.Scrap) || i.ContainsTag(ItemTag.PriorityScrap)))
                                                       .GroupBy(i => i.tier)
                                                       .ToDictionary(g => g.Key,
                                                                     g => g.Select(g => g.itemIndex).ToArray());
        }

        static IEnumerable<ItemDef> getAllScrapItems()
        {
            foreach (ItemIndex item in ItemCatalog.allItems)
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

        static IEnumerable<ItemDef> getAllScrapItems(Inventory inventory)
        {
            if (!inventory)
                return Enumerable.Empty<ItemDef>();

            return getAllScrapItems().Where(i => inventory.GetItemCount(i) > 0);
        }

        static bool canUnscrapToItem(ItemIndex item)
        {
            Run run = Run.instance;
            if (run && !run.IsItemEnabled(item))
                return false;

            if (_itemBlacklist.Contains(item))
                return false;

            return true;
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

        readonly record struct UnscrapInfo(ItemIndex ScrapItemIndex, ItemIndex[] PrintableItems);
        UnscrapInfo[] _unscrapOrder;

        public override void OnPreStartServer()
        {
            base.OnPreStartServer();

            _unscrapOrder = _printableItemsByTier.SelectMany(kvp =>
            {
                if (_scrapItemsByTier.TryGetValue(kvp.Key, out ItemIndex[] scrapItems) && scrapItems.Length > 0)
                {
                    ItemIndex[] printableItems = kvp.Value.Where(canUnscrapToItem).ToArray();
                    if (printableItems.Length > 0)
                    {
                        return scrapItems.Select(i => new UnscrapInfo(i, printableItems));
                    }
                }

                return Enumerable.Empty<UnscrapInfo>();
            }).ToArray();

            Util.ShuffleArray(_unscrapOrder, RNG.Branch());

#if DEBUG
            Log.Debug($"Unscrap order: [{string.Join(", ", _unscrapOrder.Select(u => $"({FormatUtils.GetBestItemDisplayName(u.ScrapItemIndex)})"))}]");
#endif
        }

        public override void OnStart()
        {
            PlayerUtils.GetAllPlayerMasters(false).TryDo(m =>
            {
                tryUnscrapRandomItem(m, RNG.Branch());
            }, Util.GetBestMasterName);
        }

        void tryUnscrapRandomItem(CharacterMaster master, Xoroshiro128Plus rng)
        {
            if (!master)
                return;

            Inventory inventory = master.inventory;
            if (!inventory)
                return;

            for (int i = _unscrapItemCount.Value - 1; i >= 0; i--)
            {
                int unscrapInfoIndex = Array.FindIndex(_unscrapOrder, u => inventory.GetItemCount(u.ScrapItemIndex) > 0);
                if (unscrapInfoIndex == -1) // This inventory has no more items to unscrap
                    break;

                UnscrapInfo unscrapInfo = _unscrapOrder[unscrapInfoIndex];

                int scrapCount = inventory.GetItemCount(unscrapInfo.ScrapItemIndex);
                inventory.RemoveItem(unscrapInfo.ScrapItemIndex, scrapCount);

                ItemIndex newItem = rng.NextElementUniform(unscrapInfo.PrintableItems);
                inventory.GiveItem(newItem, scrapCount);

                CharacterMasterNotificationQueue.SendTransformNotification(master, unscrapInfo.ScrapItemIndex, newItem, CharacterMasterNotificationQueue.TransformationType.Default);

                if (unscrapInfo.ScrapItemIndex == DLC1Content.Items.RegeneratingScrap.itemIndex)
                {
                    inventory.GiveItem(DLC1Content.Items.RegeneratingScrapConsumed, scrapCount);
                    CharacterMasterNotificationQueue.SendTransformNotification(master, DLC1Content.Items.RegeneratingScrap.itemIndex, DLC1Content.Items.RegeneratingScrapConsumed.itemIndex, CharacterMasterNotificationQueue.TransformationType.Default);
                }
            }
        }
    }
}
