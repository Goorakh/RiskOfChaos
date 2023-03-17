using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RoR2;

namespace RiskOfChaos.EffectDefinitions.Character
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
                    ref bool canBeFrozen = ref setStateOnHurt.canBeFrozen;
                    bool originalCanBeFrozen = canBeFrozen;

                    canBeFrozen = true;
                    setStateOnHurt.SetFrozen(4f);
                    canBeFrozen = originalCanBeFrozen;
                }
            }
        }
    }
}
