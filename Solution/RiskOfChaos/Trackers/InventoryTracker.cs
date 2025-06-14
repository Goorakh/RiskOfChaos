using HG;
using RoR2;
using UnityEngine;

namespace RiskOfChaos.Trackers
{
    public sealed class InventoryTracker : MonoBehaviour
    {
        [SystemInitializer]
        static void Init()
        {
            Inventory.onInventoryChangedGlobal += Inventory_onInventoryChangedGlobal;
            CharacterMaster.onStartGlobal += CharacterMaster_onStartGlobal;
        }

        static void CharacterMaster_onStartGlobal(CharacterMaster characterMaster)
        {
            trackInventory(characterMaster.inventory);
        }

        static void Inventory_onInventoryChangedGlobal(Inventory inventory)
        {
            trackInventory(inventory);
        }

        static void trackInventory(Inventory inventory)
        {
            if (!inventory)
                return;

            InventoryTracker tracker = inventory.gameObject.EnsureComponent<InventoryTracker>();
            tracker.Inventory = inventory;
        }

        public Inventory Inventory { get; private set; }

        void OnEnable()
        {
            InstanceTracker.Add(this);
        }

        void OnDisable()
        {
            InstanceTracker.Remove(this);
        }
    }
}
