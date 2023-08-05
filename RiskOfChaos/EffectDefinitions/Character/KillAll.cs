using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RoR2;
using System;

namespace RiskOfChaos.EffectDefinitions.Character
{
    [ChaosEffect("kill_all", DefaultSelectionWeight = 0.7f, EffectWeightReductionPercentagePerActivation = 20f)]
    public sealed class KillAll : BaseEffect
    {
        public override void OnStart()
        {
            for (int i = CharacterMaster.readOnlyInstancesList.Count - 1; i >= 0; i--)
            {
                CharacterMaster master = CharacterMaster.readOnlyInstancesList[i];
                if (!master || master.isBoss || master.playerCharacterMasterController)
                    continue;

                CharacterBody body = master.GetBody();
                if (!body)
                    continue;

                try
                {
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
                catch (Exception ex)
                {
                    Log.Error_NoCallerPrefix($"Failed to kill {Util.GetBestMasterName(master)}: {ex}");
                }
            }
        }
    }
}
