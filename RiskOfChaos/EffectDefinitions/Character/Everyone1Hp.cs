using RiskOfChaos.EffectDefinitions.Character.Buff;
using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Methods;
using RiskOfChaos.Utilities;
using RoR2;

namespace RiskOfChaos.EffectDefinitions.Character
{
    [ChaosEffect("everyone_1hp")]
    [ChaosTimedEffect(30f, AllowDuplicates = false)]
    public sealed class Everyone1Hp : ApplyBuffEffect
    {
        [EffectCanActivate]
        static bool CanActivate()
        {
            return canSelectBuff(RoR2Content.Buffs.PermanentCurse.buffIndex);
        }

        protected override BuffIndex getBuffIndexToApply()
        {
            return RoR2Content.Buffs.PermanentCurse.buffIndex;
        }

        protected override int buffCount => int.MaxValue;

        public override void OnStart()
        {
            foreach (CharacterBody playerBody in PlayerUtils.GetAllPlayerBodies(true))
            {
                playerBody.AddTimedBuff(RoR2Content.Buffs.HiddenInvincibility, 1f);
            }

            base.OnStart();
        }
    }
}
