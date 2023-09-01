using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Methods;
using RiskOfChaos.ModifierController.SkillSlots;
using RoR2;
using System;

namespace RiskOfChaos.EffectDefinitions.Character.Player
{
    [ChaosTimedEffect("lock_random_skill", 90f, DefaultSelectionWeight = 0.5f)]
    [EffectConfigBackwardsCompatibility("Effect: Disable Random Skill (Lasts 1 stage)")]
    public sealed class LockRandomSkill : TimedEffect, ISkillSlotModificationProvider
    {
        [EffectCanActivate]
        static bool CanActivate()
        {
            return SkillSlotModificationManager.Instance && SkillSlotModificationManager.Instance.NonLockedSkillSlots.Length > 0;
        }

        SkillSlot _lockedSkillSlot;

        public event Action OnValueDirty;

        public void ModifyValue(ref SkillSlotModificationData modification)
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
