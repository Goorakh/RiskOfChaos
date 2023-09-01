using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.Utilities;
using RiskOfChaos.Utilities.Extensions;
using RoR2;

namespace RiskOfChaos.EffectDefinitions.Character
{
    [ChaosTimedEffect("everyone_invisible", 30f, AllowDuplicates = false)]
    public sealed class EveryoneInvisible : TimedEffect
    {
        public override void OnStart()
        {
            CharacterBody.readOnlyInstancesList.TryDo(handleBody, FormatUtils.GetBestBodyName);

            CharacterBody.onBodyStartGlobal += handleBody;
        }

        public override void OnEnd()
        {
            CharacterBody.onBodyStartGlobal -= handleBody;

            CharacterBody.readOnlyInstancesList.TryDo(body =>
            {
                body.RemoveBuff(RoR2Content.Buffs.Cloak);
            }, FormatUtils.GetBestBodyName);
        }

        void handleBody(CharacterBody body)
        {
            body.AddBuff(RoR2Content.Buffs.Cloak);
        }
    }
}
