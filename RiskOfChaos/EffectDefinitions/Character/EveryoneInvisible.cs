using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.Utilities.Extensions;
using RoR2;

namespace RiskOfChaos.EffectDefinitions.Character
{
    [ChaosEffect("everyone_invisible")]
    [ChaosTimedEffect(30f, AllowDuplicates = false)]
    public sealed class EveryoneInvisible : TimedEffect
    {
        public override void OnStart()
        {
            CharacterBody.readOnlyInstancesList.TryDo(handleBody);

            CharacterBody.onBodyStartGlobal += handleBody;
        }

        public override void OnEnd()
        {
            CharacterBody.onBodyStartGlobal -= handleBody;

            CharacterBody.readOnlyInstancesList.TryDo(body =>
            {
                body.RemoveBuff(RoR2Content.Buffs.Cloak);
            });
        }

        void handleBody(CharacterBody body)
        {
            body.AddBuff(RoR2Content.Buffs.Cloak);
        }
    }
}
