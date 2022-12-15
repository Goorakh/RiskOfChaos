using RiskOfChaos.EffectHandling;
using RoR2;
using System;
using System.Collections.Generic;
using System.Text;

namespace RiskOfChaos.EffectDefinitions
{
    [ChaosEffect("FreezeAll")]
    public class FreezeAll : BaseEffect
    {
        public override void OnStart()
        {
            foreach (CharacterBody body in CharacterBody.readOnlyInstancesList)
            {
                if (body && body.TryGetComponent(out SetStateOnHurt setStateOnHurt))
                {
                    bool originalCanBeFrozen = setStateOnHurt.canBeFrozen;

                    setStateOnHurt.canBeFrozen = true;
                    setStateOnHurt.SetFrozen(4f);
                    setStateOnHurt.canBeFrozen = originalCanBeFrozen;
                }
            }
        }
    }
}
