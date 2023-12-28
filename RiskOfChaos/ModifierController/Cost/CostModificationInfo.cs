using RiskOfChaos.Components.CostProviders;
using RiskOfChaos.Utilities.Interpolation;
using RoR2;
using System;
using System.Collections.Generic;

namespace RiskOfChaos.ModifierController.Cost
{
    public struct CostModificationInfo : IEquatable<CostModificationInfo>
    {
        public readonly OriginalCostProvider OriginalCostProvider;

        public CostTypeIndex CostType;
        public float CostMultiplier;
        public bool? AllowZeroCostResultOverride;

        public readonly bool AllowZeroCostResult => AllowZeroCostResultOverride ?? CostType switch
        {
            CostTypeIndex.Money or CostTypeIndex.PercentHealth or CostTypeIndex.LunarCoin or CostTypeIndex.VoidCoin => true,
            _ => false
        };

        public readonly float CurrentCost => OriginalCostProvider.Cost * CostMultiplier;

        public CostModificationInfo(OriginalCostProvider costProvider)
        {
            OriginalCostProvider = costProvider;

            CostType = costProvider.CostType;
            CostMultiplier = 1f;
        }

        public static CostModificationInfo Interpolate(in CostModificationInfo a, in CostModificationInfo b, float t, ValueInterpolationFunctionType interpolationType)
        {
            if (a.OriginalCostProvider != b.OriginalCostProvider)
            {
                Log.Error("Cannot interpolate differing cost providers");
                return b;
            }

            return new CostModificationInfo(b.OriginalCostProvider)
            {
                CostMultiplier = interpolationType.Interpolate(a.CostMultiplier, b.CostMultiplier, t),
                CostType = b.CostType,
                AllowZeroCostResultOverride = b.AllowZeroCostResultOverride
            };
        }

        public override readonly bool Equals(object obj)
        {
            return obj is CostModificationInfo info && Equals(info);
        }

        public readonly bool Equals(CostModificationInfo other)
        {
            return EqualityComparer<ICostProvider>.Default.Equals(OriginalCostProvider, other.OriginalCostProvider) &&
                   CostType == other.CostType &&
                   CostMultiplier == other.CostMultiplier &&
                   AllowZeroCostResultOverride == other.AllowZeroCostResultOverride;
        }

        public override readonly int GetHashCode()
        {
            int hashCode = 1229816820;
            hashCode = (hashCode * -1521134295) + EqualityComparer<ICostProvider>.Default.GetHashCode(OriginalCostProvider);
            hashCode = (hashCode * -1521134295) + CostType.GetHashCode();
            hashCode = (hashCode * -1521134295) + CostMultiplier.GetHashCode();
            hashCode = (hashCode * -1521134295) + AllowZeroCostResultOverride.GetHashCode();
            return hashCode;
        }

        public static bool operator ==(in CostModificationInfo left, in CostModificationInfo right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(in CostModificationInfo left, in CostModificationInfo right)
        {
            return !(left == right);
        }
    }
}
