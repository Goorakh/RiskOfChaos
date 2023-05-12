using MonoMod.Cil;
using RiskOfChaos.EffectHandling;
using RiskOfChaos.EffectHandling.Controllers;
using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Data;
using RoR2;

namespace RiskOfChaos.EffectDefinitions.Character
{
    [ChaosEffect("disable_fall_damage", EffectStageActivationCountHardCap = 1)]
    [ChaosTimedEffect(TimedEffectType.UntilStageEnd)]
    public sealed class DisableFallDamage : TimedEffect
    {
        [InitEffectInfo]
        public static readonly ChaosEffectInfo EffectInfo;

        static bool _hasAppliedPatches;
        static void tryApplyPatches()
        {
            if (_hasAppliedPatches)
                return;

            IL.RoR2.GlobalEventManager.OnCharacterHitGroundServer += il =>
            {
                ILCursor c = new ILCursor(il);

                while (c.TryGotoNext(MoveType.After, x => x.MatchLdfld<CharacterBody>(nameof(CharacterBody.bodyFlags))))
                {
                    c.EmitDelegate((CharacterBody.BodyFlags bodyFlags) =>
                    {
                        if (TimedChaosEffectHandler.Instance && TimedChaosEffectHandler.Instance.IsTimedEffectActive(EffectInfo))
                        {
                            return bodyFlags | CharacterBody.BodyFlags.IgnoreFallDamage;
                        }
                        else
                        {
                            return bodyFlags;
                        }
                    });
                }
            };

            _hasAppliedPatches = true;
        }

        public override void OnStart()
        {
            tryApplyPatches();
        }

        public override void OnEnd()
        {
        }
    }
}
