using RiskOfChaos.Utilities.Interpolation;
using RoR2;

namespace RiskOfChaos.ModifierController.SkillSlots
{
    public struct SkillSlotModificationData
    {
        public readonly SkillSlot SlotIndex;

        public bool ForceIsLocked = false;
        public bool ForceActivate = false;

        public float CooldownScale = 1f;
        public int StockAdds = 0;

        public SkillSlotModificationData(SkillSlot slotIndex)
        {
            SlotIndex = slotIndex;
        }

        public static SkillSlotModificationData Interpolate(SkillSlotModificationData a, SkillSlotModificationData b, float t, ValueInterpolationFunctionType interpolationType)
        {
            if (a.SlotIndex != b.SlotIndex)
            {
                Log.Error("Attempting to interpolate modification data for different skill slots");
            }

            return new SkillSlotModificationData(a.SlotIndex)
            {
                ForceIsLocked = b.ForceIsLocked,
                ForceActivate = b.ForceActivate,
                CooldownScale = interpolationType.Interpolate(a.CooldownScale, b.CooldownScale, t),
                StockAdds = interpolationType.Interpolate(a.StockAdds, b.StockAdds, t)
            };
        }
    }
}
