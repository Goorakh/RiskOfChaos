using RoR2;
using RoR2.ExpansionManagement;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using UnityEngine;

namespace RiskOfChaos.Utilities
{
    public static class ExpansionUtils
    {
        public const string DLC1_NAME = "DLC1";

        public static bool DLC1Enabled
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => IsExpansionEnabled(DLC1_NAME);
        }

        public static ExpansionDef DLC1
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => FindExpansionDef(DLC1_NAME);
        }

        public static ExpansionDef FindExpansionDef(string name)
        {
            foreach (ExpansionDef expansion in ExpansionCatalog.expansionDefs)
            {
                if (expansion.name == name)
                    return expansion;
            }

            return null;
        }

        public static bool IsExpansionEnabled(string name)
        {
            ExpansionDef expansionDef = FindExpansionDef(name);
            return expansionDef && Run.instance && Run.instance.IsExpansionEnabled(expansionDef);
        }

        public static bool IsObjectExpansionAvailable(GameObject obj)
        {
            if (!obj || !Run.instance)
                return false;

            return obj.GetComponents<ExpansionRequirementComponent>().All(requirement =>
            {
                return !requirement.requiredExpansion || Run.instance.IsExpansionEnabled(requirement.requiredExpansion);
            });
        }
    }
}
