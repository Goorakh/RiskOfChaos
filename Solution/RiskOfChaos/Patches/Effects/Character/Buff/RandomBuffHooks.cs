using HarmonyLib;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using RoR2;
using System;

namespace RiskOfChaos.Patches.Effects.Character.Buff
{
    static class RandomBuffHooks
    {
        [SystemInitializer]
        static void Init()
        {
            IL.RoR2.GlobalEventManager.OnCharacterDeath += GlobalEventManager_OnCharacterDeath_PreventInfiniteSpawnChain;
        }

        static void GlobalEventManager_OnCharacterDeath_PreventInfiniteSpawnChain(ILContext il)
        {
            bool tryPatchOnDeathSpawn(ILCursor c, Type buffDeclaringType, string buffFieldName, string spawnedBodyName)
            {
                ILLabel afterSpawnLabel = null;
                int victimBodyLocalIndex = -1;

                if (c.TryGotoNext(MoveType.After,
                              x => x.MatchLdloc(out victimBodyLocalIndex),
                              x => x.MatchLdsfld(buffDeclaringType, buffFieldName),
                              x => x.MatchCallOrCallvirt(SymbolExtensions.GetMethodInfo<CharacterBody>(_ => _.HasBuff(default(BuffDef)))),
                              x => x.MatchBrfalse(out afterSpawnLabel)))
                {
                    c.Emit(OpCodes.Ldloc, victimBodyLocalIndex);
                    c.Emit(OpCodes.Ldstr, spawnedBodyName);
                    c.EmitDelegate(checkCanSpawn);
                    static bool checkCanSpawn(CharacterBody victimBody, string spawnedBodyName)
                    {
                        return victimBody && victimBody.bodyIndex != BodyCatalog.FindBodyIndex(spawnedBodyName);
                    }

                    c.Emit(OpCodes.Brfalse, afterSpawnLabel);

                    return true;
                }
                else
                {
                    return false;
                }
            }

            if (!tryPatchOnDeathSpawn(new ILCursor(il), typeof(RoR2Content.Buffs), nameof(RoR2Content.Buffs.AffixPoison), "UrchinTurretBody"))
            {
                Log.Error("Failed to find malachite urchin patch location");
            }

            if (!tryPatchOnDeathSpawn(new ILCursor(il), typeof(DLC1Content.Buffs), nameof(DLC1Content.Buffs.EliteEarth), "AffixEarthHealerBody"))
            {
                Log.Error("Failed to find healing core patch location");
            }

            if (!tryPatchOnDeathSpawn(new ILCursor(il), typeof(DLC1Content.Buffs), nameof(DLC1Content.Buffs.EliteVoid), "VoidInfestorBody"))
            {
                Log.Error("Failed to find void infestor patch location");
            }
        }
    }
}
