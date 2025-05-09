﻿using RiskOfChaos.Collections.ParsedValue;
using RiskOfChaos.ConfigHandling;
using RiskOfChaos.ConfigHandling.AcceptableValues;
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
using RoR2.Items;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Networking;

namespace RiskOfChaos.EffectDefinitions.Character.Player.Items
{
    [ChaosEffect("uncorrupt_random_item", DefaultSelectionWeight = 0.6f)]
    public sealed class UncorruptRandomItem : NetworkBehaviour
    {
        [EffectConfig]
        static readonly ConfigHolder<int> _uncorruptItemCount =
            ConfigFactory<int>.CreateConfig("Uncorrupt Count", 1)
                              .Description("How many items should be uncorrupted per player")
                              .AcceptableValues(new AcceptableValueMin<int>(1))
                              .OptionConfig(new IntFieldConfig { Min = 1})
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
                    Log.Debug($"Excluding transform {ItemCatalog.GetItemDef(originalItem)} -> {ItemCatalog.GetItemDef(transformedItem)}: Blacklist");

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

                List<ItemIndex> originalItems = reverseItemCorruptionMap.GetOrAddNew(transformedItem);
                originalItems.Add(originalItem);
            }

            return reverseItemCorruptionMap;
        }

        readonly record struct ItemUncorruptionInfo(ItemIndex CorruptedItem, ItemIndex[] UncorruptedItemOptions);
        ItemUncorruptionInfo[] _itemUncorruptionOrder;

        ChaosEffectComponent _effectComponent;

        Xoroshiro128Plus _rng;

        void Awake()
        {
            _effectComponent = GetComponent<ChaosEffectComponent>();
        }

        public override void OnStartServer()
        {
            base.OnStartServer();

            _rng = new Xoroshiro128Plus(_effectComponent.Rng.nextUlong);

            _itemUncorruptionOrder = getReverseItemCorruptionMap().Select(kvp => new ItemUncorruptionInfo(kvp.Key, kvp.Value.ToArray())).ToArray();
            Util.ShuffleArray(_itemUncorruptionOrder, _rng.Branch());

            Log.Debug($"Uncorruption order: [{string.Join(", ", _itemUncorruptionOrder.Select(u => FormatUtils.GetBestItemDisplayName(u.CorruptedItem)))}]");
        }

        void Start()
        {
            if (NetworkServer.active)
            {
                PlayerUtils.GetAllPlayerMasters(false).TryDo(master =>
                {
                    uncorruptRandomItem(master, _rng.Branch());
                }, Util.GetBestMasterName);
            }
        }

        void uncorruptRandomItem(CharacterMaster master, Xoroshiro128Plus rng)
        {
            Inventory inventory = master.inventory;
            if (!inventory)
                return;

            HashSet<PickupIndex> uncorruptedItems = new HashSet<PickupIndex>(_uncorruptItemCount.Value);

            for (int i = _uncorruptItemCount.Value - 1; i >= 0; i--)
            {
                int itemUncorruptIndex = Array.FindIndex(_itemUncorruptionOrder, u => inventory.GetItemCount(u.CorruptedItem) > 0);
                if (itemUncorruptIndex == -1)
                    break;

                uncorruptItem(master, rng.Branch(), _itemUncorruptionOrder[itemUncorruptIndex], uncorruptedItems);
            }

            if (uncorruptedItems.Count > 0)
            {
                PickupUtils.QueuePickupsMessage(master, [.. uncorruptedItems], PickupNotificationFlags.SendChatMessage);
            }
        }

        static void uncorruptItem(CharacterMaster master, Xoroshiro128Plus rng, ItemUncorruptionInfo uncorruptionInfo, HashSet<PickupIndex> uncorruptedItems)
        {
            Inventory inventory = master.inventory;
            if (!inventory)
                return;

            int corruptItemCount = inventory.GetItemCount(uncorruptionInfo.CorruptedItem);

            uncorruptedItems?.EnsureCapacity(uncorruptedItems.Count + corruptItemCount);

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

                ItemIndex uncorruptItemIndex = uncorruptionInfo.UncorruptedItemOptions[i];
                inventory.GiveItem(uncorruptItemIndex, uncorruptItemCount);

                if (master.playerCharacterMasterController)
                {
                    CharacterMasterNotificationQueue.SendTransformNotification(master, uncorruptionInfo.CorruptedItem, uncorruptItemIndex, CharacterMasterNotificationQueue.TransformationType.ContagiousVoid);
                }

                uncorruptedItems?.Add(PickupCatalog.FindPickupIndex(uncorruptItemIndex));
            }
        }
    }
}
