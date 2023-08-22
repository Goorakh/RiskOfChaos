using RiskOfChaos.EffectHandling;
using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Methods;
using RiskOfChaos.Utilities;
using RiskOfChaos.Utilities.Extensions;
using RoR2;
using RoR2.Items;
using System.Collections.Generic;
using System.Linq;

namespace RiskOfChaos.EffectDefinitions.Character.Player.Items
{
    [ChaosEffect("uncorrupt_random_item", DefaultSelectionWeight = 0.6f, EffectRepetitionWeightCalculationMode = EffectActivationCountMode.PerRun)]
    public sealed class UncorruptRandomItem : BaseEffect
    {
        [EffectCanActivate]
        static bool CanActivate(in EffectCanActivateContext context)
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

                Run run = Run.instance;
                if (run)
                {
                    if (!run.IsItemAvailable(transformedItem) || run.IsItemExpansionLocked(transformedItem) ||
                        !run.IsItemAvailable(originalItem) || run.IsItemExpansionLocked(originalItem))
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

            ItemIndex itemToTransform = rng.NextElementUniform(availableTransformableItems);
            uncorruptItem(master, rng, itemToTransform, reverseItemCorruptionMap[itemToTransform]);
        }

        static void uncorruptItem(CharacterMaster master, Xoroshiro128Plus rng, ItemIndex corruptItemIndex, List<ItemIndex> uncorruptItems)
        {
            Inventory inventory = master.inventory;

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
