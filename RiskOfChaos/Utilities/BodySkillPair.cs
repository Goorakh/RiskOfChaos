using RoR2;

namespace RiskOfChaos.Utilities
{
    public readonly record struct BodySkillPair(BodyIndex BodyIndex, SkillSlot SkillSlot)
    {
        public BodySkillPair(string bodyName, SkillSlot slot) : this(BodyCatalog.FindBodyIndex(bodyName), slot)
        {
        }
    }
}
