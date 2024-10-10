using EntityStates.GoldGat;
using HarmonyLib;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using RiskOfChaos.EffectDefinitions.Character.Equipment;
using RiskOfChaos.EffectHandling.Controllers;
using RoR2;
using System.Reflection;

namespace RiskOfChaos.Patches.Effects.Character.Equipment
{
    static class DisableEquipmentActivationHooks
    {
        [SystemInitializer]
        static void Init()
        {
            On.RoR2.EquipmentSlot.PerformEquipmentAction += EquipmentSlot_PerformEquipmentAction;

            MethodInfo equipmentSlotStockGetter = AccessTools.DeclaredPropertyGetter(typeof(EquipmentSlot), nameof(EquipmentSlot.stock));
            if (equipmentSlotStockGetter != null)
            {
                new Hook(equipmentSlotStockGetter, EquipmentSlot_get_stock);
            }
            else
            {
                Log.Error("Failed to find EquipmentSlot stock getter method");
            }

            IL.EntityStates.GoldGat.BaseGoldGatState.CheckReturnToIdle += BaseGoldGatState_CheckReturnToIdle;
        }

        delegate int orig_EquipmentSlot_get_stock(EquipmentSlot self);
        static int EquipmentSlot_get_stock(orig_EquipmentSlot_get_stock orig, EquipmentSlot self)
        {
            int stock = orig(self);
            if (ChaosEffectTracker.Instance && ChaosEffectTracker.Instance.IsTimedEffectActive(DisableEquipmentActivation.EffectInfo))
                stock = 0;

            return stock;
        }

        static bool EquipmentSlot_PerformEquipmentAction(On.RoR2.EquipmentSlot.orig_PerformEquipmentAction orig, EquipmentSlot self, EquipmentDef equipmentDef)
        {
            if (ChaosEffectTracker.Instance && ChaosEffectTracker.Instance.IsTimedEffectActive(DisableEquipmentActivation.EffectInfo))
                return false;

            return orig(self, equipmentDef);
        }

        static void BaseGoldGatState_CheckReturnToIdle(ILContext il)
        {
            ILCursor c = new ILCursor(il);

            if (c.TryGotoNext(x => x.MatchCallOrCallvirt(AccessTools.DeclaredPropertyGetter(typeof(CharacterMaster), nameof(CharacterMaster.money)))))
            {
                ILLabel afterIfLabel = null;
                if (c.TryGotoNext(MoveType.After,
                                  x => x.MatchBleUn(out afterIfLabel)))
                {
                    c.MoveAfterLabels();

                    c.Emit(OpCodes.Ldarg_0);
                    c.EmitDelegate(isOutOfEquipmentStocks);
                    static bool isOutOfEquipmentStocks(BaseGoldGatState state)
                    {
                        return state.bodyEquipmentSlot && state.bodyEquipmentSlot.stock <= 0;
                    }

                    c.Emit(OpCodes.Brtrue, afterIfLabel);
                }
                else
                {
                    Log.Error("[GoldGat equipment stock] Failed to find patch location");
                }
            }
            else
            {
                Log.Error("[GoldGat equipment stock] Failed to find get_money call");
            }
        }
    }
}
