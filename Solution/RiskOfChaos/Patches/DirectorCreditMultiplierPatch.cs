using Mono.Cecil.Cil;
using MonoMod.Cil;
using RiskOfChaos.ModificationController.Director;
using RoR2;

namespace RiskOfChaos.Patches
{
    static class DirectorCreditMultiplierPatch
    {
        [SystemInitializer]
        static void Init()
        {
            IL.RoR2.CombatDirector.DirectorMoneyWave.Update += DirectorMoneyWave_Update;
        }

        static float getCombatDirectorCreditMultiplier()
        {
            if (!DirectorModificationManager.Instance)
                return 1f;

            return DirectorModificationManager.Instance.CombatDirectorCreditMultiplier;
        }

        static void DirectorMoneyWave_Update(ILContext il)
        {
            ILCursor c = new ILCursor(il);

            int patchCount = 0;

            while (c.TryGotoNext(MoveType.After,
                                 x => x.MatchLdfld<CombatDirector.DirectorMoneyWave>(nameof(CombatDirector.DirectorMoneyWave.multiplier))))
            {
                c.EmitDelegate(getCombatDirectorCreditMultiplier);
                c.Emit(OpCodes.Mul);

                patchCount++;
            }

            if (patchCount == 0)
            {
                Log.Error("Found 0 patch locations");
            }
            else
            {
                Log.Debug($"Found {patchCount} patch location(s)");
            }
        }
    }
}
