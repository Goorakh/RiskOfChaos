using RiskOfChaos.Collections.ParsedValue;
using RiskOfChaos.ConfigHandling;
using RiskOfChaos.ConfigHandling.AcceptableValues;
using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Data;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Methods;
using RiskOfChaos.Utilities;
using RiskOfChaos.Utilities.Comparers;
using RiskOfChaos.Utilities.Extensions;
using RiskOfOptions.OptionConfigs;
using RoR2;
using RoR2.Items;
using System;
using System.Collections.Generic;
using System.Linq;

namespace RiskOfChaos.EffectDefinitions.Character.Player.Items
{
    [ChaosEffect("uncorrupt_random_item", DefaultSelectionWeight = 0.6f)]
    public sealed class UncorruptRandomItem : BaseEffect
    {
        [EffectConfig]
        static readonly ConfigHolder<int> _uncorruptItemCount =
            ConfigFactory<int>.CreateConfig("Uncorrupt Count", 1)
                              .Description("How many items should be uncorrupted per player")
                              .AcceptableValues(new AcceptableValueMin<int>(1))
                              .OptionConfig(new IntSliderConfig
                              {
                                  min = 1,
                                  max = 10
                              })
                              .Build();

        [EffectConfig]
        static readonly ConfigHolder<string> _itemBlacklistConfig =
            ConfigFactory<string>.CreateConfig("Item Blacklist", string.Empty)
                                 .Description("A comma-separated list of items that should not be allowed to be uncorrupted or uncorrupted to. Both internal and English display names are accepted, with spaces and commas removed.")
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

        [EffectCanActivate]
        static bool CanActivate()
        {
            return getReverseItemCorruptionMap().Keys.Any(i => PlayerUtils.GetAllPlayerMasters(false).Any(m => m.inventory && m.inventory.GetItemCount(i) > 0));
        }

        static Dictionary<ItemIndex, List<ItemIndex>> getReverseItemCorruptionMap()
        {
            Dictionary<ItemIndex, List<ItemIndex>> reverseItemCorruptionMap = [];

            foreach (ContagiousItemManager.TransformationInfo transformationInfo in ContagiousItemManager.transformationInfos)
            {
                ItemIndex transformedItem = transformationInfo.transformedItem;
                ItemIndex originalItem = transformationInfo.originalItem;

                if (_itemBlacklist.Contains(originalItem) || _itemBlacklist.Contains(transformedItem))
                {
#if DEBUG
                    Log.Debug($"Excluding transform {ItemCatalog.GetItemDef(originalItem)} -> {ItemCatalog.GetItemDef(transformedItem)}: Blacklist");
#endif

                    continue;
                }

                Run run = Run.instance;
                if (run)
                {
                    if (!run.IsItemEnabled(transformedItem) || !run.IsItemEnabled(originalItem))
                    {
                        continue;
                    }
                }

                if (!reverseItemCorruptionMap.TryGetValue(transformedItem, out List<ItemIndex> originalItems))
                {
                    originalItems = [];
                    reverseItemCorruptionMap.Add(transformedItem, originalItems);
                }

                originalItems.Add(originalItem);
            }

            return reverseItemCorruptionMap;
        }

        readonly record struct ItemUncorruptionInfo(ItemIndex CorruptedItem, ItemIndex[] UncorruptedItemOptions);
        ItemUncorruptionInfo[] _itemUncorruptionOrder;

        public override void OnPreStartServer()
        {
            base.OnPreStartServer();

            _itemUncorruptionOrder = getReverseItemCorruptionMap().Select(kvp => new ItemUncorruptionInfo(kvp.Key, kvp.Value.ToArray())).ToArray();
            Util.ShuffleArray(_itemUncorruptionOrder, RNG.Branch());

#if DEBUG
            Log.Debug($"Uncorruption order: [{string.Join(", ", _itemUncorruptionOrder.Select(u => FormatUtils.GetBestItemDisplayName(u.CorruptedItem)))}]");
#endif
        }

        public override void OnStart()
        {
            PlayerUtils.GetAllPlayerMasters(false).TryDo(master =>
            {
                uncorruptRandomItem(master, RNG.Branch());
            }, Util.GetBestMasterName);
        }

        void uncorruptRandomItem(CharacterMaster master, Xoroshiro128Plus rng)
        {
            Inventory inventory = master.inventory;
            if (!inventory)
                return;

            for (int i = _uncorruptItemCount.Value - 1; i >= 0; i--)
            {
                int itemUncorruptIndex = Array.FindIndex(_itemUncorruptionOrder, u => inventory.GetItemCount(u.CorruptedItem) > 0);
                if (itemUncorruptIndex == -1)
                    break;

                uncorruptItem(master, rng.Branch(), _itemUncorruptionOrder[itemUncorruptIndex]);
            }
        }

        static void uncorruptItem(CharacterMaster master, Xoroshiro128Plus rng, ItemUncorruptionInfo uncorruptionInfo)
        {
            Inventory inventory = master.inventory;
            if (!inventory)
                return;

            int corruptItemCount = inventory.GetItemCount(uncorruptionInfo.CorruptedItem);

            inventory.RemoveItem(uncorruptionInfo.CorruptedItem, corruptItemCount);

            int[] newItemCounts = new int[uncorruptionInfo.UncorruptedItemOptions.Length];

            while (corruptItemCount > 0)
            {
                int uncorruptItemIndex = rng.RangeInt(0, uncorruptionInfo.UncorruptedItemOptions.Length);

                newItemCounts[uncorruptItemIndex]++;
                corruptItemCount--;
            }

            for (int i = 0; i < newItemCounts.Length; i++)
            {
                int uncorruptItemCount = newItemCounts[i];
                if (uncorruptItemCount <= 0)
                    continue;

                inventory.GiveItem(uncorruptionInfo.UncorruptedItemOptions[i], uncorruptItemCount);

                if (master.playerCharacterMasterController)
                {
                    CharacterMasterNotificationQueue.SendTransformNotification(master, uncorruptionInfo.CorruptedItem, uncorruptionInfo.UncorruptedItemOptions[i], CharacterMasterNotificationQueue.TransformationType.ContagiousVoid);
                }
            }
        }
    }
}
