using HarmonyLib;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using RoR2;
using System;
using System.Runtime.CompilerServices;

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
            onSetRunDifficulty();
        }

        static void Run_OnDeserialize(ILContext il)
        {
            ILCursor c = new ILCursor(il);

            while (c.TryGotoNext(MoveType.After,
                                 x => x.MatchStfld<Run>(nameof(Run.selectedDifficultyInternal))))
            {
                c.EmitDelegate(onSetRunDifficulty);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static void onSetRunDifficulty()
        {
            OnRunDifficultyChanged?.Invoke();
        }
    }
}
