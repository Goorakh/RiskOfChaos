using EntityStates.Toolbot;
using HarmonyLib;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using RoR2;

namespace RiskOfChaos.Patches
{
    static class ToolbotDualWieldStancePatch
    {
        [SystemInitializer]
        static void Init()
        {
            IL.EntityStates.Toolbot.ToolbotDualWieldBase.OnEnter += ToolbotDualWieldBase_OnEnter;
        }

        static void ToolbotDualWieldBase_OnEnter(ILContext il)
        {
            ILCursor c = new ILCursor(il);

            Instruction primary1SlotSetInstruction = null;
            Instruction primary2SlotSetInstruction = null;

            ILCursor[] foundCursors;

            if (c.TryFindNext(out foundCursors,
                              x => x.MatchCallOrCallvirt(AccessTools.DeclaredPropertySetter(typeof(ToolbotDualWieldBase), nameof(ToolbotDualWieldBase.primary1Slot)))))
            {
                primary1SlotSetInstruction = foundCursors[0].Next;
            }

            if (c.TryFindNext(out foundCursors,
                              x => x.MatchCallOrCallvirt(AccessTools.DeclaredPropertySetter(typeof(ToolbotDualWieldBase), nameof(ToolbotDualWieldBase.primary2Slot)))))
            {
                primary2SlotSetInstruction = foundCursors[0].Next;
            }

            Instruction lastPrimarySlotSetInstruction = null;
            if (primary1SlotSetInstruction != null && primary2SlotSetInstruction != null)
            {
                int primary1SlotSetInstructionIndex = il.Instrs.IndexOf(primary1SlotSetInstruction);
                int primary2SlotSetInstructionIndex = il.Instrs.IndexOf(primary2SlotSetInstruction);

                lastPrimarySlotSetInstruction = primary2SlotSetInstructionIndex > primary1SlotSetInstructionIndex ? primary2SlotSetInstruction : primary1SlotSetInstruction;
            }
            else
            {
                lastPrimarySlotSetInstruction = primary1SlotSetInstruction ?? primary2SlotSetInstruction;
            }

            if (lastPrimarySlotSetInstruction == null)
            {
                Log.Error("Failed to find patch location");
                return;
            }

            c.Goto(lastPrimarySlotSetInstruction, MoveType.After);

            c.Emit(OpCodes.Ldarg_0);
            c.EmitDelegate(onPrimarySlotsAssigned);

            static void onPrimarySlotsAssigned(ToolbotDualWieldBase state)
            {
                bool shouldSwapPrimarySlots = false;

                EntityStateMachine stanceStateMachine = EntityStateMachine.FindByCustomName(state.gameObject, "Stance");
                if (stanceStateMachine)
                {
                    if (stanceStateMachine.state is ToolbotStanceB)
                    {
                        shouldSwapPrimarySlots = true;

                        Log.Debug($"Swapping toolbot primary slots for {Util.GetBestBodyName(state.gameObject)}");
                    }
                }

                if (shouldSwapPrimarySlots)
                {
                    (state.primary1Slot, state.primary2Slot) = (state.primary2Slot, state.primary1Slot);
                }
            }
        }
    }
}
