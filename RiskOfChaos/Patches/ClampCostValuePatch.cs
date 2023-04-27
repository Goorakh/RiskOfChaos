using HarmonyLib;
using MonoMod.RuntimeDetour;
using RoR2;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace RiskOfChaos.Patches
{
    static class ClampCostValuePatch
    {
        delegate void orig_CostSetter<T>(T self, int value);

        [SystemInitializer]
        static void Init()
        {
            new Hook(AccessTools.DeclaredPropertySetter(typeof(PurchaseInteraction), nameof(PurchaseInteraction.Networkcost)), PurchaseInteraction_set_Networkcost);
            new Hook(AccessTools.DeclaredPropertySetter(typeof(MultiShopController), nameof(MultiShopController.Networkcost)), MultiShopController_set_Networkcost);
        }

        static int clampCostValue(int cost, CostTypeIndex costType)
        {
            if (cost <= 0)
            {
                switch (costType)
                {
                    case CostTypeIndex.Money:
                    case CostTypeIndex.PercentHealth:
                    case CostTypeIndex.LunarCoin:
                    case CostTypeIndex.VoidCoin:
                        return 0;
                    default:
                        return 1;
                }
            }

            if (costType == CostTypeIndex.PercentHealth)
            {
                return Mathf.Min(cost, 99);
            }

            return cost;
        }

        static void PurchaseInteraction_set_Networkcost(orig_CostSetter<PurchaseInteraction> orig, PurchaseInteraction self, int value)
        {
            orig(self, clampCostValue(value, self.costType));
        }

        static void MultiShopController_set_Networkcost(orig_CostSetter<MultiShopController> orig, MultiShopController self, int value)
        {
            orig(self, clampCostValue(value, self.costType));
        }
    }
}
