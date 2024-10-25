using Mono.Cecil;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using R2API;
using RiskOfChaos.Utilities.Extensions;
using RoR2;

namespace RiskOfChaos.Content
{
    public static class CustomDOTs
    {
        public static DotController.DotIndex PercentHealthBurnDotIndex { get; private set; } = DotController.DotIndex.None;

        [SystemInitializer(typeof(DotController))]
        static void Init()
        {
            const float PERCENT_HEALTH_DOT_TICKS_PER_SECOND = 7f;
            PercentHealthBurnDotIndex = DotAPI.RegisterDotDef(new DotController.DotDef
            {
                damageCoefficient = 1f / PERCENT_HEALTH_DOT_TICKS_PER_SECOND,
                interval = 1f / PERCENT_HEALTH_DOT_TICKS_PER_SECOND,
                damageColorIndex = DamageColorIndex.Fragile,
                associatedBuff = RoR2Content.Buffs.OnFire
            });

            IL.RoR2.DotController.AddDot += il =>
            {
                ILCursor c = new ILCursor(il);

                if (!il.Method.TryFindParameter<float>("damageMultiplier", out ParameterDefinition damageMultiplierParameter))
                {
                    Log.Error("Unable to find damageMultiplier argument");
                    return;
                }

                int dotStackLocalIndex = -1;
                if (c.TryGotoNext(x => x.MatchLdsfld<DotController>(nameof(DotController.dotStackPool)),
                                  x => x.MatchCallOrCallvirt(out _),
                                  x => x.MatchStloc(out dotStackLocalIndex)))
                {
                    if (c.TryGotoNext(MoveType.Before,
                                      x => x.MatchSwitch(out _)))
                    {
                        c.Emit(OpCodes.Ldarg_0);
                        c.Emit(OpCodes.Ldarg, damageMultiplierParameter);
                        c.Emit(OpCodes.Ldloc, dotStackLocalIndex);

                        c.EmitDelegate(handleCustomDOTs);
                    }
                    else
                    {
                        Log.Error("Failed to find dotIndex switch");
                    }
                }
                else
                {
                    Log.Error("Failed to find dotStack local index");
                }
            };
        }

        static void handleCustomDOTs(DotController instance, float damageMultipler, DotController.DotStack dotStack)
        {
            DotController.DotIndex dotIndex = dotStack.dotIndex;
            if (dotIndex == DotController.DotIndex.None)
                return;

            DotController.DotDef dotDef = DotController.GetDotDef(dotIndex);

            if (dotIndex == PercentHealthBurnDotIndex)
            {
                dotStack.damage = (instance.victimHealthComponent.fullCombinedHealth / 100f) * damageMultipler * dotDef.damageCoefficient;
            }
        }
    }
}
