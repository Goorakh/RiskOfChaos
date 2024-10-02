using Mono.Cecil;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.Utils;
using RoR2;

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
                foreach (UseExplicitOriginPositionDelegate useExplicitPositionDelegate in UseExplicitOriginPosition.GetInvocationList())
                {
                    if (useExplicitPositionDelegate(bulletAttack))
                        return true;
                }
            }

            return false;
        }

        [SystemInitializer]
        static void Init()
        {
            IL.RoR2.BulletAttack.FireSingle += BulletAttack_FireSingle_FixTracerEffectOrigin;
            IL.RoR2.BulletAttack.FireSingle_ReturnHit += BulletAttack_FireSingle_FixTracerEffectOrigin;
            IL.RoR2.BulletAttack.FireMulti += BulletAttack_FireSingle_FixTracerEffectOrigin;
        }

        static void BulletAttack_FireSingle_FixTracerEffectOrigin(ILContext il)
        {
            ILCursor c = new ILCursor(il);

            VariableDefinition shouldUseExplicitOriginVar = new VariableDefinition(il.Import(typeof(bool)));
            il.Method.Body.Variables.Add(shouldUseExplicitOriginVar);

            c.Emit(OpCodes.Ldarg_0);
            c.EmitDelegate(shouldUseExplicitOriginPosition);
            c.Emit(OpCodes.Stloc, shouldUseExplicitOriginVar);

            int patchCount = 0;

            while (c.TryGotoNext(MoveType.Before,
                                 x => x.MatchCallOrCallvirt<EffectData>(nameof(EffectData.SetChildLocatorTransformReference))))
            {
                MethodReference method = (MethodReference)c.Next.Operand;

                ILLabel skipCallLabel = c.DefineLabel();
                ILLabel afterPatchLabel = c.DefineLabel();

                c.Emit(OpCodes.Ldloc, shouldUseExplicitOriginVar);
                c.Emit(OpCodes.Brtrue, skipCallLabel);

                c.Index++;

                c.Emit(OpCodes.Br, afterPatchLabel);

                c.MarkLabel(skipCallLabel);

                int popCount = method.Parameters.Count + (method.Resolve().IsStatic ? 0 : 1);
                for (int i = 0; i < popCount; i++)
                {
                    c.Emit(OpCodes.Pop);
                }

                if (!method.ReturnType.Is(typeof(void)))
                {
                    Log.Warning("Skipped method is not void, emitting null, this will probably cause issues");
                    c.Emit(OpCodes.Ldnull);
                }

                c.MarkLabel(afterPatchLabel);

                patchCount++;
            }

            if (patchCount == 0)
            {
                Log.Error("Found 0 patch locations");
            }
#if DEBUG
            else
            {
                Log.Debug($"Found {patchCount} patch location(s)");
            }
#endif
        }
    }
}
