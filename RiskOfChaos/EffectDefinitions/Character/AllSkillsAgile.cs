using RiskOfChaos.EffectHandling;
using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Data;
using RiskOfChaos.Utilities.Extensions;
using RoR2.Skills;
using System.Collections.Generic;

namespace RiskOfChaos.EffectDefinitions.Character
{
    [ChaosTimedEffect("all_skills_agile", TimedEffectType.UntilStageEnd, AllowDuplicates = false, IsNetworked = true)]
    [EffectConfigBackwardsCompatibility("Effect: All Skills are Agile (Lasts 1 stage)")]
    public sealed class AllSkillsAgile : TimedEffect
    {
        [InitEffectInfo]
        public static readonly new TimedEffectInfo EffectInfo;

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
            SkillCatalog.allSkillDefs.TryDo(skillDef =>
            {
                if (skillDef.cancelSprintingOnActivation || skillDef.canceledFromSprinting)
                {
                    _skillIsAgileOverrides.Add(new SkillDefIsAgileOverride(skillDef));
                }
            });
        }

        public override void OnEnd()
        {
            _skillIsAgileOverrides.TryDo(isAgileOverride => isAgileOverride.Undo());
            _skillIsAgileOverrides.Clear();
        }
    }
}
