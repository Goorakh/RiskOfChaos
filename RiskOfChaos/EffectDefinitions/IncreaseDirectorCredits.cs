using RiskOfChaos.EffectHandling;
using RoR2;
using System;
using System.Collections.Generic;
using System.Text;

namespace RiskOfChaos.EffectDefinitions
{
    [ChaosEffect("IncreaseDirectorCredits", EffectRepetitionWeightExponent = 50f)]
    public class IncreaseDirectorCredits : BaseEffect
    {
        [EffectCanActivate]
        static bool CanActivate()
        {
            return CombatDirector.instancesList.Count > 0;
        }

        public override void OnStart()
        {
            foreach (CombatDirector director in CombatDirector.instancesList)
            {
#pragma warning disable Publicizer001 // Accessing a member that was not originally public
                foreach (CombatDirector.DirectorMoneyWave moneyWave in director.moneyWaves)
#pragma warning restore Publicizer001 // Accessing a member that was not originally public
                {
                    moneyWave.multiplier *= 1.5f;
                }
            }
        }
    }
}
