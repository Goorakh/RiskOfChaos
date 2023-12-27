using RiskOfChaos.Components.CostProviders;
using RiskOfChaos.Utilities.Interpolation;
using RoR2;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace RiskOfChaos.ModifierController.Cost
{
    public struct CostModificationInfo : IEquatable<CostModificationInfo>
    {
        public readonly ICostProvider OriginalCostProvider;

        public CostTypeIndex CostType;
        public float CostMultiplier;

        public readonly int CurrentCostRounded => Mathf.RoundToInt(OriginalCostProvider.Cost * CostMultiplier);

        public readonly float CurrentCost => OriginalCostProvider.Cost * CostMultiplier;

        public CostModificationInfo(ICostProvider costProvider)
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
                CostMultiplier = interpolationType.Interpolate(a.CostMultiplier, b.CostMultiplier, t)
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
                   CostMultiplier == other.CostMultiplier;
        }

        public override readonly int GetHashCode()
        {
            int hashCode = 1229816820;
            hashCode = (hashCode * -1521134295) + EqualityComparer<ICostProvider>.Default.GetHashCode(OriginalCostProvider);
            hashCode = (hashCode * -1521134295) + CostType.GetHashCode();
            hashCode = (hashCode * -1521134295) + CostMultiplier.GetHashCode();
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
