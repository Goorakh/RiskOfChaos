using HarmonyLib;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using RoR2;
using RoR2.UI;
using System.Reflection;

namespace RiskOfChaos.Patches
{
    static class OverrideObjectiveTrackerDirtyPatch
    {
        public delegate void OverrideObjectiveTrackerDirtyDelegate(ObjectivePanelController.ObjectiveTracker objective, ref bool isDirty);
        public static event OverrideObjectiveTrackerDirtyDelegate OverrideObjectiveTrackerDirty;

        [SystemInitializer]
        static void Init()
        {
            IL.RoR2.UI.ObjectivePanelController.ObjectiveTracker.GetString += ObjectiveTracker_GetString;
        }

        static void ObjectiveTracker_GetString(ILContext il)
        {
            ILCursor c = new ILCursor(il);

            MethodInfo ObjectiveTracker_IsDirty_MI = SymbolExtensions.GetMethodInfo<ObjectivePanelController.ObjectiveTracker>(_ => _.IsDirty());

            int patchCount = 0;
            while (c.TryGotoNext(MoveType.After, x => x.MatchCallOrCallvirt(ObjectiveTracker_IsDirty_MI)))
            {
                c.Emit(OpCodes.Ldarg_0);
                c.EmitDelegate((bool isDirty, ObjectivePanelController.ObjectiveTracker instance) =>
                {
                    OverrideObjectiveTrackerDirty?.Invoke(instance, ref isDirty);
                    return isDirty;
                });

                patchCount++;
            }

            if (patchCount == 0)
            {
                Log.Warning("Unable to find any patch locations");
            }
            else
            {
#if DEBUG
                Log.Debug($"Found {patchCount} patch location(s)");
#endif
            }
        }

        public static void ForceRefresh()
        {
            static void overrideAllDirty(ObjectivePanelController.ObjectiveTracker objective, ref bool isDirty)
            {
                isDirty = true;
            }

            OverrideObjectiveTrackerDirty += overrideAllDirty;

            RoR2Application.onNextUpdate += () =>
            {
                OverrideObjectiveTrackerDirty -= overrideAllDirty;
            };
        }
    }
}
