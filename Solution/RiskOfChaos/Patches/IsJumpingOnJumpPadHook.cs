using HarmonyLib;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using RiskOfChaos.Networking.Components;
using RiskOfChaos.Utilities;
using RoR2;
using UnityEngine;

namespace RiskOfChaos.Patches
{
    static class IsJumpingOnJumpPadHook
    {
        [SystemInitializer]
        static void Init()
        {
            On.RoR2.CharacterMotor.Awake += CharacterMotor_Awake;

            IL.RoR2.JumpVolume.OnTriggerStay += JumpVolume_OnTriggerStay;
        }

        static void CharacterMotor_Awake(On.RoR2.CharacterMotor.orig_Awake orig, CharacterMotor self)
        {
            orig(self);
            self.gameObject.AddComponent<IsJumpingOnJumpPadTracker>();
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
                c.Emit(OpCodes.Ldloc, characterMotorLocalIndex);
                c.EmitDelegate(trackJump);
                static void trackJump(CharacterMotor characterMotor)
                {
                    if (!characterMotor)
                        return;

                    if (characterMotor.TryGetComponent(out IsJumpingOnJumpPadTracker jumpingTracker) && !jumpingTracker.IsJumping)
                    {
#if DEBUG
                        Log.Debug($"{FormatUtils.GetBestBodyName(characterMotor.body)} started jumping on jump pad");
#endif
                        jumpingTracker.CmdSetIsJumping(true);
                    }
                }
            }
            else
            {
                Log.Error("Failed to find patch location");
            }
        }
    }
}
