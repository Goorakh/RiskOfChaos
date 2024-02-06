using HarmonyLib;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using RoR2;
using System;
using UnityEngine;

namespace RiskOfChaos.Patches
{
    static class PlayerInputHook
    {
        static bool _appliedMoveInputPatches = false;
        static void tryApplyMoveInputPatches()
        {
            if (_appliedMoveInputPatches)
                return;

            IL.RoR2.PlayerCharacterMasterController.Update += il =>
            {
                ILCursor c = new ILCursor(il);

                ILCursor[] foundCursors;
                if (c.TryFindNext(out foundCursors,
                                  x => x.MatchCallOrCallvirt(SymbolExtensions.GetMethodInfo<Rewired.Player>(_ => _.GetAxis(default(int)))),
                                  x => x.MatchCallOrCallvirt(SymbolExtensions.GetMethodInfo<Rewired.Player>(_ => _.GetAxis(default(int)))),
                                  x => x.MatchCall(AccessTools.DeclaredConstructor(typeof(Vector2), [typeof(float), typeof(float)]))))
                {
                    ILCursor cursor = foundCursors[2];
                    int patchIndex = cursor.Index + 1;

                    int moveInputLocalIndex = -1;
                    if (cursor.TryGotoPrev(x => x.MatchLdloca(out moveInputLocalIndex)))
                    {
                        cursor.Index = patchIndex;

                        cursor.Emit(OpCodes.Ldarg_0);
                        cursor.Emit(OpCodes.Ldloca, moveInputLocalIndex);
                        cursor.EmitDelegate((PlayerCharacterMasterController playerMasterController, ref Vector2 moveInput) =>
                        {
                            _modifiyPlayerInput?.Invoke(playerMasterController, ref moveInput);
                        });
                    }
                    else
                    {
                        Log.Error("Failed to find move input local index");
                    }
                }
                else
                {
                    Log.Error("Failed to find patch location");
                }
            };

            _appliedMoveInputPatches = true;
        }

        public delegate void ModifyPlayerMoveInputDelegate(PlayerCharacterMasterController playerMasterController, ref Vector2 moveInput);
        static event ModifyPlayerMoveInputDelegate _modifiyPlayerInput;

        public static event ModifyPlayerMoveInputDelegate ModifyPlayerMoveInput
        {
            add
            {
                _modifiyPlayerInput += value;
                tryApplyMoveInputPatches();
            }
            remove
            {
                _modifiyPlayerInput -= value;
            }
        }
    }
}
