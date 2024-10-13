using HG;
using System;

namespace RiskOfChaos.OLD_ModifierController.SkillSlots
{
    internal struct SkillSlotStockAddsWrapper : IEquatable<SkillSlotStockAddsWrapper>
    {
        public static readonly SkillSlotStockAddsWrapper Default = new SkillSlotStockAddsWrapper(new int[SkillSlotModificationManager.SKILL_SLOT_COUNT]);

        public int[] Values;

        SkillSlotStockAddsWrapper(int[] values)
        {
            if (values.Length != SkillSlotModificationManager.SKILL_SLOT_COUNT)
                throw new ArgumentException($"Values must have a size of {SkillSlotModificationManager.SKILL_SLOT_COUNT}");

            Values = values;
        }

        public readonly bool Equals(SkillSlotStockAddsWrapper other)
        {
            return ArrayUtils.SequenceEquals(Values, other.Values);
        }

        public static implicit operator int[](SkillSlotStockAddsWrapper wrapper)
        {
            return wrapper.Values;
        }

        public static implicit operator SkillSlotStockAddsWrapper(int[] values)
        {
            return new SkillSlotStockAddsWrapper(values);
        }
    }
}
