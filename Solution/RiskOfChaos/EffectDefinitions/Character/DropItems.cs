using RiskOfChaos.ConfigHandling;
using RiskOfChaos.ConfigHandling.AcceptableValues;
using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Data;
using RiskOfChaos.Utilities;
using RiskOfChaos.Utilities.Extensions;
using RiskOfChaos.Utilities.Pickup;
using RiskOfOptions.OptionConfigs;
using RoR2;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;
using UnityEngine.Networking;

namespace RiskOfChaos.EffectDefinitions.Character
{
    [ChaosTimedEffect("drop_items", 10f, AllowDuplicates = false)]
    public sealed class DropItems : MonoBehaviour
    {
        [EffectConfig]
        static readonly ConfigHolder<float> _itemDropFrequency =
            ConfigFactory<float>.CreateConfig("Item Drop Frequency", 0.9f)
                                .Description("How often items will be dropped")
                                .AcceptableValues(new AcceptableValueMin<float>(0f))
                                .OptionConfig(new FloatFieldConfig { Min = 0f, FormatString = "{0:F}s" })
                                .Build();

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
                _body = GetComponent<CharacterBody>();
                _inventory = _body.inventory;

                scheduleNextDrop();
                _dropItemTimer *= RoR2Application.rng.nextNormalizedFloat;
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
                _dropItemTimer = _itemDropFrequency.Value;
            }

            void tryDropRandomItem()
            {
                if (!_body || !_inventory)
                    return;

                int equipmentSlotCount = _inventory.GetEquipmentSlotCount();

                WeightedSelection<PickupInfo> droppableItemSelection = new WeightedSelection<PickupInfo>();
                droppableItemSelection.EnsureCapacity(_inventory.itemAcquisitionOrder.Count + equipmentSlotCount);

                foreach (ItemIndex item in _inventory.itemAcquisitionOrder)
                {
                    ItemPickupInfo pickupInfo = new ItemPickupInfo(_inventory, item, 1);
                    if (ItemDropFilter(pickupInfo))
                    {
                        droppableItemSelection.AddChoice(pickupInfo, _inventory.GetItemCount(item));
                    }
                }

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

        readonly List<DropItemsOnTimer> _itemDropControllers = [];

        readonly List<OnDestroyCallback> _destroyCallbacks = [];

        bool _trackedObjectDestroyed = false;

        void Start()
        {
            if (NetworkServer.active)
            {
                ReadOnlyCollection<CharacterBody> characterBodies = CharacterBody.readOnlyInstancesList;

                _itemDropControllers.EnsureCapacity(characterBodies.Count);
                _destroyCallbacks.EnsureCapacity(characterBodies.Count);

                characterBodies.TryDo(addComponentTo, FormatUtils.GetBestBodyName);
                CharacterBody.onBodyStartGlobal += addComponentTo;
            }
        }

        void OnDestroy()
        {
            CharacterBody.onBodyStartGlobal -= addComponentTo;

            foreach (OnDestroyCallback destroyCallback in _destroyCallbacks)
            {
                if (destroyCallback)
                {
                    OnDestroyCallback.RemoveCallback(destroyCallback);
                }
            }

            _destroyCallbacks.Clear();

            foreach (DropItemsOnTimer dropComponent in _itemDropControllers)
            {
                Destroy(dropComponent);
            }

            _itemDropControllers.Clear();
        }

        void FixedUpdate()
        {
            if (_trackedObjectDestroyed)
            {
                _trackedObjectDestroyed = false;

                UnityObjectUtils.RemoveAllDestroyed(_destroyCallbacks);

                int removedDropControllers = UnityObjectUtils.RemoveAllDestroyed(_itemDropControllers);
#if DEBUG
                Log.Debug($"Cleared {removedDropControllers} destroy drop controller(s)");
#endif
            }
        }

        void addComponentTo(CharacterBody body)
        {
            DropItemsOnTimer dropComponent = body.gameObject.AddComponent<DropItemsOnTimer>();

            _itemDropControllers.Add(dropComponent);

            OnDestroyCallback destroyCallback = OnDestroyCallback.AddCallback(dropComponent.gameObject, _ =>
            {
                _trackedObjectDestroyed = true;
            });

            _destroyCallbacks.Add(destroyCallback);
        }
    }
}
