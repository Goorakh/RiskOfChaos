using HarmonyLib;
using HG;
using MonoMod.Cil;
using RiskOfChaos.Patches;
using RiskOfChaos.Trackers;
using RiskOfChaos.Utilities.Extensions;
using RoR2;
using RoR2.Items;
using System;
using System.Collections.Generic;
using System.Text;

namespace RiskOfChaos.EffectUtils.World
{
    public static class CustomContagiousItemManager
    {
        static readonly Dictionary<ItemIndex, ItemIndex> _customItemTransformations = [];
        static ItemDef.Pair[] _contaigousItemPairs = [];

        static CustomContagiousItemTokenModifier _tokenModifier;

        [SystemInitializer]
        static void Init()
        {
            Run.onRunDestroyGlobal += Run_onRunDestroyGlobal;
            IL.RoR2.Items.ContagiousItemManager.InitTransformationTable += ContagiousItemManager_InitTransformationTable_AddCustomTransformations;
        }

        public static void AddItemTransformation(ItemIndex from, ItemIndex to)
        {
            if (from == ItemIndex.None)
            {
                Log.Warning($"{nameof(from)} is not a valid item index");
                return;
            }

            if (to == ItemIndex.None)
            {
                Log.Warning($"{nameof(to)} is not a valid item index");
                return;
            }

            if (_customItemTransformations.TryGetValue(from, out ItemIndex currentTransformedItemIndex) && currentTransformedItemIndex == to)
                return;

            _customItemTransformations[from] = to;
            refreshContagiousItems();
        }

        public static void RemoveItemTransformation(ItemIndex from, ItemIndex to)
        {
            if (_customItemTransformations.Remove(from))
            {
                refreshContagiousItems();
            }
        }

        public static bool CanItemBeTransformedFrom(ItemIndex from)
        {
            return ContagiousItemManager.GetTransformedItemIndex(from) == ItemIndex.None;
        }

        public static bool CanItemBeTransformedInto(ItemIndex from, ItemIndex to)
        {
            return from != to && ContagiousItemManager.GetTransformedItemIndex(to) != from;
        }

        static void Run_onRunDestroyGlobal(Run obj)
        {
            _customItemTransformations.Clear();
            _customItemTransformations.TrimExcess();
            refreshContagiousItems();
        }

        static void refreshContagiousItems()
        {
            _contaigousItemPairs = new ItemDef.Pair[_customItemTransformations.Count];
            int itemPairIndex = 0;

            foreach (KeyValuePair<ItemIndex, ItemIndex> kvp in _customItemTransformations)
            {
                _contaigousItemPairs[itemPairIndex] = new ItemDef.Pair
                {
                    itemDef1 = ItemCatalog.GetItemDef(kvp.Key),
                    itemDef2 = ItemCatalog.GetItemDef(kvp.Value)
                };

                itemPairIndex++;
            }

            if (_contaigousItemPairs.Length > 0)
            {
                _tokenModifier ??= new CustomContagiousItemTokenModifier();
                _tokenModifier.SetItemPairs(_contaigousItemPairs);
            }
            else
            {
                if (_tokenModifier != null)
                {
                    _tokenModifier.Dispose();
                    _tokenModifier = null;
                }
            }

            ContagiousItemManager._transformationInfos = [];
            ContagiousItemManager.InitTransformationTable();

            foreach (InventoryTracker inventoryTracker in InstanceTracker.GetInstancesList<InventoryTracker>())
            {
                if (inventoryTracker.Inventory)
                {
                    ContagiousItemManager.OnInventoryChangedGlobal(inventoryTracker.Inventory);
                }
            }
        }

        static void ContagiousItemManager_InitTransformationTable_AddCustomTransformations(ILContext il)
        {
            ILCursor c = new ILCursor(il);

            if (c.TryGotoNext(MoveType.After,
                              x => x.MatchCallOrCallvirt(SymbolExtensions.GetMethodInfo(() => ItemCatalog.GetItemPairsForRelationship(default)))))
            {
                c.EmitDelegate(appendCustomItemPairs);
                static ReadOnlyArray<ItemDef.Pair> appendCustomItemPairs(ReadOnlyArray<ItemDef.Pair> itemPairs)
                {
                    if (_customItemTransformations.Count > 0)
                    {
                        itemPairs = new ReadOnlyArray<ItemDef.Pair>([.. itemPairs.src, .. _contaigousItemPairs]);
                    }

                    return itemPairs;
                }
            }
            else
            {
                Log.Error("Failed to find patch location");
            }
        }

        class CustomContagiousItemTokenModifier : IDisposable
        {
            readonly Dictionary<string, string> _cachedTokenSuffixes = [];

            ItemDef.Pair[] _itemPairs = [];

            public CustomContagiousItemTokenModifier()
            {
                LocalizedStringOverridePatch.OverrideLanguageString += LocalizedStringOverridePatch_OverrideLanguageString;
                Language.onCurrentLanguageChanged += Language_onCurrentLanguageChanged;
            }

            public void Dispose()
            {
                Language.onCurrentLanguageChanged -= Language_onCurrentLanguageChanged;
                LocalizedStringOverridePatch.OverrideLanguageString -= LocalizedStringOverridePatch_OverrideLanguageString;
            }

            public void SetItemPairs(ItemDef.Pair[] itemPairs)
            {
                _itemPairs = itemPairs;
                refreshSuffixCache();
            }

            void Language_onCurrentLanguageChanged()
            {
                refreshSuffixCache();
            }

            void refreshSuffixCache()
            {
                _cachedTokenSuffixes.Clear();

                Dictionary<ItemIndex, List<ItemIndex>> transformedToOriginalList = new Dictionary<ItemIndex, List<ItemIndex>>(_itemPairs.Length);
                foreach (ItemDef.Pair pair in _itemPairs)
                {
                    ItemDef original = pair.itemDef1;
                    ItemDef transformed = pair.itemDef2;

                    List<ItemIndex> originalItemsList = transformedToOriginalList.GetOrAddNew(transformed.itemIndex);
                    originalItemsList.Add(original.itemIndex);
                }

                StringBuilder itemNameListBuilder = HG.StringBuilderPool.RentStringBuilder();

                _cachedTokenSuffixes.EnsureCapacity(transformedToOriginalList.Count * 2);

                foreach (KeyValuePair<ItemIndex, List<ItemIndex>> kvp in transformedToOriginalList)
                {
                    ItemIndex transformedItemIndex = kvp.Key;
                    ItemDef transformedItem = ItemCatalog.GetItemDef(transformedItemIndex);

                    List<ItemIndex> originalItemIndices = kvp.Value;

                    itemNameListBuilder.Clear();
                    for (int i = 0; i < originalItemIndices.Count; i++)
                    {
                        ItemDef originalItem = ItemCatalog.GetItemDef(originalItemIndices[i]);

                        string itemNameToken = PickupCatalog.invalidPickupToken;
                        if (originalItem)
                        {
                            itemNameToken = originalItem.nameToken;
                        }

                        itemNameListBuilder.Append(' ');

                        itemNameListBuilder.Append(Language.GetStringFormatted("ITEM_CORRUPTION_FORMAT", Language.GetString(itemNameToken)));
                    }

                    string suffix = itemNameListBuilder.Take();

                    void tryAddSuffixTo(string token, string suffix)
                    {
                        if (string.IsNullOrEmpty(token) || Language.IsTokenInvalid(token))
                            return;

                        string str = Language.GetString(token);

                        string padding = string.Empty;
                        if (!str.EndsWith('.'))
                        {
                            padding = ".";
                        }

                        _cachedTokenSuffixes[token] = padding + suffix;
                    }

                    tryAddSuffixTo(transformedItem.pickupToken, suffix);
                    tryAddSuffixTo(transformedItem.descriptionToken, suffix);
                }

                _cachedTokenSuffixes.TrimExcess();

                itemNameListBuilder = HG.StringBuilderPool.ReturnStringBuilder(itemNameListBuilder);
            }

            void LocalizedStringOverridePatch_OverrideLanguageString(ref string str, string token, Language language)
            {
                if (_cachedTokenSuffixes.TryGetValue(token, out string suffix))
                {
                    str += suffix;
                }
            }
        }
    }
}
