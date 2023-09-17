using RiskOfChaos.EffectHandling;
using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Methods;
using RiskOfChaos.Utilities;
using RiskOfChaos.Utilities.Extensions;
using RoR2;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace RiskOfChaos.EffectDefinitions.Character
{
    [ChaosEffect("drop_all_items", EffectWeightReductionPercentagePerActivation = 15f, EffectRepetitionWeightCalculationMode = EffectActivationCountMode.PerRun)]
    public sealed class DropAllItems : BaseEffect
    {
        abstract record class PickupInfo(Inventory Inventory, PickupIndex PickupIndex)
        {
            public virtual int PickupDropletCount => 1;

            public abstract void RemoveFromInventory();
        }

        sealed record class ItemPickupInfo : PickupInfo
        {
            public readonly ItemIndex ItemIndex;
            public readonly int ItemCount;

            public override int PickupDropletCount => ItemCount;

            public ItemPickupInfo(Inventory inventory, ItemIndex itemIndex, int itemCount) : base(inventory, PickupCatalog.FindPickupIndex(itemIndex))
            {
                ItemCount = itemCount;
                ItemIndex = itemIndex;
            }

            public override void RemoveFromInventory()
            {
                Inventory.RemoveItem(ItemIndex, ItemCount);
            }
        }

        sealed record class EquipmentPickupInfo : PickupInfo
        {
            public readonly EquipmentIndex EquipmentIndex;
            public readonly uint EquipmentSlotIndex;

            public EquipmentPickupInfo(Inventory inventory, EquipmentIndex equipmentIndex, uint equipmentSlotIndex) : base(inventory, PickupCatalog.FindPickupIndex(equipmentIndex))
            {
                EquipmentSlotIndex = equipmentSlotIndex;
                EquipmentIndex = equipmentIndex;
            }

            public override void RemoveFromInventory()
            {
                Inventory.SetEquipmentIndexForSlot(EquipmentIndex.None, EquipmentSlotIndex);
            }
        }

        static IEnumerable<CharacterBody> getAllCharactersWithDroppableItems()
        {
            return CharacterBody.readOnlyInstancesList.Where(b => getPickupsToDrop(b).Any());
        }

        [EffectCanActivate]
        static bool CanActivate(in EffectCanActivateContext context)
        {
            return !context.IsNow || getAllCharactersWithDroppableItems().Any();
        }

        [EffectWeightMultiplierSelector]
        static float GetWeightMultiplier()
        {
            // If only non-player characters have droppable items -> Decrease weight
            return getAllCharactersWithDroppableItems().Any(b => b.isPlayerControlled) ? 1f : 0.5f;
        }

        public override void OnStart()
        {
            CharacterBody.readOnlyInstancesList.TryDo(body =>
            {
                Vector3 bodyPosition = body.corePosition;

                PickupInfo[] pickupsToDrop = getPickupsToDrop(body).ToArray();

                Vector3 dropVelocity = Quaternion.AngleAxis(RNG.RangeFloat(0f, 360f), Vector3.up) * ((Vector3.up * 40f) + (Vector3.forward * 5f));
                Quaternion rotationPerDrop = Quaternion.AngleAxis(360f / pickupsToDrop.Sum(p => p.PickupDropletCount), Vector3.up);

                foreach (PickupInfo pickupInfo in pickupsToDrop)
                {
                    pickupInfo.RemoveFromInventory();

                    int dropletCount = pickupInfo.PickupDropletCount;
                    for (int i = 0; i < dropletCount; i++)
                    {
                        PickupDropletController.CreatePickupDroplet(pickupInfo.PickupIndex, bodyPosition, dropVelocity);

                        dropVelocity = rotationPerDrop * dropVelocity;
                    }
                }
            }, FormatUtils.GetBestBodyName);
        }

        static IEnumerable<PickupInfo> getPickupsToDrop(CharacterBody playerBody)
        {
            Inventory inventory = playerBody.inventory;
            if (!inventory)
                yield break;

            foreach (ItemIndex i in inventory.itemAcquisitionOrder)
            {
                ItemDef itemDef = ItemCatalog.GetItemDef(i);
                if (itemDef && !itemDef.hidden && itemDef.canRemove && itemDef.pickupModelPrefab)
                {
                    yield return new ItemPickupInfo(inventory, itemDef.itemIndex, inventory.GetItemCount(itemDef));
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
                        yield return new EquipmentPickupInfo(inventory, equipmentState.equipmentIndex, i);
                    }
                }
            }
        }
    }
}
