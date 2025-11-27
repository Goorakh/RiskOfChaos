using RiskOfChaos.Utilities.Pickup;
using RoR2;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace RiskOfChaos.Utilities.Extensions
{
    public static class InventoryExtensions
    {
        public enum PickupReplacementRule
        {
            KeepExisting,
            DeleteExisting,
            DropExisting
        }

        public static int GetEquipmentCount(this Inventory inventory, EquipmentIndex equipmentIndex)
        {
            int equipmentCount = 0;

            int equipmentSlotCount = inventory.GetEquipmentSlotCount();
            for (uint slot = 0; slot < equipmentSlotCount; slot++)
            {
                int equipmentSetCount = inventory.GetEquipmentSetCount(slot);
                for (uint set = 0; set < equipmentSetCount; set++)
                {
                    if (inventory.GetEquipment(slot, set).equipmentIndex == equipmentIndex)
                    {
                        equipmentCount++;
                    }
                }
            }

            return equipmentCount;
        }

        public static bool TryTakeEquipment(this Inventory inventory, EquipmentIndex equipmentIndex, int count = 1)
        {
            if (count <= 0)
                return true;

            int equipmentSlotCount = inventory.GetEquipmentSlotCount();
            for (uint slot = 0; slot < equipmentSlotCount; slot++)
            {
                int equipmentSetCount = inventory.GetEquipmentSetCount(slot);
                for (uint set = 0; set < equipmentSetCount; set++)
                {
                    if (inventory.GetEquipment(slot, set).equipmentIndex == equipmentIndex)
                    {
                        inventory.SetEquipmentIndexForSlot(EquipmentIndex.None, slot, set);

                        if (--count == 0)
                            return true;
                    }
                }
            }

            return false;
        }

        public static EquipmentLocation[] FindBestLocationsForNewEquipment(this Inventory inventory)
        {
            int equipmentSetCount = inventory.GetEquipmentSetCount(inventory.activeEquipmentSlot);

            if (inventory.GetEquipmentSlotCount() == 0 || equipmentSetCount == 0)
                return [new EquipmentLocation(0, 0)];

            List<EquipmentLocation> emptyEquipmentLocations = new List<EquipmentLocation>(equipmentSetCount);
            List<EquipmentLocation> occupiedEquipmentLocations = new List<EquipmentLocation>(equipmentSetCount);

            EquipmentLocation activeEquipmentLocation = new EquipmentLocation(inventory.activeEquipmentSlot, inventory.activeEquipmentSet[inventory.activeEquipmentSlot]);
            if (inventory.GetEquipment(activeEquipmentLocation.Slot, activeEquipmentLocation.Set).equipmentIndex == EquipmentIndex.None)
            {
                emptyEquipmentLocations.Add(activeEquipmentLocation);
            }
            else
            {
                occupiedEquipmentLocations.Add(activeEquipmentLocation);
            }

            for (uint set = 0; set < equipmentSetCount; set++)
            {
                if (set != inventory.activeEquipmentSet[inventory.activeEquipmentSlot])
                {
                    EquipmentLocation equipmentLocation = new EquipmentLocation(inventory.activeEquipmentSlot, set);
                    if (inventory.GetEquipment(equipmentLocation.Slot, equipmentLocation.Set).equipmentIndex == EquipmentIndex.None)
                    {
                        emptyEquipmentLocations.Add(equipmentLocation);
                    }
                    else
                    {
                        occupiedEquipmentLocations.Add(equipmentLocation);
                    }
                }
            }

            return [.. emptyEquipmentLocations, .. occupiedEquipmentLocations];
        }

        public static int GetOwnedItemCount(this Inventory inventory, ItemIndex itemIndex)
        {
            return inventory.GetItemCountPermanent(itemIndex) + inventory.GetItemCountTemp(itemIndex);
        }
        public static int GetPickupCountEffective(this Inventory inventory, PickupIndex pickupIndex)
        {
            if (!inventory)
                return 0;

            PickupDef pickupDef = PickupCatalog.GetPickupDef(pickupIndex);
            if (pickupDef == null)
                return 0;

            if (pickupDef.itemIndex != ItemIndex.None)
            {
                return inventory.GetItemCountEffective(pickupDef.itemIndex);
            }
            else if (pickupDef.equipmentIndex != EquipmentIndex.None)
            {
                return inventory.GetEquipmentCount(pickupDef.equipmentIndex);
            }
            else
            {
                Log.Warning($"Unhandled pickup index {pickupDef.pickupIndex}");
                return 0;
            }
        }

        public static int GetPickupCountPermanent(this Inventory inventory, PickupIndex pickupIndex)
        {
            if (!inventory)
                return 0;

            PickupDef pickupDef = PickupCatalog.GetPickupDef(pickupIndex);
            if (pickupDef == null)
                return 0;

            if (pickupDef.itemIndex != ItemIndex.None)
            {
                return inventory.GetItemCountPermanent(pickupDef.itemIndex);
            }
            else if (pickupDef.equipmentIndex != EquipmentIndex.None)
            {
                return inventory.GetEquipmentCount(pickupDef.equipmentIndex);
            }
            else
            {
                Log.Warning($"Unhandled pickup index {pickupDef.pickupIndex}");
                return 0;
            }
        }

        public static int GetPickupCountTemp(this Inventory inventory, PickupIndex pickupIndex)
        {
            if (!inventory)
                return 0;

            PickupDef pickupDef = PickupCatalog.GetPickupDef(pickupIndex);
            if (pickupDef == null)
                return 0;

            if (pickupDef.itemIndex != ItemIndex.None)
            {
                return inventory.GetItemCountTemp(pickupDef.itemIndex);
            }
            else if (pickupDef.equipmentIndex != EquipmentIndex.None)
            {
                return 0;
            }
            else
            {
                Log.Warning($"Unhandled pickup index {pickupDef.pickupIndex}");
                return 0;
            }
        }

        public static int GetPickupCountChanneled(this Inventory inventory, PickupIndex pickupIndex)
        {
            if (!inventory)
                return 0;

            PickupDef pickupDef = PickupCatalog.GetPickupDef(pickupIndex);
            if (pickupDef == null)
                return 0;

            if (pickupDef.itemIndex != ItemIndex.None)
            {
                return inventory.GetItemCountChanneled(pickupDef.itemIndex);
            }
            else if (pickupDef.equipmentIndex != EquipmentIndex.None)
            {
                return 0;
            }
            else
            {
                Log.Warning($"Unhandled pickup index {pickupDef.pickupIndex}");
                return 0;
            }
        }

        public static int GetOwnedPickupCount(this Inventory inventory, PickupIndex pickupIndex)
        {
            if (!inventory)
                return 0;

            PickupDef pickupDef = PickupCatalog.GetPickupDef(pickupIndex);
            if (pickupDef == null)
                return 0;

            if (pickupDef.itemIndex != ItemIndex.None)
            {
                return inventory.GetOwnedItemCount(pickupDef.itemIndex);
            }
            else if (pickupDef.equipmentIndex != EquipmentIndex.None)
            {
                return inventory.GetEquipmentCount(pickupDef.equipmentIndex);
            }
            else
            {
                Log.Warning($"Unhandled pickup index {pickupDef.pickupIndex}");
                return 0;
            }
        }

        public static bool HasAtLeastXTotalRemovableOwnedItemsOfTier(this Inventory inventory, ItemTier tier, int x)
        {
            int numEncounteredItems = 0;
            foreach (ItemIndex itemIndex in inventory.itemAcquisitionOrder)
            {
                ItemDef itemDef = ItemCatalog.GetItemDef(itemIndex);
                if (itemDef && itemDef.canRemove)
                {
                    numEncounteredItems += inventory.GetOwnedItemCount(itemIndex);
                    if (numEncounteredItems >= x)
                        break;
                }
            }

            return numEncounteredItems >= x;
        }

        public ref struct PickupTransformation
        {
            public static event Action<TryTransformResult> OnPickupTransformedServerGlobal;

            int? _minToTransform;

            int? _maxToTransform;

            public ItemTransformationTypeIndex TransformationType;

            public PickupReplacementRule ReplacementRule;

            public PickupIndex OriginalPickupIndex { readonly get; set; }

            public PickupIndex NewPickupIndex { readonly get; set; }

            public bool AllowWhenDisabled { readonly get; set; }

            public int MinToTransform
            {
                readonly get
                {
                    return _minToTransform.GetValueOrDefault(1);
                }
                set
                {
                    if (value <= 0)
                        throw new ArgumentOutOfRangeException("value", "Value must be a positive integer.");

                    _minToTransform = value;
                }
            }

            public int MaxToTransform
            {
                readonly get
                {
                    return _maxToTransform.GetValueOrDefault(1);
                }
                set
                {
                    _maxToTransform = value;
                }
            }

            public bool ForbidTempItems { readonly get; set; }

            public bool ForbidPermanentItems { readonly get; set; }

            public bool TryTransform(Inventory inventory, out TryTransformResult result)
            {
                result = TryTransformResult.Create();
                if (TryTake(inventory, out TakeResult takeResult))
                {
                    result.Inventory = inventory;
                    result.TakenPickup = takeResult.TakenPickup;
                    TryTransformResult tryTransformResult = takeResult.GiveTakenPickup(NewPickupIndex);
                    result.GivenPickup = tryTransformResult.GivenPickup;
                    return true;
                }

                return false;
            }

            public bool CanTake(Inventory inventory, out CanTakeResult result)
            {
                PickupDef originalPickupDef = PickupCatalog.GetPickupDef(OriginalPickupIndex);
                PickupDef newPickupDef = PickupCatalog.GetPickupDef(NewPickupIndex);
                if (originalPickupDef == null || newPickupDef == null)
                {
                    result = default;
                    return false;
                }

                result = new CanTakeResult
                {
                    Inventory = inventory,
                    TakenPickup = new PickupStack
                    {
                        PickupIndex = OriginalPickupIndex
                    },
                    ItemTransformationType = TransformationType,
                    ReplacementRule = ReplacementRule
                };

                if (originalPickupDef.equipmentIndex == EquipmentIndex.None && newPickupDef.equipmentIndex != EquipmentIndex.None)
                {
                    if (ReplacementRule == PickupReplacementRule.KeepExisting)
                    {
                        int numEmptyEquipmentLocations = 0;
                        foreach (EquipmentLocation equipmentLocation in inventory.FindBestLocationsForNewEquipment())
                        {
                            if (inventory.GetEquipment(equipmentLocation.Slot, equipmentLocation.Set).equipmentIndex == EquipmentIndex.None)
                            {
                                numEmptyEquipmentLocations++;
                            }
                        }

                        if (numEmptyEquipmentLocations < MinToTransform)
                            return false;

                        if (MaxToTransform > numEmptyEquipmentLocations)
                            MaxToTransform = numEmptyEquipmentLocations;
                    }
                }

                if (originalPickupDef.itemIndex != ItemIndex.None)
                {
                    Inventory.ItemTransformation itemTransformation = (Inventory.ItemTransformation)this;
                    bool canTake = itemTransformation.CanTake(inventory, out Inventory.ItemTransformation.CanTakeResult itemTakeResult);
                    result = itemTakeResult;
                    return canTake;
                }

                if (inventory.inventoryDisabled && !AllowWhenDisabled)
                    return false;

                if (originalPickupDef.equipmentIndex != EquipmentIndex.None)
                {
                    int equipmentCount = inventory.GetEquipmentCount(originalPickupDef.equipmentIndex);

                    int numRemainingToTake = MaxToTransform;
                    int takenCount = HGMath.TakeIntClamped(ref numRemainingToTake, equipmentCount);
                    int totalTakenCount = MaxToTransform - numRemainingToTake;
                    if (totalTakenCount < MinToTransform)
                        return false;

                    result.TakenPickup.StackValues.permanentStacks = takenCount;
                    result.TakenPickup.StackValues.totalStacks = totalTakenCount;

                    return true;
                }

                return false;
            }

            public CanTakeResult? CanTake(Inventory inventory)
            {
                if (!CanTake(inventory, out CanTakeResult canTakeResult))
                    return null;

                return canTakeResult;
            }

            public bool TryTake(Inventory inventory, out TakeResult result)
            {
                result = new TakeResult
                {
                    Inventory = inventory,
                    TakenPickup = new PickupStack
                    {
                        PickupIndex = OriginalPickupIndex
                    }
                };

                if (CanTake(inventory, out CanTakeResult canTakeResult))
                {
                    result = canTakeResult.PerformTake();
                    return true;
                }

                return false;
            }

            public TakeResult? TryTake(Inventory inventory)
            {
                if (!TryTake(inventory, out TakeResult takeResult))
                    return null;

                return takeResult;
            }

            public static explicit operator Inventory.ItemTransformation(in PickupTransformation pickupTransformation)
            {
                PickupDef originalPickupDef = PickupCatalog.GetPickupDef(pickupTransformation.OriginalPickupIndex);
                PickupDef newPickupDef = PickupCatalog.GetPickupDef(pickupTransformation.NewPickupIndex);

                return new Inventory.ItemTransformation
                {
                    allowWhenDisabled = pickupTransformation.AllowWhenDisabled,
                    forbidPermanentItems = pickupTransformation.ForbidPermanentItems,
                    forbidTempItems = pickupTransformation.ForbidTempItems,
                    maxToTransform = pickupTransformation.MaxToTransform,
                    minToTransform = pickupTransformation.MinToTransform,
                    newItemIndex = newPickupDef?.itemIndex ?? ItemIndex.None,
                    originalItemIndex = originalPickupDef?.itemIndex ?? ItemIndex.None,
                    transformationType = pickupTransformation.TransformationType,
                };
            }

            public static implicit operator PickupTransformation(in Inventory.ItemTransformation itemTransformation)
            {
                return new PickupTransformation
                {
                    AllowWhenDisabled = itemTransformation.allowWhenDisabled,
                    ReplacementRule = PickupReplacementRule.DropExisting,
                    ForbidPermanentItems = itemTransformation.forbidPermanentItems,
                    ForbidTempItems = itemTransformation.forbidTempItems,
                    MaxToTransform = itemTransformation.maxToTransform,
                    MinToTransform = itemTransformation.minToTransform,
                    NewPickupIndex = PickupCatalog.FindPickupIndex(itemTransformation.newItemIndex),
                    OriginalPickupIndex = PickupCatalog.FindPickupIndex(itemTransformation.originalItemIndex),
                    TransformationType = itemTransformation.transformationType,
                };
            }

            public struct CanTakeResult
            {
                public Inventory Inventory;

                public PickupStack TakenPickup;

                public ItemTransformationTypeIndex ItemTransformationType;

                public PickupReplacementRule ReplacementRule;

                public TakeResult PerformTake()
                {
                    PickupDef takenPickupDef = PickupCatalog.GetPickupDef(TakenPickup.PickupIndex);
                    if (takenPickupDef != null)
                    {
                        if (takenPickupDef.itemIndex != ItemIndex.None)
                        {
                            Inventory.ItemTransformation.CanTakeResult itemCanTakeResult = (Inventory.ItemTransformation.CanTakeResult)this;
                            return itemCanTakeResult.PerformTake();
                        }
                        else if (takenPickupDef.equipmentIndex != EquipmentIndex.None)
                        {
                            Inventory.TryTakeEquipment(takenPickupDef.equipmentIndex, TakenPickup.StackValues.totalStacks);
                        }
                    }

                    return new TakeResult
                    {
                        Inventory = Inventory,
                        TakenPickup = TakenPickup,
                        TransformationType = ItemTransformationType,
                        ReplacementRule = ReplacementRule
                    };
                }

                public static explicit operator Inventory.ItemTransformation.CanTakeResult(in CanTakeResult canTakeResult)
                {
                    return new Inventory.ItemTransformation.CanTakeResult
                    {
                        inventory = canTakeResult.Inventory,
                        itemTransformationType = canTakeResult.ItemTransformationType,
                        takenItem = (Inventory.ItemAndStackValues)canTakeResult.TakenPickup,
                    };
                }

                public static implicit operator CanTakeResult(in Inventory.ItemTransformation.CanTakeResult canTakeResult)
                {
                    return new CanTakeResult
                    {
                        Inventory = canTakeResult.inventory,
                        ItemTransformationType = canTakeResult.itemTransformationType,
                        TakenPickup = canTakeResult.takenItem,
                        ReplacementRule = PickupReplacementRule.DropExisting,
                    };
                }
            }

            public struct TakeResult
            {
                public Inventory Inventory;

                public PickupStack TakenPickup;

                public ItemTransformationTypeIndex TransformationType;

                public PickupReplacementRule ReplacementRule;

                public TryTransformResult GiveTakenPickup(PickupIndex newPickupIndex)
                {
                    PickupDef newPickupDef = PickupCatalog.GetPickupDef(newPickupIndex);
                    if (newPickupDef != null)
                    {
                        if (newPickupDef.itemIndex != ItemIndex.None)
                        {
                            Inventory.ItemTransformation.TakeResult itemTakeResult = (Inventory.ItemTransformation.TakeResult)this;
                            return itemTakeResult.GiveTakenItem(Inventory, newPickupDef.itemIndex);
                        }
                        else
                        {
                            PickupGrantParameters grantParameters = new PickupGrantParameters
                            {
                                PickupToGrant = TakenPickup.WithPickupIndex(newPickupIndex),
                                NotificationFlags = PickupNotificationFlags.None,
                                ReplacementRule = ReplacementRule
                            };

                            grantParameters.AttemptGrant(Inventory);
                        }
                    }

                    TryTransformResult tryTransformResult = new TryTransformResult
                    {
                        Inventory = Inventory,
                        TakenPickup = TakenPickup,
                        GivenPickup = newPickupIndex.isValid ? TakenPickup.WithPickupIndex(newPickupIndex) : default,
                        TransformationType = TransformationType
                    };

                    OnPickupTransformedServerGlobal?.Invoke(tryTransformResult);

                    return tryTransformResult;
                }

                public static explicit operator Inventory.ItemTransformation.TakeResult(in TakeResult takeResult)
                {
                    return new Inventory.ItemTransformation.TakeResult
                    {
                        inventory = takeResult.Inventory,
                        takenItem = (Inventory.ItemAndStackValues)takeResult.TakenPickup,
                        transformationType = takeResult.TransformationType,
                    };
                }

                public static implicit operator TakeResult(in Inventory.ItemTransformation.TakeResult takeResult)
                {
                    return new TakeResult
                    {
                        Inventory = takeResult.inventory,
                        TakenPickup = takeResult.takenItem,
                        TransformationType = takeResult.transformationType,
                        ReplacementRule = PickupReplacementRule.DropExisting
                    };
                }
            }

            public struct TryTransformResult
            {
                public Inventory Inventory;

                public PickupStack TakenPickup;

                public PickupStack GivenPickup;

                public ItemTransformationTypeIndex TransformationType;

                public readonly int TotalTransformed
                {
                    get
                    {
                        return TakenPickup.StackValues.totalStacks;
                    }
                }

                public static TryTransformResult Create()
                {
                    return new TryTransformResult
                    {
                        Inventory = null,
                        TakenPickup = PickupStack.Create(),
                        GivenPickup = PickupStack.Create(),
                        TransformationType = ItemTransformationTypeIndex.None,
                    };
                }

                public static explicit operator Inventory.ItemTransformation.TryTransformResult(in TryTransformResult tryTransformResult)
                {
                    return new Inventory.ItemTransformation.TryTransformResult
                    {
                        inventory = tryTransformResult.Inventory,
                        takenItem = (Inventory.ItemAndStackValues)tryTransformResult.TakenPickup,
                        givenItem = (Inventory.ItemAndStackValues)tryTransformResult.GivenPickup,
                        transformationType = tryTransformResult.TransformationType
                    };
                }

                public static implicit operator TryTransformResult(in Inventory.ItemTransformation.TryTransformResult tryTransformResult)
                {
                    return new TryTransformResult
                    {
                        Inventory = tryTransformResult.inventory,
                        TakenPickup = tryTransformResult.takenItem,
                        GivenPickup = tryTransformResult.givenItem,
                        TransformationType = tryTransformResult.transformationType,
                    };
                }
            }
        }

        public ref struct PickupGrantParameters
        {
            public PickupStack PickupToGrant;

            public PickupReplacementRule ReplacementRule;

            public PickupNotificationFlags NotificationFlags;

            public readonly bool AttemptGrant(Inventory inventory)
            {
                PickupIndex pickupIndex = PickupToGrant.PickupIndex;
                PickupDef pickupDef = PickupCatalog.GetPickupDef(pickupIndex);
                if (pickupDef == null)
                {
                    Log.Error("Invalid pickup index");
                    return false;
                }

                bool grantResult = false;
                if (pickupDef.itemIndex != ItemIndex.None)
                {
                    using (new Inventory.InventoryChangeScope(inventory))
                    {
                        if (PickupToGrant.StackValues.temporaryStacksValue > 0)
                        {
                            inventory.GiveItemTemp(pickupDef.itemIndex, PickupToGrant.StackValues.temporaryStacksValue);
                        }

                        if (PickupToGrant.StackValues.permanentStacks > 0)
                        {
                            inventory.GiveItemPermanent(pickupDef.itemIndex, PickupToGrant.StackValues.permanentStacks);
                        }
                    }

                    grantResult = true;
                }
                else if (pickupDef.equipmentIndex != EquipmentIndex.None)
                {
                    Vector3 dropPosition = inventory.transform.position;
                    if (inventory.TryGetComponent(out CharacterMaster master) && master.TryGetBodyPosition(out Vector3 bodyPosition))
                    {
                        dropPosition = bodyPosition;
                    }

                    int grantedEquipmentCount = 0;

                    for (int i = 0; i < PickupToGrant.StackValues.totalStacks; i++)
                    {
                        foreach (EquipmentLocation equipmentLocation in inventory.FindBestLocationsForNewEquipment())
                        {
                            if (inventory.GetEquipment(equipmentLocation.Slot, equipmentLocation.Set).equipmentIndex != EquipmentIndex.None)
                            {
                                switch (ReplacementRule)
                                {
                                    case PickupReplacementRule.KeepExisting:
                                        continue;
                                    case PickupReplacementRule.DeleteExisting:
                                        break;
                                    case PickupReplacementRule.DropExisting:
                                        PickupDropletController.CreatePickupDroplet(new UniquePickup(pickupIndex), dropPosition, (Vector3.up * 15f) + (UnityEngine.Random.insideUnitSphere * 3f), false);
                                        break;
                                }
                            }

                            inventory.SetEquipmentIndexForSlot(pickupDef.equipmentIndex, equipmentLocation.Slot, equipmentLocation.Set);
                            grantedEquipmentCount++;
                        }
                    }

                    grantResult = grantedEquipmentCount > 0;
                }
                else
                {
                    Log.Error($"Pickup {pickupIndex} is not implemented");
                    grantResult = false;
                }

                if (grantResult)
                {
                    if (NotificationFlags != PickupNotificationFlags.None && inventory.TryGetComponent(out CharacterMaster master))
                    {
                        PickupUtils.QueuePickupMessage(master, PickupToGrant.PickupIndex, NotificationFlags);
                    }
                }

                return grantResult;
            }
        }
    }
}
