using Mono.Cecil.Cil;
using MonoMod.Cil;
using RiskOfChaos.Utilities.Extensions;
using RoR2;
using System.Linq;

namespace RiskOfChaos.Patches
{
    static class OverrideBulletTracerOriginExplicitPatch
    {
        public delegate bool UseExplicitOriginPositionDelegate(BulletAttack bulletAttack);
        public static event UseExplicitOriginPositionDelegate UseExplicitOriginPosition;

        static bool shouldUseExplicitOriginPosition(BulletAttack bulletAttack)
        {
            if (UseExplicitOriginPosition != null)
            {
                foreach (UseExplicitOriginPositionDelegate useExplicitPositionDelegate in UseExplicitOriginPosition.GetInvocationList()
                                                                                                                   .OfType<UseExplicitOriginPositionDelegate>())
                {
                    if (useExplicitPositionDelegate(bulletAttack))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        [SystemInitializer]
        static void Init()
        {
            IL.RoR2.BulletAttack.FireSingle += BulletAttack_FireSingle_FixTracerEffectOrigin;
        }

        static void BulletAttack_FireSingle_FixTracerEffectOrigin(ILContext il)
        {
            ILCursor c = new ILCursor(il);

            VariableDefinition shouldUseExplicitOriginVar = il.AddVariable<bool>();

            c.Emit(OpCodes.Ldarg_0);
            c.EmitDelegate(shouldUseExplicitOriginPosition);
            c.Emit(OpCodes.Stloc, shouldUseExplicitOriginVar);

            int patchCount = 0;

            while (c.TryGotoNext(MoveType.Before,
                                 x => x.MatchCallOrCallvirt<EffectData>(nameof(EffectData.SetChildLocatorTransformReference))))
            {
                c.Emit(OpCodes.Ldloc, shouldUseExplicitOriginVar);
                c.EmitSkipMethodCall(OpCodes.Brtrue);

                c.SearchTarget = SearchTarget.Next;

                patchCount++;
            }

            if (patchCount == 0)
            {
                Log.Error("Found 0 patch locations");
            }
            else
            {
                Log.Debug($"Found {patchCount} patch location(s)");
            }
        }
    }
}
