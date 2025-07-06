using RoR2.ExpansionManagement;
using System;

namespace RiskOfChaos.Utilities
{
    public struct SpawnPoolEntryParameters
    {
        public float Weight;
        public ExpansionIndex[] RequiredExpansions;
        public Func<bool> IsAvailableFunc;

        public SpawnPoolEntryParameters(float weight, ExpansionIndex[] requiredExpansions)
        {
            Weight = weight;
            RequiredExpansions = requiredExpansions ?? [];
        }

        public SpawnPoolEntryParameters(float weight, ExpansionIndex requiredExpansion) : this(weight, [requiredExpansion])
        {
        }

        public SpawnPoolEntryParameters(float weight) : this(weight, [])
        {
        }
    }
}
