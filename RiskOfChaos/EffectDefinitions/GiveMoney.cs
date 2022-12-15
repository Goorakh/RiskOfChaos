using RiskOfChaos.EffectHandling;
using RoR2;
using System;
using System.Collections.Generic;
using System.Text;

namespace RiskOfChaos.EffectDefinitions
{
    [ChaosEffect("GiveMoney", EffectRepetitionWeightExponent = 0f)]
    public class GiveMoney : BaseEffect
    {
        public override void OnStart()
        {
            uint amount = (uint)Run.instance.GetDifficultyScaledCost(200);

            foreach (CharacterMaster master in PlayerUtils.GetAllPlayerMasters(false))
            {
                master.GiveMoney(amount);
            }
        }
    }
}
