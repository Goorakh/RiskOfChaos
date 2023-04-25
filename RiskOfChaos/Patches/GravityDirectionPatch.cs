using HarmonyLib;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using RoR2;
using UnityEngine;

namespace RiskOfChaos.Patches
{
    static class GravityDirectionPatch
    {
        [SystemInitializer]
        static void Init()
        {
            IL.RoR2.CharacterMotor.PreMove += il =>
            {
                ILCursor c = new ILCursor(il);

                ILCursor[] foundCursors;
                if (c.TryFindNext(out foundCursors,
                                  x => x.MatchLdarg(0),
                                  x => x.MatchCall(AccessTools.DeclaredPropertyGetter(typeof(CharacterMotor), nameof(CharacterMotor.useGravity))),
                                  x => x.MatchBrfalse(out _)))
                {
                    ILCursor cursor = foundCursors[2];
                    cursor.Index++;

                    cursor.Emit(OpCodes.Ldarg_0);
                    cursor.Emit(OpCodes.Ldarg_1);
                    cursor.EmitDelegate((CharacterMotor instance, float deltaTime) =>
                    {
                        Vector3 xzGravity = new Vector3(Physics.gravity.x, 0f, Physics.gravity.z);
                        instance.velocity += xzGravity * deltaTime;
                    });
                }
            };

            IL.RoR2.ModelLocator.UpdateTargetNormal += il =>
            {
                ILCursor c = new ILCursor(il);

                while (c.TryGotoNext(MoveType.After,
                                     x => x.MatchCallOrCallvirt(AccessTools.DeclaredPropertyGetter(typeof(Vector3), nameof(Vector3.up)))))
                {
                    c.EmitDelegate((Vector3 up) =>
                    {
                        Vector3 gravity = Physics.gravity;
                        if (Mathf.Abs(gravity.x) > float.Epsilon || Mathf.Abs(gravity.z) > float.Epsilon)
                        {
                            return -gravity.normalized;
                        }
                        else
                        {
                            return up;
                        }
                    });
                }
            };
        }
    }
}
