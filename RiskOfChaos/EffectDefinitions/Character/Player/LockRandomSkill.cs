using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Methods;
using RiskOfChaos.Patches;
using RoR2;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Networking;

namespace RiskOfChaos.EffectDefinitions.Character.Player
{
    [ChaosEffect("lock_random_skill", DefaultSelectionWeight = 0.3f, EffectWeightReductionPercentagePerActivation = 80f, IsNetworked = true)]
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

        SkillSlot _lockedSkillSlot;

        public override void Serialize(NetworkWriter writer)
        {
            base.Serialize(writer);
            writer.Write((sbyte)_lockedSkillSlot);
        }

        public override void Deserialize(NetworkReader reader)
        {
            base.Deserialize(reader);
            _lockedSkillSlot = (SkillSlot)reader.ReadSByte();
        }

        public override void OnStart()
        {
            if (NetworkServer.active)
            {
                _lockedSkillSlot = RNG.NextElementUniform(getNonLockedSlots().ToList());
            }

            ForceLockPlayerSkillSlot.SetSkillSlotLocked(_lockedSkillSlot);
        }
    }
}
