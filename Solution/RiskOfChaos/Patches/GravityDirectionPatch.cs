﻿using HarmonyLib;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using RiskOfChaos.Utilities;
using RiskOfChaos.Utilities.Extensions;
using RoR2;
using System;
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

                if (c.TryFindNext(out ILCursor[] foundCursors,
                                  x => x.MatchLdarg(0),
                                  x => x.MatchCall(AccessTools.DeclaredPropertyGetter(typeof(CharacterMotor), nameof(CharacterMotor.useGravity))),
                                  x => x.MatchBrfalse(out _)))
                {
                    ILCursor cursor = foundCursors[2];
                    cursor.Index++;

                    cursor.Emit(OpCodes.Ldarg_0);
                    cursor.Emit(OpCodes.Ldarg_1);
                    cursor.EmitDelegate(applyXZGravity);
                    static void applyXZGravity(CharacterMotor instance, float deltaTime)
                    {
                        if (instance.isGrounded)
                            return;

                        Vector3 gravity = instance.GetGravity();

                        Vector3 xzGravity = new Vector3(gravity.x, 0f, gravity.z);
                        instance.velocity += xzGravity * deltaTime;
                    }
                }
                else
                {
                    Log.Error("Failed to find XZ gravity patch location");
                }
            };

            IL.RoR2.ModelLocator.UpdateTargetNormal += il =>
            {
                ILCursor c = new ILCursor(il);

                int patchCount = 0;

                while (c.TryGotoNext(MoveType.After,
                                     x => x.MatchCallOrCallvirt(AccessTools.DeclaredPropertyGetter(typeof(Vector3), nameof(Vector3.up)))))
                {
                    c.EmitDelegate<Func<Vector3, Vector3>>(WorldUtils.GetWorldUpByGravity);

                    patchCount++;
                }

                if (patchCount == 0)
                {
                    Log.Error("Found 0 up override patch locations");
                }
                else
                {
                    Log.Debug($"Found {patchCount} up override patch location(s)");
                }
            };
        }
    }
}
