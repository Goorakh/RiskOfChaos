using RoR2;
using RoR2.ExpansionManagement;
using System;
using System.Collections.Generic;
using System.Text;

namespace RiskOfChaos.Utilities
{
    public static class ExpansionUtils
    {
        public const string DLC1_NAME = "DLC1";

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
    }
}
