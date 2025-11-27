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
    [ChaosEffect("unscrap_random_item", DefaultSelectionWeight = 0.5f)]
    public sealed class UnscrapRandomItem : NetworkBehaviour
    {
        [EffectConfig]
        static readonly ConfigHolder<int> _unscrapItemCount =
            ConfigFactory<int>.CreateConfig("Unscrap Count", 1)
                              .Description("How many items should be unscrapped per player")
                              .AcceptableValues(new AcceptableValueMin<int>(1))
                              .OptionConfig(new IntFieldConfig { Min = 1 })
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

        static PickupIndex[] _scrapPickupIndices = [];

        static Dictionary<ItemTier, ItemIndex[]> _printableItemsByTier = [];
        static Dictionary<ItemTier, ItemIndex[]> _scrapItemsByTier = [];

        [SystemInitializer(typeof(ItemCatalog), typeof(ItemTierCatalog), typeof(PickupCatalog))]
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

            using (SetPool<PickupIndex>.RentCollection(out HashSet<PickupIndex> scrapPickupIndices))
            {
                foreach (ItemIndex item in ItemCatalog.allItems)
                {
                    ItemDef itemDef = ItemCatalog.GetItemDef(item);
                    if (itemDef.ContainsTag(ItemTag.Scrap) || itemDef.ContainsTag(ItemTag.PriorityScrap))
                    {
                        scrapPickupIndices.Add(PickupCatalog.FindPickupIndex(item));
                    }
                }

                foreach (ItemTierDef itemTierDef in ItemTierCatalog.allItemTierDefs)
                {
                    PickupIndex scrapPickupIndex = PickupCatalog.FindScrapIndexForItemTier(itemTierDef.tier);
                    if (scrapPickupIndex != PickupIndex.none)
                    {
                        scrapPickupIndices.Add(scrapPickupIndex);
                    }
                }

                _scrapPickupIndices = [.. scrapPickupIndices];
            }
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
                return m && _scrapPickupIndices.Any(pickupIndex =>
                {
                    if (m.inventory.GetOwnedPickupCount(pickupIndex) <= 0)
                        return false;

                    PickupDef pickupDef = PickupCatalog.GetPickupDef(pickupIndex);
                    return _printableItemsByTier.TryGetValue(pickupDef.itemTier, out ItemIndex[] printableItems) && printableItems.Any(canUnscrapToItem);
                });
            });
        }

        ChaosEffectComponent _effectComponent;

        readonly record struct UnscrapInfo(ItemIndex ScrapItemIndex, ItemIndex[] PrintableItems);
        UnscrapInfo[] _unscrapOrder;

        Xoroshiro128Plus _rng;

        void Awake()
        {
            _effectComponent = GetComponent<ChaosEffectComponent>();
        }

        public override void OnStartServer()
        {
            base.OnStartServer();

            _rng = new Xoroshiro128Plus(_effectComponent.Rng.nextUlong);

            _unscrapOrder = _printableItemsByTier.SelectMany(kvp =>
            {
                if (_scrapItemsByTier.TryGetValue(kvp.Key, out ItemIndex[] scrapItems) && scrapItems.Length > 0)
                {
                    ItemIndex[] printableItems = [.. kvp.Value.Where(canUnscrapToItem)];
                    if (printableItems.Length > 0)
                    {
                        return scrapItems.Select(i => new UnscrapInfo(i, printableItems));
                    }
                }

                return [];
            }).ToArray();

            Util.ShuffleArray(_unscrapOrder, _rng.Branch());

            Log.Debug($"Unscrap order: [{string.Join(", ", _unscrapOrder.Select(u => $"({FormatUtils.GetBestItemDisplayName(u.ScrapItemIndex)})"))}]");
        }

        void Start()
        {
            if (NetworkServer.active)
            {
                PlayerUtils.GetAllPlayerMasters(false).TryDo(m =>
                {
                    tryUnscrapRandomItem(m, _rng.Branch());
                }, Util.GetBestMasterName);
            }
        }

        void tryUnscrapRandomItem(CharacterMaster master, Xoroshiro128Plus rng)
        {
            if (!master)
                return;

            Inventory inventory = master.inventory;
            if (!inventory)
                return;

            using (SetPool<PickupIndex>.RentCollection(out HashSet<PickupIndex> unscrappedItems))
            {
                unscrappedItems.EnsureCapacity(_unscrapItemCount.Value);

                for (int i = _unscrapItemCount.Value - 1; i >= 0; i--)
                {
                    Xoroshiro128Plus unscrapItemRng = rng.Branch();

                    bool unscrapSuccess = false;
                    foreach (UnscrapInfo unscrapInfo in _unscrapOrder)
                    {
                        ItemIndex unscrappedItemIndex = unscrapItemRng.NextElementUniform(unscrapInfo.PrintableItems);
                        ItemDef unscrappedItemDef = ItemCatalog.GetItemDef(unscrappedItemIndex);
                        if (!unscrappedItemDef)
                            continue;

                        InventoryExtensions.PickupTransformation scrapItemTransformation = new InventoryExtensions.PickupTransformation
                        {
                            AllowWhenDisabled = true,
                            MinToTransform = 1,
                            MaxToTransform = int.MaxValue,
                            OriginalPickupIndex = PickupCatalog.FindPickupIndex(unscrapInfo.ScrapItemIndex),
                            NewPickupIndex = PickupCatalog.FindScrapIndexForItemTier(unscrappedItemDef.tier),
                            TransformationType = (ItemTransformationTypeIndex)CharacterMasterNotificationQueue.TransformationType.Default,
                            ReplacementRule = InventoryExtensions.PickupReplacementRule.DropExisting
                        };

                        if (scrapItemTransformation.TryTransform(inventory, out InventoryExtensions.PickupTransformation.TryTransformResult result))
                        {
                            if (result.TakenPickup.PickupIndex == PickupCatalog.FindPickupIndex(DLC1Content.Items.RegeneratingScrap.itemIndex))
                            {
                                PickupIndex consumedScrapPickupIndex = PickupCatalog.FindPickupIndex(DLC1Content.Items.RegeneratingScrapConsumed.itemIndex);

                                InventoryExtensions.PickupGrantParameters consumedScrapGrantParameters = new InventoryExtensions.PickupGrantParameters
                                {
                                    PickupToGrant = result.TakenPickup.WithPickupIndex(consumedScrapPickupIndex),
                                    ReplacementRule = InventoryExtensions.PickupReplacementRule.DropExisting
                                };

                                if (consumedScrapGrantParameters.AttemptGrant(inventory))
                                {
                                    CharacterMasterNotificationQueue.SendTransformNotification(master, DLC1Content.Items.RegeneratingScrap.itemIndex, DLC1Content.Items.RegeneratingScrapConsumed.itemIndex, CharacterMasterNotificationQueue.TransformationType.Default);
                                }
                            }

                            unscrappedItems.Add(result.GivenPickup.PickupIndex);
                            unscrapSuccess = true;
                            break;
                        }
                    }

                    if (!unscrapSuccess)
                        break;
                }

                if (unscrappedItems.Count > 0)
                {
                    PickupUtils.QueuePickupsMessage(master, [.. unscrappedItems], PickupNotificationFlags.SendChatMessage);
                }
            }
        }
    }
}
