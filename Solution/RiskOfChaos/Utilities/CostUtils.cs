using RoR2;
using UnityEngine;

namespace RiskOfChaos.Utilities
{
    public static class CostUtils
    {
        public static bool AllowsZeroCost(CostTypeIndex costType)
        {
            switch (costType)
            {
                case CostTypeIndex.None:
                case CostTypeIndex.Money:
                case CostTypeIndex.PercentHealth:
                case CostTypeIndex.LunarCoin:
                case CostTypeIndex.VoidCoin:
                case CostTypeIndex.SoulCost:
                    return true;
                default:
                    return false;
            }
        }

        public static int GetMaxCost(CostTypeIndex costType)
        {
            switch (costType)
            {
                case CostTypeIndex.PercentHealth:
                case CostTypeIndex.SoulCost:
                    return 99;
                case CostTypeIndex.Equipment:
                case CostTypeIndex.VolatileBattery:
                    return 1;
                default:
                    return int.MaxValue;
            }
        }

        public static int GetMinCost(CostTypeIndex costType)
        {
            switch (costType)
            {
                case CostTypeIndex.None:
                    return 0;
                case CostTypeIndex.SoulCost:
                    return 10;
                default:
                    return 1;
            }
        }

        public static float ConvertCost(float cost, CostTypeIndex from, CostTypeIndex to)
        {
            if (from == to)
                return cost;

            if (to <= CostTypeIndex.None || (int)to >= CostTypeCatalog.costTypeCount)
                return 0f;

            if (from <= CostTypeIndex.None || (int)from >= CostTypeCatalog.costTypeCount)
                return GetMinCost(to);

            int fromCostMin = GetMinCost(from);
            int fromCostMax = GetMaxCost(from);

            int toCostMax = GetMaxCost(to);
            int toCostMin = GetMinCost(to);

            if (fromCostMax < int.MaxValue && fromCostMin != fromCostMax)
            {
                float costFraction = Mathf.Clamp01(Mathf.InverseLerp(fromCostMin, fromCostMax, cost));
                cost = fromCostMin - ((50f - fromCostMin) * (1f + (1f / (costFraction - 1f))));
            }

            static float getConversionRate(CostTypeIndex costType)
            {
                switch (costType)
                {
                    case CostTypeIndex.WhiteItem:
                        return 25;
                    case CostTypeIndex.GreenItem:
                    case CostTypeIndex.Equipment:
                    case CostTypeIndex.VolatileBattery:
                    case CostTypeIndex.LunarCoin:
                    case CostTypeIndex.VoidCoin:
                        return 50;
                    case CostTypeIndex.TreasureCacheItem:
                        return 75;
                    case CostTypeIndex.LunarItemOrEquipment:
                        return 100;
                    case CostTypeIndex.RedItem:
                    case CostTypeIndex.BossItem:
                    case CostTypeIndex.ArtifactShellKillerItem:
                    case CostTypeIndex.TreasureCacheVoidItem:
                        return 150;
                    default:
                        return 1;
                }
            }

            cost *= getConversionRate(from) / getConversionRate(to);

            if (toCostMax < int.MaxValue && toCostMin != toCostMax)
            {
                // cost = toCostMin - ((50f - toCostMin) * (1f + (1f / (costFraction - 1f))))
                // (50f - toCostMin) * (1f + (1f / (costFraction - 1f))) = toCostMin - cost
                // 1f + (1f / (costFraction - 1f)) = (toCostMin - cost) / (50f - toCostMin)
                // 1f / (costFraction - 1f) = (toCostMin - cost) / (50f - toCostMin) - 1f
                // 1f / ((toCostMin - cost) / (50f - toCostMin) - 1f) = costFraction - 1f
                // 1f + 1f / ((toCostMin - cost) / (50f - toCostMin) - 1f) = costFraction

                float costFraction = 1f + (1f / (((toCostMin - cost) / (50f - toCostMin)) - 1f));

                cost = Mathf.Lerp(toCostMin, toCostMax, costFraction);
            }

            cost = Mathf.Clamp(cost, toCostMin, toCostMax);

            return cost;
        }
    }
}