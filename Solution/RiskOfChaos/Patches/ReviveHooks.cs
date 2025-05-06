using Mono.Cecil;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using RiskOfChaos.Utilities.Extensions;
using RoR2;
using System;

namespace RiskOfChaos.Patches
{
    static class ReviveHooks
    {
        public delegate void OverrideAllowReviveDelegate(CharacterMaster master, ref bool allowRevive);
        public static event OverrideAllowReviveDelegate OverrideAllowRevive;

        [SystemInitializer]
        static void Init()
        {
            IL.RoR2.CharacterMaster.OnBodyDeath += CharacterMaster_OnBodyDeath;
            IL.RoR2.EquipmentSlot.FireHealAndRevive += EquipmentSlot_FireHealAndRevive;
            IL.RoR2.SeekerController.UnlockGateEffects += SeekerController_UnlockGateEffects;
            IL.RoR2.ShrineColossusAccessBehavior.ReviveAlliedPlayers += ShrineColossusAccessBehavior_ReviveAlliedPlayers;
        }

        static bool canRevive(CharacterMaster master)
        {
            bool allowRevive = true;
            OverrideAllowRevive?.Invoke(master, ref allowRevive);
            return allowRevive;
        }

        static ILCursor EmitCanReviveCall(this ILCursor c)
        {
            c.EmitDelegate<Func<CharacterMaster, bool>>(canRevive);
            return c;
        }

        static void CharacterMaster_OnBodyDeath(ILContext il)
        {
            ILCursor c = new ILCursor(il);

            if (!il.Method.TryFindParameter<CharacterBody>(out ParameterDefinition bodyParameter))
            {
                Log.Error("Failed to find body parameter");
                return;
            }

            if (!c.TryGotoNext(x => x.MatchLdfld<CharacterMaster>(nameof(CharacterMaster.destroyOnBodyDeath))) ||
                !c.TryGotoPrev(MoveType.AfterLabel, x => x.MatchLdarg(0)))
            {
                Log.Error("Failed to find branch location");
                return;
            }

            ILLabel skipReviveLabel = c.MarkLabel();

            c.Index = 0;
            if (!c.TryGotoNext(x => x.MatchLdsfld(typeof(DLC2Content.Buffs), nameof(DLC2Content.Buffs.ExtraLifeBuff))) ||
                !c.TryGotoPrev(MoveType.AfterLabel, x => x.MatchLdarg(bodyParameter.Index)))
            {
                Log.Error("Failed to find patch location");
                return;
            }

            c.Emit(OpCodes.Ldarg_0);
            c.EmitCanReviveCall();
            c.Emit(OpCodes.Brfalse, skipReviveLabel);
        }

        static void EquipmentSlot_FireHealAndRevive(ILContext il)
        {
            ILCursor c = new ILCursor(il);

            ILLabel aliveLabel = null;
            if (!c.TryFindNext(out ILCursor[] foundCursors,
                               x => x.MatchCallOrCallvirt<CharacterMaster>(nameof(CharacterMaster.IsDeadAndOutOfLivesServer)),
                               x => x.MatchBrfalse(out aliveLabel)))
            {
                Log.Error("Failed to find patch location");
                return;
            }

            VariableDefinition characterMasterVar = il.AddVariable<CharacterMaster>();
            foundCursors[0].Emit(OpCodes.Dup)
                           .Emit(OpCodes.Stloc, characterMasterVar);

            ILCursor cursor = foundCursors[foundCursors.Length - 1];
            cursor.Index++;

            if (!cursor.TryFindForeachContinueLabel(out ILLabel skipReviveLabel))
            {
                Log.Warning("Failed to find continue label, using alive label");
                skipReviveLabel = aliveLabel;
            }

            cursor.Emit(OpCodes.Ldloc, characterMasterVar)
                  .EmitCanReviveCall()
                  .Emit(OpCodes.Brfalse, skipReviveLabel);
        }

        static void SeekerController_UnlockGateEffects(ILContext il)
        {
            ILCursor c = new ILCursor(il);

            int playerMasterLocalIndex = -1;
            if (!c.TryFindNext(out ILCursor[] foundCursors,
                               x => x.MatchCallOrCallvirt(typeof(PlayerCharacterMasterController), "get_" + nameof(PlayerCharacterMasterController.master)),
                               x => x.MatchStloc(out playerMasterLocalIndex)))
            {
                Log.Error("Failed to find patch location");
                return;
            }

            ILCursor cursor = foundCursors[foundCursors.Length - 1];
            cursor.Index++;

            if (!cursor.TryFindForeachContinueLabel(out ILLabel continueLabel))
            {
                Log.Error("Failed to find continue label");
                return;
            }

            cursor.Emit(OpCodes.Ldloc, playerMasterLocalIndex)
                  .EmitCanReviveCall()
                  .Emit(OpCodes.Brfalse, continueLabel);
        }

        static void ShrineColossusAccessBehavior_ReviveAlliedPlayers(ILContext il)
        {
            ILCursor c = new ILCursor(il);

            ILLabel aliveLabel = null;
            if (!c.TryFindNext(out ILCursor[] foundCursors,
                               x => x.MatchCallOrCallvirt<CharacterMaster>(nameof(CharacterMaster.IsDeadAndOutOfLivesServer)),
                               x => x.MatchBrfalse(out aliveLabel)))
            {
                Log.Error("Failed to find patch location");
                return;
            }

            VariableDefinition characterMasterVar = il.AddVariable<CharacterMaster>();
            foundCursors[0].Emit(OpCodes.Dup)
                           .Emit(OpCodes.Stloc, characterMasterVar);

            ILCursor cursor = foundCursors[foundCursors.Length - 1];
            cursor.Index++;

            if (!cursor.TryFindForeachContinueLabel(out ILLabel skipReviveLabel))
            {
                Log.Warning("Failed to find continue label, using alive label");
                skipReviveLabel = aliveLabel;
            }

            cursor.Emit(OpCodes.Ldloc, characterMasterVar)
                  .EmitCanReviveCall()
                  .Emit(OpCodes.Brfalse, skipReviveLabel);
        }
    }
}
