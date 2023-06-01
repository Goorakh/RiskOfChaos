using RiskOfChaos.EffectHandling;
using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Methods;
using RoR2;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace RiskOfChaos.EffectDefinitions.Character
{
    [ChaosEffect("drop_all_items", EffectWeightReductionPercentagePerActivation = 15f, EffectRepetitionWeightCalculationMode = EffectActivationCountMode.PerRun)]
    public sealed class DropAllItems : BaseEffect
    {
        [EffectCanActivate]
        static bool CanActivate(EffectCanActivateContext context)
        {
            return !context.IsNow || CharacterBody.readOnlyInstancesList.Any(b => getPickupsToDrop(b, false).Any());
        }

        [EffectWeightMultiplierSelector]
        static float GetWeightMultiplier()
        {
            IEnumerable<CharacterBody> charactersWithDroppableItems = CharacterBody.readOnlyInstancesList
                                                                                   .Where(b => getPickupsToDrop(b, false).Any());

            // If only non-player characters have droppable items -> Decrease weight
            return charactersWithDroppableItems.Any(b => b.isPlayerControlled) ? 1f : 0.5f;
        }

        public override void OnStart()
        {
            foreach (CharacterBody body in CharacterBody.readOnlyInstancesList)
            {
                Vector3 bodyPosition = body.corePosition;

                List<PickupIndex> pickupsToDrop = getPickupsToDrop(body, true).ToList();

                Vector3 dropVelocity = Quaternion.AngleAxis(Random.Range(0f, 360f), Vector3.up) * ((Vector3.up * 40f) + (Vector3.forward * 5f));
                Quaternion rotationPerDrop = Quaternion.AngleAxis(360f / pickupsToDrop.Count, Vector3.up);

                foreach (PickupIndex pickup in pickupsToDrop)
                {
                    PickupDropletController.CreatePickupDroplet(pickup, bodyPosition, dropVelocity);

                    dropVelocity = rotationPerDrop * dropVelocity;
                }
            }
        }

        static IEnumerable<PickupIndex> getPickupsToDrop(CharacterBody playerBody, bool remove)
        {
            Inventory inventory = playerBody.inventory;
            if (!inventory)
                yield break;

            for (ItemIndex i = 0; i < (ItemIndex)ItemCatalog.itemCount; i++)
            {
                ItemDef itemDef = ItemCatalog.GetItemDef(i);
                if (itemDef && !itemDef.hidden && itemDef.canRemove && itemDef.pickupModelPrefab)
                {
                    int itemCount = inventory.GetItemCount(itemDef);
                    if (itemCount > 0)
                    {
                        PickupIndex pickupIndex = PickupCatalog.FindPickupIndex(itemDef.itemIndex);
                        for (int j = 0; j < itemCount; j++)
                        {
                            yield return pickupIndex;
                        }

                        if (remove)
                        {
                            inventory.RemoveItem(itemDef, itemCount);
                        }
                    }
                }
            }

            int equipmentSlotCount = inventory.GetEquipmentSlotCount();
            for (uint i = 0; i < equipmentSlotCount; i++)
            {
                EquipmentState equipmentState = inventory.GetEquipment(i);
                if (equipmentState.equipmentIndex != EquipmentIndex.None)
                {
                    EquipmentDef equipmentDef = EquipmentCatalog.GetEquipmentDef(equipmentState.equipmentIndex);
                    if (equipmentDef && equipmentDef.pickupModelPrefab)
                    {
                        yield return PickupCatalog.FindPickupIndex(equipmentState.equipmentIndex);

                        if (remove)
                        {
                            inventory.SetEquipmentIndexForSlot(EquipmentIndex.None, i);
                        }
                    }
                }
            }
        }
    }
}
