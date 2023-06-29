using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.Utilities;
using RiskOfChaos.Utilities.Extensions;
using RoR2;

namespace RiskOfChaos.EffectDefinitions.Character
{
    [ChaosEffect("freeze_all")]
    public sealed class FreezeAll : BaseEffect
    {
        public override void OnStart()
        {
            CharacterBody.readOnlyInstancesList.TryDo(body =>
            {
                if (body && body.TryGetComponent(out SetStateOnHurt setStateOnHurt))
                {
                    ref bool canBeFrozen = ref setStateOnHurt.canBeFrozen;
                    bool originalCanBeFrozen = canBeFrozen;

                    canBeFrozen = true;
                    setStateOnHurt.SetFrozen(4f);
                    canBeFrozen = originalCanBeFrozen;
                }
            }, FormatUtils.GetBestBodyName);
        }
    }
}
