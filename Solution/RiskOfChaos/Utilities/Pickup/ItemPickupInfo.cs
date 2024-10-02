using RoR2;

namespace RiskOfChaos.Utilities.Pickup
{
    public sealed record class ItemPickupInfo : PickupInfo
    {
        public readonly ItemIndex ItemIndex;
        public readonly int ItemCount;

        public override int PickupDropletCount => ItemCount;

        public ItemPickupInfo(Inventory inventory, ItemIndex itemIndex, int itemCount) : base(inventory, PickupCatalog.FindPickupIndex(itemIndex))
        {
            ItemCount = itemCount;
            ItemIndex = itemIndex;
        }

        public ItemPickupInfo(Inventory inventory, ItemIndex itemIndex) : this(inventory, itemIndex, inventory.GetItemCount(itemIndex))
        {
        }

        public override void RemoveFromInventory()
        {
            Inventory.RemoveItem(ItemIndex, ItemCount);
        }
    }
}
