using Mono.Cecil.Cil;
using MonoMod.Cil;
using RoR2;
using System;

namespace RiskOfChaos.Patches
{
    static class CustomPlayerDeathMessageTokenPatch
    {
        public delegate void OverridePlayerDeathMessageTokenDelegate(DamageReport damageReport, ref string messageToken);
        public static event OverridePlayerDeathMessageTokenDelegate OverridePlayerDeathMessageToken;

        [SystemInitializer]
        static void Init()
        {
            IL.RoR2.GlobalEventManager.OnPlayerCharacterDeath += GlobalEventManager_OnPlayerCharacterDeath;
        }

        static void GlobalEventManager_OnPlayerCharacterDeath(ILContext il)
        {
            Instruction[] instructions = il.Body.Instructions.ToArray();

            int localSearchStartIndex = Array.FindIndex(instructions, i => i.MatchLdstr("PLAYER_DEATH_QUOTE_VOIDDEATH"));
            if (localSearchStartIndex == -1)
            {
                Log.Error("Failed to find death message token location");
                return;
            }

            int deathTokenLocalIndex = -1;

            for (int i = localSearchStartIndex + 1; i < instructions.Length; i++)
            {
                if (instructions[i].MatchStloc(out int localIndex))
                {
                    deathTokenLocalIndex = localIndex;
                    break;
                }
            }

            if (deathTokenLocalIndex == -1)
            {
                Log.Error("Failed to find death message token local index");
                return;
            }

#if DEBUG
            Log.Debug($"Death message token local index: {deathTokenLocalIndex}");
#endif

            ILCursor c = new ILCursor(il);

            while (c.TryGotoNext(MoveType.After, x => x.MatchLdloc(deathTokenLocalIndex)))
            {
                c.Emit(OpCodes.Ldarg_1); // DamageReport damageReport
                c.EmitDelegate((string messageToken, DamageReport damageReport) =>
                {
                    OverridePlayerDeathMessageToken?.Invoke(damageReport, ref messageToken);
                    return messageToken;
                });
            }
        }
    }
}
