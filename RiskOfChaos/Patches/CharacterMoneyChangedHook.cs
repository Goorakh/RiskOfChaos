using HarmonyLib;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using RoR2;

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

#pragma warning disable Publicizer001 // Accessing a member that was not originally public
            const string MONEY_FIELD_NAME = nameof(CharacterMaster._money);
#pragma warning restore Publicizer001 // Accessing a member that was not originally public

            while (c.TryGotoNext(MoveType.Before, x => x.MatchStfld<CharacterMaster>(MONEY_FIELD_NAME)))
            {
                c.Emit(OpCodes.Stloc, newMoneyVar);
                c.Emit(OpCodes.Stloc, masterInstanceVar);

                c.Emit(OpCodes.Ldloc, masterInstanceVar);
                c.EmitDelegate((CharacterMaster masterInstance) =>
                {
                    return masterInstance.money;
                });
                c.Emit(OpCodes.Stloc, oldMoneyVar);

                c.Emit(OpCodes.Ldloc, masterInstanceVar);
                c.Emit(OpCodes.Ldloc, newMoneyVar);

                c.Index++;

                c.Emit(OpCodes.Ldloc, masterInstanceVar);
                c.Emit(OpCodes.Ldloc, oldMoneyVar);
                c.Emit(OpCodes.Ldloc, newMoneyVar);
                c.EmitDelegate((CharacterMaster masterInstance, uint oldMoney, uint newMoney) =>
                {
                    if (masterInstance && oldMoney != newMoney)
                    {
                        OnCharacterMoneyChanged?.Invoke(masterInstance, newMoney - (long)oldMoney);
                    }
                });
            }
        }
    }
}
