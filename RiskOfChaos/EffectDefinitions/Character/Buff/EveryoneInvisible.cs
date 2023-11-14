using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Methods;
using RoR2;

namespace RiskOfChaos.EffectDefinitions.Character.Buff
{
    [ChaosTimedEffect("everyone_invisible", 30f, AllowDuplicates = false)]
    public sealed class EveryoneInvisible : ApplyBuffEffect
    {
        [EffectCanActivate]
        static bool CanActivate()
        {
            return canSelectBuff(RoR2Content.Buffs.Cloak.buffIndex);
        }

        protected override BuffIndex getBuffIndexToApply()
        {
            return RoR2Content.Buffs.Cloak.buffIndex;
        }
    }
}
