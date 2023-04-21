using RiskOfChaos.EffectHandling;
using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Methods;
using RiskOfChaos.ModifierController.SkillSlots;
using RoR2;

namespace RiskOfChaos.EffectDefinitions.Character.Player
{
    [ChaosEffect("lock_random_skill", DefaultSelectionWeight = 0.3f, EffectWeightReductionPercentagePerActivation = 80f)]
    public sealed class LockRandomSkill : TimedEffect, ISkillSlotModificationProvider
    {
        [EffectCanActivate]
        static bool CanActivate()
        {
            return SkillSlotModificationManager.Instance && SkillSlotModificationManager.Instance.NonLockedSkillSlots.Length > 0;
        }

        public override TimedEffectType TimedType => TimedEffectType.UntilStageEnd;

        SkillSlot _lockedSkillSlot;

        public void ModifySkillSlot(ref SkillSlotModificationData modification)
        {
            if (modification.SlotIndex == _lockedSkillSlot)
            {
                modification.ForceIsLocked = true;
            }
        }

        public override void OnPreStartServer()
        {
            base.OnPreStartServer();

            _lockedSkillSlot = RNG.NextElementUniform(SkillSlotModificationManager.Instance.NonLockedSkillSlots);
        }

        public override void OnStart()
        {
            SkillSlotModificationManager.Instance.RegisterModificationProvider(this);
        }

        public override void OnEnd()
        {
            if (SkillSlotModificationManager.Instance)
            {
                SkillSlotModificationManager.Instance.UnregisterModificationProvider(this);
            }
        }
    }
}
