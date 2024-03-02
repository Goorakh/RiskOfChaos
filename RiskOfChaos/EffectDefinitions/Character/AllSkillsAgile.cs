using RiskOfChaos.EffectHandling;
using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Data;
using RiskOfChaos.EffectUtils.Character.AllSkillsAgile;

namespace RiskOfChaos.EffectDefinitions.Character
{
    [ChaosTimedEffect("all_skills_agile", TimedEffectType.UntilStageEnd, AllowDuplicates = false, IsNetworked = true)]
    [EffectConfigBackwardsCompatibility("Effect: All Skills are Agile (Lasts 1 stage)")]
    public sealed class AllSkillsAgile : TimedEffect
    {
        [InitEffectInfo]
        public static readonly new TimedEffectInfo EffectInfo;

        public override void OnStart()
        {
            OverrideSkillsAgile.AllSkillsAgileCount++;
        }

        public override void OnEnd()
        {
            OverrideSkillsAgile.AllSkillsAgileCount--;
        }
    }
}
