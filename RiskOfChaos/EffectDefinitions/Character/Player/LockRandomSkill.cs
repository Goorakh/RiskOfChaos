using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Methods;
using RiskOfChaos.Patches;
using RoR2;
using System.Collections.Generic;
using System.Linq;

namespace RiskOfChaos.EffectDefinitions.Character.Player
{
    [ChaosEffect("lock_random_skill", DefaultSelectionWeight = 0.3f, EffectWeightReductionPercentagePerActivation = 80f)]
    public sealed class LockRandomSkill : BaseEffect
    {
        [EffectCanActivate]
        static bool CanActivate()
        {
            return getNonLockedSlots().Any();
        }

        static IEnumerable<SkillSlot> getNonLockedSlots()
        {
            for (SkillSlot i = 0; i < (SkillSlot)ForceLockPlayerSkillSlot.SKILL_SLOT_COUNT; i++)
            {
                if (!ForceLockPlayerSkillSlot.IsSkillSlotLocked(i))
                {
                    yield return i;
                }
            }
        }

        public override void OnStart()
        {
            SkillSlot skillSlotToLock = RNG.NextElementUniform(getNonLockedSlots().ToList());
            ForceLockPlayerSkillSlot.SetSkillSlotLocked(skillSlotToLock);
        }
    }
}
