using HarmonyLib;
using Mono.Cecil;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using RoR2;
using System.Reflection;

namespace RiskOfChaos.Patches
{
    static class CharacterMoneyChangedHook
    {
        public delegate void OnCharacterMoneyChangedDelegate(CharacterMaster master, long moneyDiff);
        public static event OnCharacterMoneyChangedDelegate OnCharacterMoneyChanged;

        [SystemInitializer]
        static void Init()
        {
            new ILHook(AccessTools.DeclaredPropertySetter(typeof(CharacterMaster), nameof(CharacterMaster.money)), hookSetMoney);
            IL.RoR2.CharacterMaster.OnDeserialize += hookSetMoney;
        }

        static void hookSetMoney(ILContext il)
        {
            ILCursor c = new ILCursor(il);

            VariableDefinition newMoneyVar = new VariableDefinition(il.Module.ImportReference(typeof(uint)));
            il.Method.Body.Variables.Add(newMoneyVar);

            VariableDefinition masterInstanceVar = new VariableDefinition(il.Module.ImportReference(typeof(CharacterMaster)));
            il.Method.Body.Variables.Add(masterInstanceVar);

            VariableDefinition oldMoneyVar = new VariableDefinition(il.Module.ImportReference(typeof(uint)));
            il.Method.Body.Variables.Add(oldMoneyVar);

            FieldInfo characterMasterMoneyField = AccessTools.DeclaredField(typeof(CharacterMaster), nameof(CharacterMaster._money));
            FieldReference characterMasterMoneyFieldRef = il.Import(characterMasterMoneyField);

            int patchCount = 0;

            while (c.TryGotoNext(MoveType.Before,
                                 x => x.MatchStfld(characterMasterMoneyField)))
            {
                c.Emit(OpCodes.Stloc, newMoneyVar);
                c.Emit(OpCodes.Stloc, masterInstanceVar);

                c.Emit(OpCodes.Ldloc, masterInstanceVar);
                c.Emit(OpCodes.Ldfld, characterMasterMoneyFieldRef);
                c.Emit(OpCodes.Stloc, oldMoneyVar);

                c.Emit(OpCodes.Ldloc, masterInstanceVar);
                c.Emit(OpCodes.Ldloc, newMoneyVar);

                c.Index++;

                c.Emit(OpCodes.Ldloc, masterInstanceVar);
                c.Emit(OpCodes.Ldloc, oldMoneyVar);
                c.Emit(OpCodes.Ldloc, masterInstanceVar);
                c.Emit(OpCodes.Ldfld, characterMasterMoneyFieldRef);
                c.EmitDelegate(onSetMoney);
                static void onSetMoney(CharacterMaster masterInstance, uint oldMoney, uint newMoney)
                {
                    if (masterInstance && oldMoney != newMoney)
                    {
                        OnCharacterMoneyChanged?.Invoke(masterInstance, newMoney - (long)oldMoney);
                    }
                }

                patchCount++;
            }

            if (patchCount == 0)
            {
                Log.Warning("Found 0 patch locations");
            }
            else
            {
#if DEBUG
                Log.Debug($"Found {patchCount} patch locations");
#endif
            }
        }
    }
}
