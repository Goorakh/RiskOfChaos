using RiskOfChaos.EffectHandling;
using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Methods;
using RiskOfChaos.Utilities;
using RoR2;
using RoR2.Items;
using System.Collections.Generic;
using System.Linq;

namespace RiskOfChaos.EffectDefinitions.Character.Player.Items
{
    [ChaosEffect("corrupt_random_item", DefaultSelectionWeight = 0.6f, EffectRepetitionWeightCalculationMode = EffectActivationCountMode.PerRun)]
    public sealed class CorruptRandomItem : BaseEffect
    {
        [EffectCanActivate]
        static bool CanActivate(EffectCanActivateContext context)
        {
            return (!context.IsNow && ExpansionUtils.DLC1Enabled) || PlayerUtils.GetAllPlayerMasters(false).Any(m => getAllCorruptableItems(m.inventory).Any());
        }

        static IEnumerable<ItemIndex> getAllCorruptableItems(Inventory inventory)
        {
            Run run = Run.instance;
            if (!run || !inventory)
                yield break;

            foreach (ItemIndex item in inventory.itemAcquisitionOrder)
            {
                if (inventory.GetItemCount(item) <= 0)
                    continue;

                ItemIndex transformedItem = ContagiousItemManager.GetTransformedItemIndex(item);
                if (transformedItem == ItemIndex.None || !run.IsItemAvailable(transformedItem) || run.IsItemExpansionLocked(transformedItem))
                    continue;
                
                yield return item;
            }
        }

        public override void OnStart()
        {
            foreach (CharacterMaster playerMaster in PlayerUtils.GetAllPlayerMasters(false))
            {
                Inventory inventory = playerMaster.inventory;
                if (!inventory)
                    continue;

                ItemIndex[] allCorruptableItems = getAllCorruptableItems(inventory).ToArray();
                if (allCorruptableItems.Length <= 0)
                    continue;

                ContagiousItemManager.TryForceReplacement(inventory, RNG.NextElementUniform(allCorruptableItems));
            }
        }
    }
}
