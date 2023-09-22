using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.Utilities;
using RiskOfChaos.Utilities.Extensions;
using RiskOfChaos.Utilities.Pickup;
using RoR2;
using UnityEngine;

namespace RiskOfChaos.EffectDefinitions.Character
{
    [ChaosTimedEffect("drop_items", 20f, AllowDuplicates = false)]
    public sealed class DropItems : TimedEffect
    {
        [RequireComponent(typeof(CharacterBody))]
        class DropItemsOnTimer : MonoBehaviour
        {
            static readonly Vector3 _baseDropVelocity = new Vector3(0f, 25f, 10f);

            CharacterBody _body;
            Inventory _inventory;

            float _dropItemTimer;

            public static bool ItemDropFilter(PickupInfo pickup)
            {
                switch (pickup)
                {
                    case ItemPickupInfo itemPickup:
                        ItemDef itemDef = ItemCatalog.GetItemDef(itemPickup.ItemIndex);
                        return !itemDef.hidden && itemDef.canRemove && itemDef.pickupModelPrefab;
                    case EquipmentPickupInfo:
                        return true;
                    default:
                        return false;
                }
            }

            void Awake()
            {
                InstanceTracker.Add(this);

                _body = GetComponent<CharacterBody>();
                _inventory = _body.inventory;

                scheduleNextDrop();
                _dropItemTimer *= RoR2Application.rng.nextNormalizedFloat;
            }

            void OnDestroy()
            {
                InstanceTracker.Remove(this);
            }

            void FixedUpdate()
            {
                _dropItemTimer -= Time.fixedDeltaTime;
                if (_dropItemTimer <= 0f)
                {
                    scheduleNextDrop();
                    tryDropRandomItem();
                }
            }

            void scheduleNextDrop()
            {
                _dropItemTimer = 0.8f;
            }

            void tryDropRandomItem()
            {
                if (!_body || !_inventory)
                    return;

                WeightedSelection<PickupInfo> droppableItemSelection = new WeightedSelection<PickupInfo>();

                foreach (ItemIndex item in _inventory.itemAcquisitionOrder)
                {
                    ItemPickupInfo pickupInfo = new ItemPickupInfo(_inventory, item, 1);
                    if (ItemDropFilter(pickupInfo))
                    {
                        droppableItemSelection.AddChoice(pickupInfo, _inventory.GetItemCount(item));
                    }
                }

                int equipmentSlotCount = _inventory.GetEquipmentSlotCount();
                for (uint i = 0; i < equipmentSlotCount; i++)
                {
                    EquipmentIndex equipmentIndex = _inventory.GetEquipment(i).equipmentIndex;
                    if (equipmentIndex == EquipmentIndex.None)
                        continue;

                    EquipmentPickupInfo pickupInfo = new EquipmentPickupInfo(_inventory, equipmentIndex, i);
                    if (ItemDropFilter(pickupInfo))
                    {
                        droppableItemSelection.AddChoice(pickupInfo, 1f);
                    }
                }

                if (droppableItemSelection.Count == 0)
                    return;

                PickupInfo itemToDrop = droppableItemSelection.Evaluate(RoR2Application.rng.nextNormalizedFloat);

                itemToDrop.RemoveFromInventory();

                Vector3 dropVelocity = Quaternion.AngleAxis(RoR2Application.rng.RangeFloat(0f, 360f), Vector3.up) * _baseDropVelocity;
                Quaternion rotationPerDrop = Quaternion.AngleAxis(360f / itemToDrop.PickupDropletCount, Vector3.up);

                for (int i = itemToDrop.PickupDropletCount - 1; i >= 0; i--)
                {
                    PickupDropletController.CreatePickupDroplet(itemToDrop.PickupIndex, _body.corePosition, dropVelocity);

                    dropVelocity = rotationPerDrop * dropVelocity;
                }
            }
        }

        static void addComponentTo(CharacterBody body)
        {
            body.gameObject.AddComponent<DropItemsOnTimer>();
        }

        public override void OnStart()
        {
            CharacterBody.readOnlyInstancesList.TryDo(addComponentTo, FormatUtils.GetBestBodyName);
            CharacterBody.onBodyStartGlobal += addComponentTo;
        }

        public override void OnEnd()
        {
            CharacterBody.onBodyStartGlobal -= addComponentTo;

            InstanceUtils.DestroyAllTrackedInstances<DropItemsOnTimer>();
        }
    }
}
