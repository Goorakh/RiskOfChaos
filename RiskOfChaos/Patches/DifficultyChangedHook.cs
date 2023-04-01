using HarmonyLib;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using RoR2;
using System;

namespace RiskOfChaos.Patches
{
    public static class DifficultyChangedHook
    {
        public static event Action OnRunDifficultyChanged;

        [SystemInitializer]
        static void Init()
        {
            new Hook(AccessTools.PropertySetter(typeof(Run), nameof(Run.NetworkselectedDifficultyInternal)), set_NetworkselectedDifficultyInternal_Hook);

            IL.RoR2.Run.OnDeserialize += Run_OnDeserialize;
        }

        delegate void orig_set_NetworkselectedDifficultyInternal(Run self, int value);
        static void set_NetworkselectedDifficultyInternal_Hook(orig_set_NetworkselectedDifficultyInternal orig, Run self, int value)
        {
            orig(self, value);
            OnRunDifficultyChanged?.Invoke();
        }

        static void Run_OnDeserialize(ILContext il)
        {
            ILCursor c = new ILCursor(il);

#pragma warning disable Publicizer001 // Accessing a member that was not originally public
            const string SELECTED_DIFFUICULTY_FIELD_NAME = nameof(Run.selectedDifficultyInternal);
#pragma warning restore Publicizer001 // Accessing a member that was not originally public

            while (c.TryGotoNext(MoveType.After, x => x.MatchStfld<Run>(SELECTED_DIFFUICULTY_FIELD_NAME)))
            {
                c.EmitDelegate(static () =>
                {
                    OnRunDifficultyChanged?.Invoke();
                });
            }
        }
    }
}
