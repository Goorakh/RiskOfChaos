using RiskOfChaos.EffectHandling;
using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Data;
using RoR2.Skills;
using System.Collections.Generic;

namespace RiskOfChaos.EffectDefinitions.Character
{
    [ChaosEffect("all_skills_agile", EffectStageActivationCountHardCap = 1, IsNetworked = true)]
    [ChaosTimedEffect(TimedEffectType.UntilStageEnd)]
    [EffectConfigBackwardsCompatibility("Effect: All Skills are Agile (Lasts 1 stage)")]
    public sealed class AllSkillsAgile : TimedEffect
    {
        [InitEffectInfo]
        public static readonly ChaosEffectInfo EffectInfo;

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
