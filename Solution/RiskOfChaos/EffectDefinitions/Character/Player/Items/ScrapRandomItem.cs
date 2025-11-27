using HG;
using RiskOfChaos.Collections.ParsedValue;
using RiskOfChaos.ConfigHandling;
using RiskOfChaos.ConfigHandling.AcceptableValues;
using RiskOfChaos.EffectHandling;
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
using UnityEngine.Networking;

namespace RiskOfChaos.EffectDefinitions.Character.Player.Items
{
    [ChaosEffect("scrap_random_item", DefaultSelectionWeight = 0.5f)]
    public sealed class ScrapRandomItem : NetworkBehaviour
    {
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
                              .AcceptableValues(new AcceptableValueMin<int>(1))
                              .OptionConfig(new IntFieldConfig { Min = 1 })
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

                if (!PickupCatalog.FindScrapIndexForItemTier(itemDef.tier).isValid)
                {
                    Log.Warning($"{itemDef} ({itemTierDef}) should be scrappable, but no scrap item is defined");
                    continue;
                }

                if (_itemBlacklist.Contains(itemIndex))
                {
                    Log.Debug($"Not scrapping {itemDef}: Blacklist");
                    continue;
                }

                yield return itemIndex;
            }
        }

        static IEnumerable<ItemIndex> getAllScrappableItems(Inventory inventory)
        {
            if (!inventory)
                return [];

            return getAllScrappableItems().Where(i => inventory.GetOwnedItemCount(i) > 0);
        }

        [EffectCanActivate]
        static bool CanActivate(in EffectCanActivateContext context)
        {
            return !context.IsNow || PlayerUtils.GetAllPlayerMasters(false).Any(cm => getAllScrappableItems(cm.inventory).Any());
        }

        ChaosEffectComponent _effectComponent;

        ItemIndex[] _itemScrapOrder;

        void Awake()
        {
            _effectComponent = GetComponent<ChaosEffectComponent>();
        }

        public override void OnStartServer()
        {
            base.OnStartServer();

            Xoroshiro128Plus rng = new Xoroshiro128Plus(_effectComponent.Rng.nextUlong);

            _itemScrapOrder = [.. getAllScrappableItems()];
            Util.ShuffleArray(_itemScrapOrder, rng);

            Log.Debug($"Scrap order: [{string.Join(", ", _itemScrapOrder.Select(FormatUtils.GetBestItemDisplayName))}]");
        }

        void Start()
        {
            if (NetworkServer.active)
            {
                PlayerUtils.GetAllPlayerMasters(false).TryDo(tryScrapRandomItem, Util.GetBestMasterName);
            }
        }

        void tryScrapRandomItem(CharacterMaster characterMaster)
        {
            Inventory inventory = characterMaster.inventory;
            if (!inventory)
                return;

            using (SetPool<PickupIndex>.RentCollection(out HashSet<PickupIndex> scrapPickupIndicesGiven))
            {
                for (int i = _scrapCount.Value - 1; i >= 0; i--)
                {
                    bool scrapSuccess = false;
                    foreach (ItemIndex itemIndexToScrap in _itemScrapOrder)
                    {
                        ItemDef itemToScrap = ItemCatalog.GetItemDef(itemIndexToScrap);
                        if (!itemToScrap)
                            continue;

                        InventoryExtensions.PickupTransformation scrapItemTransformation = new InventoryExtensions.PickupTransformation
                        {
                            AllowWhenDisabled = true,
                            MinToTransform = 1,
                            MaxToTransform = _scrapWholeStack.Value ? int.MaxValue : 1,
                            OriginalPickupIndex = PickupCatalog.FindPickupIndex(itemIndexToScrap),
                            NewPickupIndex = PickupCatalog.FindScrapIndexForItemTier(itemToScrap.tier),
                            TransformationType = (ItemTransformationTypeIndex)CharacterMasterNotificationQueue.TransformationType.Default,
                            ReplacementRule = InventoryExtensions.PickupReplacementRule.DropExisting
                        };

                        if (scrapItemTransformation.TryTransform(inventory, out InventoryExtensions.PickupTransformation.TryTransformResult result))
                        {
                            scrapPickupIndicesGiven.Add(result.GivenPickup.PickupIndex);
                            scrapSuccess = true;
                            break;
                        }
                    }

                    if (!scrapSuccess)
                        break;
                }

                if (scrapPickupIndicesGiven.Count > 0)
                {
                    PickupUtils.QueuePickupsMessage(characterMaster, [.. scrapPickupIndicesGiven], PickupNotificationFlags.SendChatMessage);
                }
            }
        }
    }
}
