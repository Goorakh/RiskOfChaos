using RoR2;
using System;

namespace RiskOfChaos.Utilities
{
    public struct PickupStack : IEquatable<PickupStack>
    {
        public PickupIndex PickupIndex;

        public Inventory.ItemStackValues StackValues;

        public PickupStack(PickupIndex pickupIndex, Inventory.ItemStackValues stackValues)
        {
            PickupIndex = pickupIndex;
            StackValues = stackValues;
        }

        public readonly PickupStack WithPickupIndex(PickupIndex pickupIndex)
        {
            PickupStack stack = this;
            stack.PickupIndex = pickupIndex;
            return stack;
        }

        public static PickupStack Create()
        {
            return new PickupStack
            {
                PickupIndex = PickupIndex.none,
                StackValues = Inventory.ItemStackValues.Create(),
            };
        }

        public override readonly bool Equals(object obj)
        {
            return obj is PickupStack stack && Equals(stack);
        }

        public readonly bool Equals(in PickupStack other)
        {
            return PickupIndex.Equals(other.PickupIndex) &&
                   StackValues.Equals(other.StackValues);
        }

        public override readonly int GetHashCode()
        {
            return HashCode.Combine(PickupIndex, StackValues);
        }

        readonly bool IEquatable<PickupStack>.Equals(PickupStack other)
        {
            return Equals(other);
        }

        public static bool operator ==(in PickupStack left, in PickupStack right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(in PickupStack left, in PickupStack right)
        {
            return !left.Equals(right);
        }

        public static explicit operator Inventory.ItemAndStackValues(in PickupStack pickupStack)
        {
            PickupDef pickupDef = PickupCatalog.GetPickupDef(pickupStack.PickupIndex);

            return new Inventory.ItemAndStackValues
            {
                itemIndex = pickupDef?.itemIndex ?? ItemIndex.None,
                stackValues = pickupStack.StackValues
            };
        }

        public static implicit operator PickupStack(in Inventory.ItemAndStackValues itemAndStackValues)
        {
            return new PickupStack
            {
                PickupIndex = PickupCatalog.FindPickupIndex(itemAndStackValues.itemIndex),
                StackValues = itemAndStackValues.stackValues
            };
        }
    }
}
