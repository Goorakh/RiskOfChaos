namespace RiskOfChaos.ModifierController.SkillSlots
{
    public interface ISkillSlotModificationProvider
    {
        void ModifySkillSlot(ref SkillSlotModificationData modification);
    }
}
