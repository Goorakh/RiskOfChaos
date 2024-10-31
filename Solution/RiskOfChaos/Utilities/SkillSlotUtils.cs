using RoR2;

namespace RiskOfChaos.Utilities
{
    public static class SkillSlotUtils
    {
        public const SkillSlot MaxSlot = SkillSlot.Special;

        public const int SkillSlotCount = (int)MaxSlot + 1;

        public const int ValidSkillSlotMask = (1 << SkillSlotCount) - 1;

        public static uint GetSlotBitMask(SkillSlot skillSlot)
        {
            if (skillSlot < 0 || skillSlot > MaxSlot)
                return 0U;

            return 1U << (int)skillSlot;
        }

        public static bool GetSkillSlotBit(uint mask, SkillSlot skillSlot)
        {
            uint bitMask = GetSlotBitMask(skillSlot);
            return bitMask != 0 && (mask & bitMask) == bitMask;
        }

        public static uint SetSkillSlotBit(uint mask, SkillSlot skillSlot, bool bitValue)
        {
            uint bitMask = GetSlotBitMask(skillSlot);
            if (bitMask != 0)
            {
                if (bitValue)
                {
                    mask |= bitMask;
                }
                else
                {
                    mask &= ~bitMask;
                }
            }

            return mask;
        }
    }
}
