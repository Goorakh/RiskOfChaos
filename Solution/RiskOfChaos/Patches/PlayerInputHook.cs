using HarmonyLib;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using RoR2;
using UnityEngine;

namespace RiskOfChaos.Patches
{
    static class PlayerInputHook
    {
        public delegate void ModifyPlayerMoveInputDelegate(PlayerCharacterMasterController playerMasterController, ref Vector2 moveInput);
        public static event ModifyPlayerMoveInputDelegate ModifyPlayerMoveInput;

        [SystemInitializer]
        static void Init()
        {
            IL.RoR2.PlayerCharacterMasterController.Update += PlayerCharacterMasterController_Update;
        }

        static void PlayerCharacterMasterController_Update(ILContext il)
        {
            ILCursor c = new ILCursor(il);

            if (!c.TryFindNext(out ILCursor[] foundCursors,
                               x => x.MatchLdcI4(RewiredConsts.Action.MoveHorizontal),
                               x => x.MatchCallOrCallvirt(SymbolExtensions.GetMethodInfo<Rewired.Player>(_ => _.GetAxis(default(int)))),
                               x => x.MatchLdcI4(RewiredConsts.Action.MoveVertical),
                               x => x.MatchCallOrCallvirt(SymbolExtensions.GetMethodInfo<Rewired.Player>(_ => _.GetAxis(default(int)))),
                               x => x.MatchCall(AccessTools.DeclaredConstructor(typeof(Vector2), [typeof(float), typeof(float)]))))
            {
                Log.Error("Failed to find patch location");
                return;
            }

            ILCursor cursor = foundCursors[foundCursors.Length - 1];

            int moveInputLocalIndex = -1;
            if (!cursor.Clone().TryGotoPrev(x => x.MatchLdloca(out moveInputLocalIndex)))
            {
                Log.Error("Failed to find move input local index");
                return;
            }

            if (!cursor.TryGotoNext(MoveType.After,
                                    x => x.MatchCallOrCallvirt(SymbolExtensions.GetMethodInfo<InputBankTest>(_ => _.SetRawMoveStates(default)))))
            {
                Log.Warning("Failed to find SetRawMoveStates call");
            }

            cursor.Emit(OpCodes.Ldarg_0);
            cursor.Emit(OpCodes.Ldloca, moveInputLocalIndex);
            cursor.EmitDelegate(modifyInput);
            static void modifyInput(PlayerCharacterMasterController playerMasterController, ref Vector2 moveInput)
            {
                ModifyPlayerMoveInput?.Invoke(playerMasterController, ref moveInput);
            }
        }
    }
}
