using RoR2.ExpansionManagement;

namespace RiskOfChaos.Utilities
{
    public struct SpawnPoolEntryParameters
    {
        public float Weight;
        public ExpansionDef[] RequiredExpansions;

        public SpawnPoolEntryParameters(float weight, ExpansionDef[] requiredExpansions)
        {
            Weight = weight;
            RequiredExpansions = requiredExpansions ?? [];
        }

        public SpawnPoolEntryParameters(float weight, ExpansionDef requiredExpansion) : this(weight, [requiredExpansion])
        {
        }

        public SpawnPoolEntryParameters(float weight) : this(weight, [])
        {
        }
    }
}
