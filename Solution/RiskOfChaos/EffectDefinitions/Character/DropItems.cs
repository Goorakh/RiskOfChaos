using RiskOfChaos.ConfigHandling;
using RiskOfChaos.ConfigHandling.AcceptableValues;
using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Data;
using RiskOfChaos.EffectHandling.EffectComponents;
using RiskOfChaos.Utilities;
using RiskOfChaos.Utilities.Extensions;
using RiskOfChaos.Utilities.Pickup;
using RiskOfOptions.OptionConfigs;
using RoR2;
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

        sealed class DropItemsOnTimer : MonoBehaviour
        {
            static readonly Vector3 _baseDropVelocity = new Vector3(0f, 25f, 10f);

            public ChaosEffectComponent OwnerEffect;

            CharacterBody _body;
            Inventory _inventory;

            Xoroshiro128Plus _rng;

            float _dropItemTimer;

            public static bool ItemDropFilter(PickupInfo pickup)
            {
                switch (pickup)
                {
                    case ItemPickupInfo itemPickup:
                        ItemDef itemDef = ItemCatalog.GetItemDef(itemPickup.ItemIndex);
                        return !itemDef.hidden && itemDef.canRemove;
                    case EquipmentPickupInfo:
                        return true;
                    default:
                        return false;
                }
            }

            void Start()
            {
                if (OwnerEffect)
                {
                    OwnerEffect.OnEffectEnd += onEffectEnd;
                }
            }

            void OnDestroy()
            {
                if (OwnerEffect)
                {
                    OwnerEffect.OnEffectEnd -= onEffectEnd;
                }
            }

            void Awake()
            {
                _body = GetComponent<CharacterBody>();
                _inventory = _body.inventory;

                _rng = new Xoroshiro128Plus(RoR2Application.rng.nextUlong);

                scheduleNextDrop();
                _dropItemTimer *= _rng.nextNormalizedFloat;
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

            void onEffectEnd(ChaosEffectComponent effectComponent)
            {
                Destroy(this);
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
                droppableItemSelection.EnsureCapacity(_inventory.itemAcquisitionOrder.Count + (equipmentSlotCount * 2));

                foreach (ItemIndex item in _inventory.itemAcquisitionOrder)
                {
                    int droppableItemCount = _inventory.GetItemCountPermanent(item);
                    if (droppableItemCount > 0)
                    {
                        ItemPickupInfo pickupInfo = new ItemPickupInfo(_inventory, item, 1);
                        if (ItemDropFilter(pickupInfo))
                        {
                            droppableItemSelection.AddChoice(pickupInfo, droppableItemCount);
                        }
                    }
                }

                for (uint slot = 0; slot < equipmentSlotCount; slot++)
                {
                    int equipmentSetCount = _inventory.GetEquipmentSetCount(slot);

                    for (uint set = 0; set < equipmentSetCount; set++)
                    {
                        EquipmentIndex equipmentIndex = _inventory.GetEquipment(slot, set).equipmentIndex;
                        if (equipmentIndex == EquipmentIndex.None)
                            continue;

                        EquipmentPickupInfo pickupInfo = new EquipmentPickupInfo(_inventory, equipmentIndex, slot, set);
                        if (ItemDropFilter(pickupInfo))
                        {
                            droppableItemSelection.AddChoice(pickupInfo, 1f);
                        }
                    }
                }

                if (droppableItemSelection.Count == 0)
                    return;

                PickupInfo itemToDrop = droppableItemSelection.Evaluate(_rng.nextNormalizedFloat);

                itemToDrop.RemoveFromInventory();

                Vector3 dropVelocity = Quaternion.AngleAxis(_rng.RangeFloat(0f, 360f), Vector3.up) * _baseDropVelocity;
                Quaternion rotationPerDrop = Quaternion.AngleAxis(360f / itemToDrop.PickupDropletCount, Vector3.up);

                for (int i = itemToDrop.PickupDropletCount - 1; i >= 0; i--)
                {
                    PickupDropletController.CreatePickupDroplet(new UniquePickup(itemToDrop.PickupIndex), _body.corePosition, dropVelocity, false, false);

                    dropVelocity = rotationPerDrop * dropVelocity;
                }
            }
        }

        ChaosEffectComponent _effectComponent;

        void Awake()
        {
            _effectComponent = GetComponent<ChaosEffectComponent>();
        }

        void Start()
        {
            if (NetworkServer.active)
            {
                CharacterBody.readOnlyInstancesList.TryDo(addComponentTo, FormatUtils.GetBestBodyName);
                CharacterBody.onBodyStartGlobal += addComponentTo;
            }
        }

        void OnDestroy()
        {
            CharacterBody.onBodyStartGlobal -= addComponentTo;
        }

        void addComponentTo(CharacterBody body)
        {
            DropItemsOnTimer dropComponent = body.gameObject.AddComponent<DropItemsOnTimer>();
            dropComponent.OwnerEffect = _effectComponent;
        }
    }
}
