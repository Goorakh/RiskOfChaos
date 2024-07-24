using HarmonyLib;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using RiskOfChaos.Utilities.Extensions;
using RoR2;
using UnityEngine;

namespace RiskOfChaos.Patches
{
    static class CharacterJumpPadGravityPatch
    {
        [SystemInitializer]
        static void Init()
        {
            IL.RoR2.CharacterMotor.PreMove += CharacterMotor_PreMove;
        }

        static void CharacterMotor_PreMove(ILContext il)
        {
            ILCursor c = new ILCursor(il);

            while (c.TryGotoNext(MoveType.After,
                                 x => x.MatchCallOrCallvirt(AccessTools.DeclaredPropertyGetter(typeof(Physics), nameof(Physics.gravity)))))
            {
                c.Emit(OpCodes.Ldarg_0);
                c.EmitDelegate(overrideGravity);
                static Vector3 overrideGravity(Vector3 worldGravity, CharacterMotor instance)
                {
                    return instance.GetGravity(worldGravity);
                }
            }
        }
    }
}
