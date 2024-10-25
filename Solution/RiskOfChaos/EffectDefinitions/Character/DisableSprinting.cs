using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.Patches;
using RiskOfChaos.Utilities.Extensions;
using RoR2;
using UnityEngine;

namespace RiskOfChaos.EffectDefinitions.Character
{
    [ChaosTimedEffect("disable_sprinting", 30f, AllowDuplicates = false, DefaultSelectionWeight = 0.8f)]
    [IncompatibleEffects(typeof(ForceSprinting))]
    public sealed class DisableSprinting : MonoBehaviour
    {
        void Start()
        {
            CharacterBody.readOnlyInstancesList.TryDo(body =>
            {
                if (body.hasEffectiveAuthority)
                {
                    body.isSprinting = false;
                }
            });

            SetIsSprintingOverride.OverrideCharacterSprinting += overrideSprint;
        }

        void OnDestroy()
        {
            SetIsSprintingOverride.OverrideCharacterSprinting -= overrideSprint;
        }

        static void overrideSprint(CharacterBody body, ref bool isSprinting)
        {
            isSprinting = false;
        }
    }
}
