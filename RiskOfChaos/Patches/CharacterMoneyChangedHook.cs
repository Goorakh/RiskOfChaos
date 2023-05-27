using HarmonyLib;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using RoR2;
using System;
using System.Collections.Generic;
using System.Text;

namespace RiskOfChaos.Patches
{
    static class CharacterMoneyChangedHook
    {
        public delegate void OnCharacterMoneyChangedDelegate(CharacterMaster master, int moneyDiff);
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

#pragma warning disable Publicizer001 // Accessing a member that was not originally public
            const string MONEY_FIELD_NAME = nameof(CharacterMaster._money);
#pragma warning restore Publicizer001 // Accessing a member that was not originally public

            while (c.TryGotoNext(MoveType.Before, x => x.MatchStfld<CharacterMaster>(MONEY_FIELD_NAME)))
            {
                CharacterMaster masterInstance = null;
                uint oldMoney = 0;
                uint newMoney = 0;

                c.EmitDelegate((uint _newMoney) =>
                {
                    newMoney = _newMoney;
                });

                c.Emit(OpCodes.Dup);
                c.EmitDelegate((CharacterMaster instance) =>
                {
                    oldMoney = instance.money;
                    masterInstance = instance;
                });

                c.EmitDelegate(() => newMoney);

                c.Index++;

                c.EmitDelegate(() =>
                {
                    if (masterInstance)
                    {
                        OnCharacterMoneyChanged?.Invoke(masterInstance, (int)newMoney - (int)oldMoney);
                    }
                });
            }
        }
    }
}
