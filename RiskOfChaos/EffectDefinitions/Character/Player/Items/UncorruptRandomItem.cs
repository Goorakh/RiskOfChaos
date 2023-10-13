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
using RoR2.Items;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace RiskOfChaos.EffectDefinitions.Character.Player.Items
{
    [ChaosEffect("uncorrupt_random_item", DefaultSelectionWeight = 0.6f, EffectRepetitionWeightCalculationMode = EffectActivationCountMode.PerRun)]
    public sealed class UncorruptRandomItem : BaseEffect
    {
        [EffectConfig]
        static readonly ConfigHolder<int> _uncorruptItemCount =
            ConfigFactory<int>.CreateConfig("Uncorrupt Count", 1)
                              .Description("How many items should be uncorrupted per player")
                              .OptionConfig(new IntSliderConfig
                              {
                                  min = 1,
                                  max = 10
                              })
                              .ValueConstrictor(CommonValueConstrictors.GreaterThanOrEqualTo(1))
                              .Build();

        [EffectConfig]
        static readonly ConfigHolder<string> _itemBlacklistConfig =
            ConfigFactory<string>.CreateConfig("Item Blacklist", string.Empty)
                                 .Description("A comma-separated list of items that should not be allowed to be uncorrupted or uncorrupted to. Both internal and English display names are accepted, with spaces and commas removed.")
                                 .OptionConfig(new InputFieldConfig
                                 {
                                     submitOn = InputFieldConfig.SubmitEnum.OnSubmit
                                 })
                                 .Build();

        static readonly ParsedItemList _itemBlacklist = new ParsedItemList(ItemIndexComparer.Instance)
        {
            ConfigHolder = _itemBlacklistConfig
        };

        [EffectCanActivate]
        static bool CanActivate()
        {
            return getReverseItemCorruptionMap().Keys.Any(i => PlayerUtils.GetAllPlayerMasters(false).Any(m => m.inventory.GetItemCount(i) > 0));
        }

        static Dictionary<ItemIndex, List<ItemIndex>> getReverseItemCorruptionMap()
        {
            Dictionary<ItemIndex, List<ItemIndex>> reverseItemCorruptionMap = new Dictionary<ItemIndex, List<ItemIndex>>();

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
                    if (!run.IsItemAvailable(transformedItem) || !run.IsItemAvailable(originalItem))
                    {
                        continue;
                    }
                }

                if (!reverseItemCorruptionMap.TryGetValue(transformedItem, out List<ItemIndex> originalItems))
                {
                    originalItems = new List<ItemIndex>();
                    reverseItemCorruptionMap.Add(transformedItem, originalItems);
                }

                originalItems.Add(originalItem);
            }

            return reverseItemCorruptionMap;
        }

        public override void OnStart()
        {
            Dictionary<ItemIndex, List<ItemIndex>> reverseItemCorruptionMap = getReverseItemCorruptionMap();

            PlayerUtils.GetAllPlayerMasters(false).TryDo(master =>
            {
                uncorruptRandomItem(master, new Xoroshiro128Plus(RNG.nextUlong), reverseItemCorruptionMap);
            }, Util.GetBestMasterName);
        }

        static void uncorruptRandomItem(CharacterMaster master, Xoroshiro128Plus rng, Dictionary<ItemIndex, List<ItemIndex>> reverseItemCorruptionMap)
        {
            List<ItemIndex> availableTransformableItems = reverseItemCorruptionMap.Keys.Where(i => master.inventory.GetItemCount(i) > 0).ToList();
            if (availableTransformableItems.Count == 0)
                return;

            int numItemsToUncorrupt = Mathf.Min(availableTransformableItems.Count, _uncorruptItemCount.Value);
            for (int i = 0; i < numItemsToUncorrupt; i++)
            {
                ItemIndex itemToTransform = availableTransformableItems.GetAndRemoveRandom(rng);
                uncorruptItem(master, rng, itemToTransform, reverseItemCorruptionMap[itemToTransform]);
            }
        }

        static void uncorruptItem(CharacterMaster master, Xoroshiro128Plus rng, ItemIndex corruptItemIndex, List<ItemIndex> uncorruptItems)
        {
            Inventory inventory = master.inventory;
            if (!inventory)
                return;

            int corruptItemCount = inventory.GetItemCount(corruptItemIndex);

            inventory.RemoveItem(corruptItemIndex, corruptItemCount);

            int[] newItemCounts = new int[uncorruptItems.Count];

            while (corruptItemCount > 0)
            {
                int uncorruptItemIndex = rng.RangeInt(0, uncorruptItems.Count);

                newItemCounts[uncorruptItemIndex]++;
                corruptItemCount--;
            }

            for (int i = 0; i < newItemCounts.Length; i++)
            {
                int uncorruptItemCount = newItemCounts[i];
                if (uncorruptItemCount <= 0)
                    continue;

                inventory.GiveItem(uncorruptItems[i], uncorruptItemCount);

                if (master.playerCharacterMasterController)
                {
                    CharacterMasterNotificationQueue.SendTransformNotification(master, corruptItemIndex, uncorruptItems[i], CharacterMasterNotificationQueue.TransformationType.ContagiousVoid);
                }
            }
        }
    }
}
