using HarmonyLib;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using RoR2;
using UnityEngine;

namespace RiskOfChaos.Patches
{
    static class JumpVolumeHooks
    {
        public delegate void OnJumpVolumeJumpDelegate(JumpVolume jumpVolume, CharacterMotor jumpingCharacterMotor);
        public static event OnJumpVolumeJumpDelegate OnJumpVolumeJumpAuthority;

        [SystemInitializer]
        static void Init()
        {
            IL.RoR2.JumpVolume.OnTriggerStay += JumpVolume_OnTriggerStay;
        }

        static void JumpVolume_OnTriggerStay(ILContext il)
        {
            ILCursor c = new ILCursor(il);

            int characterMotorLocalIndex = -1;
            if (!c.TryFindNext(out _,
                               x => x.MatchCallOrCallvirt(SymbolExtensions.GetMethodInfo<Component>(_ => _.GetComponent<CharacterMotor>())),
                               x => x.MatchStloc(out characterMotorLocalIndex)))
            {
                Log.Error("Could not find CharacterMotor local index");
                return;
            }

            if (c.TryGotoNext(MoveType.After,
                              x => x.MatchStfld<CharacterMotor>(nameof(CharacterMotor.velocity))))
            {
                c.Emit(OpCodes.Ldarg_0);
                c.Emit(OpCodes.Ldloc, characterMotorLocalIndex);
                c.EmitDelegate(onJump);
                static void onJump(JumpVolume jumpVolume, CharacterMotor characterMotor)
                {
                    OnJumpVolumeJumpAuthority?.Invoke(jumpVolume, characterMotor);
                }
            }
            else
            {
                Log.Error("Failed to find patch location");
            }
        }
    }
}
