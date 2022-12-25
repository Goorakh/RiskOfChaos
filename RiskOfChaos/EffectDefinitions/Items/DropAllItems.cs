using RiskOfChaos.EffectHandling;
using RiskOfChaos.Utilities;
using RoR2;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace RiskOfChaos.EffectDefinitions.Items
{
    [ChaosEffect("DropAllItems")]
    public class DropAllItems : BaseEffect
    {
        [EffectCanActivate]
        static bool CanActivate()
        {
            return PlayerUtils.GetAllPlayerBodies(true).Any(b => getPickupsToDrop(b, false).Any());
        }

        public override void OnStart()
        {
            foreach (CharacterBody playerBody in PlayerUtils.GetAllPlayerBodies(true))
            {
                Vector3 bodyPosition = playerBody.corePosition;

                List<PickupIndex> pickupsToDrop = getPickupsToDrop(playerBody, true).ToList();

                float angle = 360f / pickupsToDrop.Count;
                Vector3 dropVelocity = Quaternion.AngleAxis(UnityEngine.Random.Range(0f, 360f), Vector3.up) * (Vector3.up * 40f + Vector3.forward * 5f);
                Quaternion rotation = Quaternion.AngleAxis(angle, Vector3.up);

                foreach (PickupIndex pickup in pickupsToDrop)
                {
                    PickupDropletController.CreatePickupDroplet(pickup, bodyPosition, dropVelocity);

                    dropVelocity = rotation * dropVelocity;
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
                if (itemDef && !itemDef.hidden && itemDef.canRemove)
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
