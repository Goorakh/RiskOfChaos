using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RoR2;
using System.Linq;

namespace RiskOfChaos.EffectDefinitions.Character
{
    [ChaosEffect("kill_all", DefaultSelectionWeight = 0.7f, EffectWeightReductionPercentagePerActivation = 20f)]
    public sealed class KillAll : BaseEffect
    {
        public override void OnStart()
        {
            foreach (CharacterMaster master in CharacterMaster.readOnlyInstancesList.ToList())
            {
                if (!master || master.isBoss || master.playerCharacterMasterController)
                    continue;

                CharacterBody body = master.GetBody();
                if (!body)
                    continue;

                switch (body.teamComponent.teamIndex)
                {
                    case TeamIndex.Neutral:
                    case TeamIndex.Monster:
                    case TeamIndex.Lunar:
                    case TeamIndex.Void:
                        HealthComponent healthComponent = body.healthComponent;
                        if (healthComponent)
                        {
                            healthComponent.Suicide();
                        }

                        break;
                }
            }
        }
    }
}
