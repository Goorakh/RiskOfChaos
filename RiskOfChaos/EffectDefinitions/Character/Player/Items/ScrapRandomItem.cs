using HG;
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
using System;
using System.Collections.Generic;
using System.Linq;

namespace RiskOfChaos.EffectDefinitions.Character.Player.Items
{
    [ChaosEffect("scrap_random_item", DefaultSelectionWeight = 0.8f, EffectWeightReductionPercentagePerActivation = 15f)]
    public sealed class ScrapRandomItem : BaseEffect
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

        [EffectConfig]
        static readonly ConfigHolder<bool> _scrapWholeStack =
            ConfigFactory<bool>.CreateConfig("Scrap Whole Stack", true)
                               .Description("If the effect should scrap all items of the selected stack. If this option is disabled, only one item will be turned into scrap, and if it's enabled, it's as if you used a scrapper on that item.")
                               .OptionConfig(new CheckBoxConfig())
                               .Build();

        [EffectConfig]
        static readonly ConfigHolder<int> _scrapCount =
            ConfigFactory<int>.CreateConfig("Scrap Count", 1)
                              .Description("How many items/stacks should be scrapped per player")
                              .OptionConfig(new IntSliderConfig
                              {
                                  min = 1,
                                  max = 10
                              })
                              .ValueConstrictor(CommonValueConstrictors.GreaterThanOrEqualTo(1))
                              .Build();

        [EffectConfig]
        static readonly ConfigHolder<string> _itemBlacklistConfig =
            ConfigFactory<string>.CreateConfig("Scrap Blacklist", string.Empty)
                                 .Description("A comma-separated list of items that should not be allowed to be scrapped. Both internal and English display names are accepted, with spaces and commas removed.")
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

        static IEnumerable<ItemIndex> getAllScrappableItems()
        {
            foreach (ItemIndex itemIndex in ItemCatalog.allItems)
            {
                ItemDef itemDef = ItemCatalog.GetItemDef(itemIndex);
                if (!itemDef || itemDef.hidden || !itemDef.canRemove || itemDef.ContainsTag(ItemTag.Scrap))
                    continue;

                ItemTierDef itemTierDef = ItemTierCatalog.GetItemTierDef(itemDef.tier);
                if (!itemTierDef || !itemTierDef.canScrap)
                    continue;

                if (getScrapPickupForItemTier(itemDef.tier) == null)
                {
                    Log.Warning($"{itemDef} ({itemTierDef}) should be scrappable, but no scrap item is defined");
                    continue;
                }

                if (_itemBlacklist.Contains(itemIndex))
                {
#if DEBUG
                    Log.Debug($"Not scrapping {itemDef}: Blacklist");
#endif
                    continue;
                }

                yield return itemIndex;
            }
        }

        static IEnumerable<ItemIndex> getAllScrappableItems(Inventory inventory)
        {
            if (!inventory)
                return Enumerable.Empty<ItemIndex>();

            return getAllScrappableItems().Where(i => inventory.GetItemCount(i) > 0);
        }

        [EffectCanActivate]
        static bool CanActivate(in EffectCanActivateContext context)
        {
            return _scrapPickupByItemTier != null && (!context.IsNow || PlayerUtils.GetAllPlayerMasters(false).Any(cm => getAllScrappableItems(cm.inventory).Any()));
        }

        ItemIndex[] _itemScrapOrder;

        public override void OnPreStartServer()
        {
            base.OnPreStartServer();

            _itemScrapOrder = getAllScrappableItems().ToArray();
            Util.ShuffleArray(_itemScrapOrder, RNG.Branch());

#if DEBUG
            Log.Debug($"Scrap order: [{string.Join(", ", _itemScrapOrder.Select(FormatUtils.GetBestItemDisplayName))}]");
#endif
        }

        public override void OnStart()
        {
            PlayerUtils.GetAllPlayerMasters(false).TryDo(tryScrapRandomItem, Util.GetBestMasterName);
        }

        void tryScrapRandomItem(CharacterMaster characterMaster)
        {
            Inventory inventory = characterMaster.inventory;
            if (!inventory)
                return;

            HashSet<ItemIndex> notifiedScrapItems = new HashSet<ItemIndex>();

            for (int i = _scrapCount.Value - 1; i >= 0; i--)
            {
                int itemToScrapIndex = Array.FindIndex(_itemScrapOrder, i => inventory.GetItemCount(i) > 0);
                if (itemToScrapIndex == -1) // No more items to scrap, inventory is out of scrappable items
                    break;

                ItemDef itemToScrap = ItemCatalog.GetItemDef(_itemScrapOrder[itemToScrapIndex]);
                scrapItem(characterMaster, inventory, itemToScrap, notifiedScrapItems);
            }

            RoR2Application.onNextUpdate += () =>
            {
                foreach (ItemIndex scrapItemIndex in notifiedScrapItems)
                {
                    PickupDef scrapPickup = PickupCatalog.GetPickupDef(PickupCatalog.FindPickupIndex(scrapItemIndex));
                    if (scrapPickup == null)
                        continue;

                    Chat.AddPickupMessage(characterMaster.GetBody(), scrapPickup.nameToken, scrapPickup.baseColor, (uint)inventory.GetItemCount(scrapItemIndex));
                }
            };
        }

        static void scrapItem(CharacterMaster characterMaster, Inventory inventory, ItemDef itemToScrap, HashSet<ItemIndex> notifiedScrapItems)
        {
            PickupDef scrapPickup = getScrapPickupForItemTier(itemToScrap.tier);
            if (scrapPickup == null)
                return;

            int itemCount;
            if (_scrapWholeStack.Value)
            {
                itemCount = inventory.GetItemCount(itemToScrap);
            }
            else
            {
                itemCount = 1;
            }

            inventory.RemoveItem(itemToScrap, itemCount);
            inventory.GiveItem(scrapPickup.itemIndex, itemCount);

            CharacterMasterNotificationQueue.SendTransformNotification(characterMaster, itemToScrap.itemIndex, scrapPickup.itemIndex, CharacterMasterNotificationQueue.TransformationType.Default);

            notifiedScrapItems?.Add(scrapPickup.itemIndex);
        }

        static PickupDef getScrapPickupForItemTier(ItemTier tier)
        {
            return PickupCatalog.GetPickupDef(ArrayUtils.GetSafe(_scrapPickupByItemTier, (int)tier, PickupIndex.none));
        }
    }
}
