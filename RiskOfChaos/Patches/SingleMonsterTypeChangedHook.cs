using HarmonyLib;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using RoR2;
using RoR2.UI;
using System;
using System.Runtime.CompilerServices;

namespace RiskOfChaos.Patches
{
    static class SingleMonsterTypeChangedHook
    {
        public static event Action OnSingleMonsterTypeChanged;

        [SystemInitializer]
        static void Init()
        {
            new Hook(AccessTools.PropertySetter(typeof(Stage), nameof(Stage.Network_singleMonsterTypeBodyIndex)), set_Network_singleMonsterTypeBodyIndex_Hook);

            IL.RoR2.Stage.OnDeserialize += Stage_OnDeserialize;
        }

        delegate void orig_set_Network_singleMonsterTypeBodyIndex(Stage self, int value);
        static void set_Network_singleMonsterTypeBodyIndex_Hook(orig_set_Network_singleMonsterTypeBodyIndex orig, Stage self, int value)
        {
            orig(self, value);
            onSingleMonsterTypeChanged();
        }

        static void Stage_OnDeserialize(ILContext il)
        {
            ILCursor c = new ILCursor(il);

            while (c.TryGotoNext(MoveType.After,
                                 x => x.MatchStfld<Stage>(nameof(Stage._singleMonsterTypeBodyIndex))))
            {
                c.EmitDelegate(onSingleMonsterTypeChanged);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static void onSingleMonsterTypeChanged()
        {
            OnSingleMonsterTypeChanged?.Invoke();
            EnemyInfoPanel.MarkDirty();
        }
    }
}
