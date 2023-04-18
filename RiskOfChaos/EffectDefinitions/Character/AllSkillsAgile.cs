using RiskOfChaos.EffectHandling;
using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RoR2.Skills;
using System.Collections.Generic;

namespace RiskOfChaos.EffectDefinitions.Character
{
    [ChaosEffect("all_skills_agile", EffectStageActivationCountHardCap = 1, IsNetworked = true)]
    public sealed class AllSkillsAgile : TimedEffect
    {
        readonly struct SkillDefIsAgileOverride
        {
            public readonly SkillDef SkillDef;

            public readonly bool OriginalCancelSprintingOnActivation;
            public readonly bool OriginalCanceledFromSprinting;

            public SkillDefIsAgileOverride(SkillDef skillDef)
            {
                SkillDef = skillDef;

                OriginalCancelSprintingOnActivation = SkillDef.cancelSprintingOnActivation;
                SkillDef.cancelSprintingOnActivation = false;

                OriginalCanceledFromSprinting = SkillDef.canceledFromSprinting;
                SkillDef.canceledFromSprinting = false;
            }

            public readonly void Undo()
            {
                if (!SkillDef)
                    return;

                SkillDef.cancelSprintingOnActivation = OriginalCancelSprintingOnActivation;
                SkillDef.canceledFromSprinting = OriginalCanceledFromSprinting;
            }
        }

        public override TimedEffectType TimedType => TimedEffectType.UntilStageEnd;

        readonly List<SkillDefIsAgileOverride> _skillIsAgileOverrides = new List<SkillDefIsAgileOverride>();

        public override void OnStart()
        {
            foreach (SkillDef skillDef in SkillCatalog.allSkillDefs)
            {
                if (skillDef.cancelSprintingOnActivation || skillDef.canceledFromSprinting)
                {
                    _skillIsAgileOverrides.Add(new SkillDefIsAgileOverride(skillDef));
                }
            }
        }

        public override void OnEnd()
        {
            foreach (SkillDefIsAgileOverride isAgileOverride in _skillIsAgileOverrides)
            {
                isAgileOverride.Undo();
            }

            _skillIsAgileOverrides.Clear();
        }
    }
}
