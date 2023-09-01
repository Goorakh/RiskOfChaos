using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Methods;
using RiskOfChaos.ModifierController.SkillSlots;
using RoR2;
using System;

namespace RiskOfChaos.EffectDefinitions.Character
{
    [ChaosTimedEffect("force_activate_random_skill", 90f, DefaultSelectionWeight = 0.6f)]
    [EffectConfigBackwardsCompatibility("Effect: Force Activate Random Skill (Lasts 1 stage)")]
    public sealed class ForceActivateRandomSkill : TimedEffect, ISkillSlotModificationProvider
    {
        [EffectCanActivate]
        static bool CanActivate()
        {
            return SkillSlotModificationManager.Instance && SkillSlotModificationManager.Instance.NonLockedNonForceActivatedSkillSlots.Length > 0;
        }

        SkillSlot _forcedSkillSlot;

        public event Action OnValueDirty;

        public void ModifyValue(ref SkillSlotModificationData modification)
        {
            if (modification.SlotIndex == _forcedSkillSlot)
            {
                modification.ForceActivate = true;
            }
        }

        public override void OnPreStartServer()
        {
            base.OnPreStartServer();

            _forcedSkillSlot = RNG.NextElementUniform(SkillSlotModificationManager.Instance.NonLockedNonForceActivatedSkillSlots);
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
