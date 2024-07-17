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

            ILCursor[] foundCursors;
            if (c.TryFindNext(out foundCursors,
                              x => x.MatchLdloc(characterMotorLocalIndex),
                              x => x.MatchCallOrCallvirt(AccessTools.DeclaredPropertyGetter(typeof(CharacterMotor), nameof(CharacterMotor.hasEffectiveAuthority))),
                              x => x.MatchBrfalse(out _)))
            {
                ILCursor cursor = foundCursors[2];
                cursor.Next.MatchBrfalse(out ILLabel label);
                cursor.Next = label.Target;

                cursor.Emit(OpCodes.Ldloc, characterMotorLocalIndex);
                cursor.EmitDelegate((CharacterMotor characterMotor) =>
                {
                    if (!characterMotor)
                        return;

                    if (characterMotor.TryGetComponent(out IsJumpingOnJumpPadTracker jumpingTracker) && !jumpingTracker.IsJumping)
                    {
#if DEBUG
#pragma warning disable Publicizer001 // Accessing a member that was not originally public
                        Log.Debug($"{FormatUtils.GetBestBodyName(characterMotor.body)} started jumping on jump pad");
#pragma warning restore Publicizer001 // Accessing a member that was not originally public
#endif
                        jumpingTracker.CmdSetIsJumping(true);
                    }
                });
            }
            else
            {
                Log.Error("Unable to find patch location");
                return;
            }
        }
    }
}
