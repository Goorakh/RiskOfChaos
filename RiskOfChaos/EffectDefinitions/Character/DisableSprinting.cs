using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Methods;
using RiskOfChaos.Patches;
using RiskOfChaos.Utilities.Extensions;
using RoR2;

namespace RiskOfChaos.EffectDefinitions.Character
{
    [ChaosEffect("disable_sprinting", DefaultSelectionWeight = 0.8f, IsNetworked = true)]
    [ChaosTimedEffect(30f, AllowDuplicates = false)]
    public sealed class DisableSprinting : TimedEffect
    {
        [EffectCanActivate]
        static bool CanActivate()
        {
            return SetIsSprintingOverride.PatchSuccessful;
        }

        public override void OnStart()
        {
            CharacterBody.readOnlyInstancesList.TryDo(body =>
            {
                if (body.hasEffectiveAuthority)
                {
                    body.isSprinting = false;
                }
            });

            SetIsSprintingOverride.OverrideCharacterSprinting += SetIsSprintingOverride_OverrideCharacterSprinting;
        }

        public override void OnEnd()
        {
            SetIsSprintingOverride.OverrideCharacterSprinting -= SetIsSprintingOverride_OverrideCharacterSprinting;
        }

        static void SetIsSprintingOverride_OverrideCharacterSprinting(CharacterBody body, ref bool isSprinting)
        {
            if (body.hasEffectiveAuthority)
            {
                isSprinting = false;
            }
        }
    }
}
