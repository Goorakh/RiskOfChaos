﻿using Mono.Cecil;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using RiskOfChaos.Utilities.Extensions;
using RoR2;

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
            ILCursor c = new ILCursor(il);

            if (!il.Method.TryFindParameter<DamageReport>(out ParameterDefinition damageReportParameter))
            {
                Log.Error("Failed to find DamageReport parameter");
                return;
            }

            if (!c.TryGotoNext(x => x.MatchLdstr("PLAYER_DEATH_QUOTE_VOIDDEATH")))
            {
                Log.Error("Failed to find death message token location");
                return;
            }

            int deathMessageTokenLocalIndex = -1;
            if (!c.TryGotoNext(x => x.MatchStloc(out deathMessageTokenLocalIndex)))
            {
                Log.Error("Failed to find death message token local index");
                return;
            }

            Log.Debug($"Death message token local index: {deathMessageTokenLocalIndex}");

            ILLabel messageTokenDecidedLabel = null;
            if (!c.TryGotoNext(x => x.MatchBr(out messageTokenDecidedLabel)))
            {
                Log.Error("Failed to find patch location");
            }

            c.GotoLabel(messageTokenDecidedLabel);
            c.Emit(OpCodes.Ldloca, deathMessageTokenLocalIndex);
            c.Emit(OpCodes.Ldarg, damageReportParameter);
            c.EmitDelegate(overrideMessageToken);
            static void overrideMessageToken(ref string messageToken, DamageReport damageReport)
            {
                if (OverridePlayerDeathMessageToken != null)
                {
                    Log.Debug($"Overriding death message token: {messageToken}");

                    OverridePlayerDeathMessageToken(damageReport, ref messageToken);
                }
            }
        }
    }
}
