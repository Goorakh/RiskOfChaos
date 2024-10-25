using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.EffectUtils.Character.AllSkillsAgile;
using RiskOfChaos.Patches;
using RiskOfChaos.Utilities.Extensions;
using RoR2;
using UnityEngine;

namespace RiskOfChaos.EffectDefinitions.Character
{
    [ChaosTimedEffect("force_sprinting", 60f, AllowDuplicates = false)]
    [IncompatibleEffects(typeof(DisableSprinting))]
    public sealed class ForceSprinting : MonoBehaviour
    {
        void Start()
        {
            OverrideSkillsAgile.AllSkillsAgileCount++;

            CharacterBody.readOnlyInstancesList.TryDo(body =>
            {
                if (shouldForceSprint(body))
                {
                    body.isSprinting = true;
                }
            });

            SetIsSprintingOverride.OverrideCharacterSprinting += overrideSprint;
        }

        void OnDestroy()
        {
            OverrideSkillsAgile.AllSkillsAgileCount--;

            SetIsSprintingOverride.OverrideCharacterSprinting -= overrideSprint;
        }

        static bool shouldForceSprint(CharacterBody body)
        {
            return body.hasEffectiveAuthority && (!body.inputBank || body.inputBank.moveVector.sqrMagnitude > 0f);
        }

        static void overrideSprint(CharacterBody body, ref bool isSprinting)
        {
            isSprinting = isSprinting || shouldForceSprint(body);
        }
    }
}
