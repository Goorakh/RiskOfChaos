using HarmonyLib;
using Mono.Cecil;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using RiskOfChaos.EffectDefinitions.World;
using RiskOfChaos.EffectHandling.Controllers;
using RoR2;
using System.Reflection;

namespace RiskOfChaos.Patches.Effects.World
{
    static class GuaranteedChanceRollsHooks
    {
        static bool _isRollingTougherTimesProc;
        static bool _isRollingEquipmentDrop;

        [SystemInitializer]
        static void Init()
        {
            On.RoR2.Util.CheckRoll_float_float_CharacterMaster += checkGuaranteedRoll;

            IL.RoR2.HealthComponent.TakeDamageProcess += HealthComponent_TakeDamageProcess;

            IL.RoR2.GlobalEventManager.OnCharacterDeath += GlobalEventManager_OnCharacterDeath;
        }

        static bool checkGuaranteedRoll(On.RoR2.Util.orig_CheckRoll_float_float_CharacterMaster orig, float percentChance, float luck, CharacterMaster effectOriginMaster)
        {
            bool effectActive = ChaosEffectTracker.Instance &&
                                ChaosEffectTracker.Instance.IsTimedEffectActive(GuaranteedChanceRolls.EffectInfo);

            if (effectActive && _isRollingEquipmentDrop)
            {
                luck += 200f;
            }

            return orig(percentChance, luck, effectOriginMaster) || (percentChance > 0f && effectActive && !_isRollingTougherTimesProc);
        }

        static void HealthComponent_TakeDamageProcess(ILContext il)
        {
            ILCursor c = new ILCursor(il);

            if (!c.TryFindNext(out ILCursor[] foundCursors,
                               x => x.MatchLdfld<HealthComponent.ItemCounts>(nameof(HealthComponent.ItemCounts.bear)),
                               x => x.MatchCallOrCallvirt(typeof(Util), nameof(Util.CheckRoll))))
            {
                Log.Error("Failed to find Tougher Times patch location");
                return;
            }

            ILCursor cursor = foundCursors[1];

            FieldInfo isRollingTougherTimesProc = AccessTools.DeclaredField(typeof(GuaranteedChanceRollsHooks), nameof(_isRollingTougherTimesProc));

            cursor.Emit(OpCodes.Ldc_I4_1);
            cursor.Emit(OpCodes.Stsfld, isRollingTougherTimesProc);

            cursor.Index++;

            cursor.Emit(OpCodes.Ldc_I4_0);
            cursor.Emit(OpCodes.Stsfld, isRollingTougherTimesProc);
        }

        static void GlobalEventManager_OnCharacterDeath(ILContext il)
        {
            ILCursor c = new ILCursor(il);

            if (!c.TryFindNext(out ILCursor[] foundCursors,
                               x => x.MatchLdfld<EquipmentDef>(nameof(EquipmentDef.dropOnDeathChance)),
                               x => x.MatchCallOrCallvirt(out MethodReference m) && m?.Name?.Contains("g__LocalCheckRoll|") == true))
            {
                Log.Error("Failed to find equipment drop patch location");
                return;
            }

            c.Goto(foundCursors[1].Next, MoveType.AfterLabel);

            FieldInfo isRollingEquipmentDropField = AccessTools.DeclaredField(typeof(GuaranteedChanceRollsHooks), nameof(_isRollingEquipmentDrop));

            c.Emit(OpCodes.Ldc_I4_1);
            c.Emit(OpCodes.Stsfld, isRollingEquipmentDropField);

            c.Index++;

            c.Emit(OpCodes.Ldc_I4_0);
            c.Emit(OpCodes.Stsfld, isRollingEquipmentDropField);
        }
    }
}
