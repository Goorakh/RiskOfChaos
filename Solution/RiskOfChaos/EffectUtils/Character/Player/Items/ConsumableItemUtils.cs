using RiskOfChaos.Utilities;
using RoR2;
using System;
using System.Collections.Generic;

namespace RiskOfChaos.EffectUtils.Character.Player.Items
{
    public static class ConsumableItemUtils
    {
        public static ReadOnlyMemory<ConsumableItemPair> ConsumableItemPairs { get; private set; } = ReadOnlyMemory<ConsumableItemPair>.Empty;

        [SystemInitializer(typeof(ItemCatalog), typeof(EquipmentCatalog))]
        static void Init()
        {
            static bool isValidItem(ItemDef itemDef)
            {
                return itemDef && !itemDef.hidden && !Language.IsTokenInvalid(itemDef.nameToken);
            }

            static ItemIndex findItemIndexCaseInsensitive(string name)
            {
                for (int i = 0; i < ItemCatalog.itemCount; i++)
                {
                    ItemDef itemDef = ItemCatalog.GetItemDef((ItemIndex)i);
                    if (itemDef && string.Equals(itemDef.name, name, StringComparison.OrdinalIgnoreCase))
                    {
                        return (ItemIndex)i;
                    }
                }

                return ItemIndex.None;
            }

            List<ConsumableItemPair> consumableItemPairs = [];

            for (int i = 0; i < ItemCatalog.itemCount; i++)
            {
                ItemIndex itemIndex = (ItemIndex)i;
                ItemDef item = ItemCatalog.GetItemDef(itemIndex);
                if (!isValidItem(item))
                    continue;

                string itemName = item.name;
                if (string.IsNullOrWhiteSpace(itemName))
                    continue;

                string consumedName = itemName + "Consumed";
                ItemIndex consumedItemIndex = findItemIndexCaseInsensitive(consumedName);
                if (consumedItemIndex == ItemIndex.None)
                    continue;

                ItemDef consumedItem = ItemCatalog.GetItemDef(consumedItemIndex);
                if (!isValidItem(consumedItem))
                    continue;

                consumableItemPairs.Add(new ConsumableItemPair(PickupCatalog.FindPickupIndex(itemIndex), PickupCatalog.FindPickupIndex(consumedItemIndex)));

                Log.Debug($"Registered consumable item pair: {FormatUtils.GetBestItemDisplayName(item)} -> {FormatUtils.GetBestItemDisplayName(consumedItem)}");
            }

            static bool isValidEquipment(EquipmentDef equipmentDef)
            {
                return equipmentDef && !Language.IsTokenInvalid(equipmentDef.nameToken);
            }

            static EquipmentIndex findEquipmentIndexCaseInsensitive(string name)
            {
                for (int i = 0; i < EquipmentCatalog.equipmentCount; i++)
                {
                    EquipmentDef equipmentDef = EquipmentCatalog.GetEquipmentDef((EquipmentIndex)i);
                    if (equipmentDef && string.Equals(equipmentDef.name, name, StringComparison.OrdinalIgnoreCase))
                    {
                        return (EquipmentIndex)i;
                    }
                }

                return EquipmentIndex.None;
            }

            for (int i = 0; i < EquipmentCatalog.equipmentCount; i++)
            {
                EquipmentIndex equipmentIndex = (EquipmentIndex)i;
                EquipmentDef equipmentDef = EquipmentCatalog.GetEquipmentDef(equipmentIndex);
                if (!isValidEquipment(equipmentDef))
                    continue;

                string equipmentName = equipmentDef.name;
                if (string.IsNullOrWhiteSpace(equipmentName))
                    continue;

                string consumedName = equipmentName + "Consumed";
                EquipmentIndex consumedEquipmentIndex = findEquipmentIndexCaseInsensitive(consumedName);
                if (consumedEquipmentIndex == EquipmentIndex.None)
                    continue;

                EquipmentDef consumedEquipmentDef = EquipmentCatalog.GetEquipmentDef(consumedEquipmentIndex);
                if (!isValidEquipment(consumedEquipmentDef))
                    continue;

                consumableItemPairs.Add(new ConsumableItemPair(PickupCatalog.FindPickupIndex(equipmentIndex), PickupCatalog.FindPickupIndex(consumedEquipmentIndex)));

                Log.Debug($"Registered consumable equipment pair: {FormatUtils.GetBestEquipmentDisplayName(equipmentDef)} -> {FormatUtils.GetBestEquipmentDisplayName(consumedEquipmentDef)}");
            }

            ConsumableItemPairs = new ReadOnlyMemory<ConsumableItemPair>([.. consumableItemPairs]);
        }

        public readonly struct ConsumableItemPair : IEquatable<ConsumableItemPair>
        {
            public readonly PickupIndex Item;
            public readonly PickupIndex ConsumedItem;

            public ConsumableItemPair(PickupIndex item, PickupIndex consumedItem)
            {
                Item = item;
                ConsumedItem = consumedItem;
            }

            public override readonly bool Equals(object obj)
            {
                return obj is ConsumableItemPair otherPair && Equals(otherPair);
            }

            public readonly bool Equals(in ConsumableItemPair other)
            {
                return Item == other.Item && ConsumedItem == other.ConsumedItem;
            }

            readonly bool IEquatable<ConsumableItemPair>.Equals(ConsumableItemPair other)
            {
                return Equals(other);
            }

            public override readonly int GetHashCode()
            {
                return HashCode.Combine(Item, ConsumedItem);
            }

            public static bool operator ==(in ConsumableItemPair left, in ConsumableItemPair right)
            {
                return ((IEquatable<ConsumableItemPair>)left).Equals(right);
            }

            public static bool operator !=(in ConsumableItemPair left, in ConsumableItemPair right)
            {
                return !((IEquatable<ConsumableItemPair>)left).Equals(right);
            }
        }
    }
}
