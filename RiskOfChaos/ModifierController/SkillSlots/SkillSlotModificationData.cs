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
    }
}
