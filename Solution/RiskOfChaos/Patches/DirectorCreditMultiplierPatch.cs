using Mono.Cecil.Cil;
using MonoMod.Cil;
using RiskOfChaos.ModificationController.Director;
using RoR2;
using System.Collections.Generic;

namespace RiskOfChaos.Patches
{
    static class DirectorCreditMultiplierPatch
    {
        [SystemInitializer]
        static void Init()
        {
            IL.RoR2.CombatDirector.AttemptSpawnOnTarget += CombatDirector_ApplyCreditMultiplier;
            IL.RoR2.CombatDirector.PrepareNewMonsterWave += CombatDirector_ApplyCreditMultiplier;
            IL.RoR2.CombatDirector.SpendAllCreditsOnMapSpawns += CombatDirector_ApplyCreditMultiplier;
        }

        static float getCombatDirectorCreditMultiplier()
        {
            if (!DirectorModificationManager.Instance)
                return 1f;

            return DirectorModificationManager.Instance.CombatDirectorCreditMultiplier;
        }

        static void CombatDirector_ApplyCreditMultiplier(ILContext il)
        {
            ILCursor c = new ILCursor(il);

            List<Instruction> ignoreMonsterCreditLoadInstructions = [];

            while (c.TryGotoNext(x => x.MatchStfld<CombatDirector>(nameof(CombatDirector.monsterCredit))))
            {
                if (c.TryFindPrev(out ILCursor[] foundCursors, x => x.MatchLdfld<CombatDirector>(nameof(CombatDirector.monsterCredit))))
                {
                    ignoreMonsterCreditLoadInstructions.Add(foundCursors[0].Next);
                }
            }

            Log.Debug($"{il.Method.FullName}: Found {ignoreMonsterCreditLoadInstructions.Count} credit load location(s) to ignore");

            c.Index = 0;

            int patchCount = 0;

            while (c.TryGotoNext(MoveType.After,
                                 x => x.MatchLdfld<CombatDirector>(nameof(CombatDirector.monsterCredit))))
            {
                if (!ignoreMonsterCreditLoadInstructions.Contains(c.Prev))
                {
                    c.EmitDelegate(getCombatDirectorCreditMultiplier);
                    c.Emit(OpCodes.Mul);

                    patchCount++;
                }
            }

            if (patchCount == 0)
            {
                Log.Error($"{il.Method.FullName}: Failed to find any valid patch locations");
            }
            else
            {
                Log.Debug($"{il.Method.FullName}: Found {patchCount} valid patch location(s)");
            }
        }
    }
}
