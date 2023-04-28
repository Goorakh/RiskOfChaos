using RiskOfChaos.EffectHandling;
using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Methods;
using RiskOfChaos.ModifierController.SkillSlots;
using RoR2;
using System;
using System.Collections.Generic;
using System.Text;

namespace RiskOfChaos.EffectDefinitions.Character
{
    [ChaosEffect("force_activate_random_skill", DefaultSelectionWeight = 0.3f, EffectWeightReductionPercentagePerActivation = 80f)]
    public sealed class ForceActivateRandomSkill : TimedEffect, ISkillSlotModificationProvider
    {
        [EffectCanActivate]
        static bool CanActivate()
        {
            return SkillSlotModificationManager.Instance && SkillSlotModificationManager.Instance.NonLockedNonForceActivatedSkillSlots.Length > 0;
        }

        public override TimedEffectType TimedType => TimedEffectType.UntilStageEnd;

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
