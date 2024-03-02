using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Methods;
using RiskOfChaos.EffectUtils.Character.AllSkillsAgile;
using RiskOfChaos.Patches;
using RiskOfChaos.Utilities.Extensions;
using RoR2;

namespace RiskOfChaos.EffectDefinitions.Character
{
    [ChaosTimedEffect("force_sprinting", 60f, AllowDuplicates = false, IsNetworked = true)]
    [IncompatibleEffects(typeof(DisableSprinting))]
    public sealed class ForceSprinting : TimedEffect
    {
        [EffectCanActivate]
        static bool CanActivate()
        {
            return SetIsSprintingOverride.PatchSuccessful;
        }

        public override void OnStart()
        {
            OverrideSkillsAgile.AllSkillsAgileCount++;

            CharacterBody.readOnlyInstancesList.TryDo(body =>
            {
                if (body.hasEffectiveAuthority)
                {
                    body.isSprinting = true;
                }
            });

            SetIsSprintingOverride.OverrideCharacterSprinting += SetIsSprintingOverride_OverrideCharacterSprinting;
        }

        public override void OnEnd()
        {
            OverrideSkillsAgile.AllSkillsAgileCount--;

            SetIsSprintingOverride.OverrideCharacterSprinting -= SetIsSprintingOverride_OverrideCharacterSprinting;
        }

        static void SetIsSprintingOverride_OverrideCharacterSprinting(CharacterBody body, ref bool isSprinting)
        {
            if (body.hasEffectiveAuthority)
            {
                if (body.inputBank && body.inputBank.moveVector.sqrMagnitude < 0.01f)
                    return;

                isSprinting = true;
            }
        }
    }
}
