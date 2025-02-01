using R2API;
using RiskOfChaos.EffectDefinitions.Character;
using RoR2;

namespace RiskOfChaos.Content
{
    public static class DeployableSlots
    {
        public static readonly DeployableSlot PoisonTrailSegment;
        static int getPoisonTrailSameSlotLimit(CharacterMaster master, int deployableCountMultiplier)
        {
            return PoisonTrail.GetPoisonTrailSegmentLimit(master);
        }

        static DeployableSlots()
        {
            PoisonTrailSegment = DeployableAPI.RegisterDeployableSlot(getPoisonTrailSameSlotLimit);
        }
    }
}
