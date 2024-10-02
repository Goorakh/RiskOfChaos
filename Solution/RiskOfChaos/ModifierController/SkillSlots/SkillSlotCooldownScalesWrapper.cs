using HG;
using System;

namespace RiskOfChaos.ModifierController.SkillSlots
{
    internal struct SkillSlotCooldownScalesWrapper : IEquatable<SkillSlotCooldownScalesWrapper>
    {
        public float[] Values;

        SkillSlotCooldownScalesWrapper(float[] values)
        {
            if (values.Length != SkillSlotModificationManager.SKILL_SLOT_COUNT)
                throw new ArgumentException($"Values must have a size of {SkillSlotModificationManager.SKILL_SLOT_COUNT}");

            Values = values;
        }

        public readonly bool Equals(SkillSlotCooldownScalesWrapper other)
        {
            return ArrayUtils.SequenceEquals(Values, other.Values);
        }

        public static implicit operator float[](SkillSlotCooldownScalesWrapper wrapper)
        {
            return wrapper.Values;
        }

        public static implicit operator SkillSlotCooldownScalesWrapper(float[] values)
        {
            return new SkillSlotCooldownScalesWrapper(values);
        }
    }
}
