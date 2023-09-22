using RoR2;

namespace RiskOfChaos.Utilities.Pickup
{
    public abstract record class PickupInfo(Inventory Inventory, PickupIndex PickupIndex)
    {
        public virtual int PickupDropletCount => 1;

        public abstract void RemoveFromInventory();
    }
}
