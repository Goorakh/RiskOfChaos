using HarmonyLib;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RoR2;
using UnityEngine;

namespace RiskOfChaos.EffectDefinitions.Gravity
{
    [ChaosEffect("rotate_gravity")]
    public sealed class RotateGravity : GenericGravityEffect
    {
        static bool _hasAppliedPatches = false;

        static void tryApplyPatches()
        {
            if (_hasAppliedPatches)
                return;

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
                        if (!AnyGravityChangeActive)
                            return;

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
                        if (AnyGravityChangeActive)
                        {
                            return -Physics.gravity.normalized;
                        }
                        else
                        {
                            return up;
                        }
                    });
                }
            };

            _hasAppliedPatches = true;
        }

        protected override Vector3 modifyGravity(Vector3 originalGravity)
        {
            const float MAX_DEVIATION = 30f;

            return Quaternion.Euler(RNG.RangeFloat(-MAX_DEVIATION, MAX_DEVIATION),
                                    RNG.RangeFloat(-MAX_DEVIATION, MAX_DEVIATION),
                                    RNG.RangeFloat(-MAX_DEVIATION, MAX_DEVIATION)) * originalGravity;
        }

        public override void OnStart()
        {
            tryApplyPatches();
            base.OnStart();
        }
    }
}
