using RiskOfChaos.EffectHandling;
using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Methods;
using RiskOfChaos.Utilities;
using RoR2;
using System.Collections.Generic;
using System.Linq;

namespace RiskOfChaos.EffectDefinitions.Character.Player.Items
{
    [ChaosEffect("duplicate_random_item_stack", EffectWeightReductionPercentagePerActivation = 0f)]
    public sealed class DuplicateRandomItemStack : BaseEffect
    {
        readonly struct ItemStack
        {
            public readonly ItemIndex ItemIndex;
            public readonly int ItemCount;

            public ItemStack(ItemIndex itemIndex, int itemCount)
            {
                ItemIndex = itemIndex;
                ItemCount = itemCount;
            }
        }

        [EffectCanActivate]
        static bool CanActivate(EffectCanActivateContext context)
        {
            return !context.IsNow || PlayerUtils.GetAllPlayerMasters(false).Any(master => getAllDuplicatableItemStacks(master.inventory).Any());
        }

        static IEnumerable<ItemStack> getAllDuplicatableItemStacks(Inventory inventory)
        {
            if (!inventory)
                yield break;

            foreach (ItemDef item in ItemCatalog.allItemDefs)
            {
                if (!item || item.hidden)
                    continue;

                int itemCount = inventory.GetItemCount(item);
                if (itemCount <= 0)
                    continue;

                yield return new ItemStack(item.itemIndex, itemCount);
            }
        }

        public override void OnStart()
        {
            foreach (CharacterMaster master in PlayerUtils.GetAllPlayerMasters(false))
            {
                Inventory inventory = master.inventory;
                if (!inventory)
                    continue;

                ItemStack[] duplicatableItemStacks = getAllDuplicatableItemStacks(inventory).ToArray();
                if (duplicatableItemStacks.Length <= 0)
                    continue;

                ItemStack itemStack = RNG.NextElementUniform(duplicatableItemStacks);
                inventory.GiveItem(itemStack.ItemIndex, itemStack.ItemCount);

                GenericPickupController.SendPickupMessage(master, PickupCatalog.FindPickupIndex(itemStack.ItemIndex));
            }
        }
    }
}
