using EntityStates.GoldGat;
using HarmonyLib;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using RiskOfChaos.EffectHandling;
using RiskOfChaos.EffectHandling.Controllers;
using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Data;
using RiskOfChaos.Patches;
using RoR2;
using RoR2.Skills;
using RoR2.UI;
using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace RiskOfChaos.EffectDefinitions.Character.Equipment
{
    [ChaosTimedEffect("disable_equipment_activation", 60f, AllowDuplicates = false, IsNetworked = true)]
    public sealed class DisableEquipmentActivation : TimedEffect
    {
        [InitEffectInfo]
        public static new readonly TimedEffectInfo EffectInfo;

        static Texture _lockedIconTexture;

        [SystemInitializer]
        static void Init()
        {
            CaptainSupplyDropSkillDef supplyDropSkillDef = Addressables.LoadAssetAsync<CaptainSupplyDropSkillDef>("RoR2/Base/Captain/PrepSupplyDrop.asset").WaitForCompletion();
            if (supplyDropSkillDef)
            {
                Sprite exhaustedIcon = supplyDropSkillDef.exhaustedIcon;
                if (exhaustedIcon)
                {
                    _lockedIconTexture = exhaustedIcon.texture;
                }
            }
        }

        static bool _appliedPatches;
        static void tryApplyPatches()
        {
            if (_appliedPatches)
                return;

            _appliedPatches = true;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            static bool isEffectActive()
            {
                return TimedChaosEffectHandler.Instance && TimedChaosEffectHandler.Instance.IsTimedEffectActive(EffectInfo);
            }

            MethodInfo equipmentSlotStockGetter = AccessTools.DeclaredPropertyGetter(typeof(EquipmentSlot), nameof(EquipmentSlot.stock));
            if (equipmentSlotStockGetter != null)
            {
                new Hook(equipmentSlotStockGetter, (Func<EquipmentSlot, int> orig, EquipmentSlot self) =>
                {
                    int stock = orig(self);
                    if (isEffectActive())
                        stock = 0;

                    return stock;
                });
            }
            else
            {
                Log.Error("Failed to find EquipmentSlot stock getter method");
            }

            IL.EntityStates.GoldGat.BaseGoldGatState.CheckReturnToIdle += il =>
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
            };

            On.RoR2.EquipmentSlot.PerformEquipmentAction += (orig, self, equipmentDef) =>
            {
                if (isEffectActive())
                    return false;

                return orig(self, equipmentDef);
            };
        }

        public override void OnStart()
        {
            tryApplyPatches();

            OverrideEquipmentIconHook.OverrideEquipmentIcon += overrideEquipmentIcon;
        }

        public override void OnEnd()
        {
            OverrideEquipmentIconHook.OverrideEquipmentIcon -= overrideEquipmentIcon;
        }

        static void overrideEquipmentIcon(in EquipmentIcon.DisplayData displayData, ref OverrideEquipmentIconHook.IconOverrideInfo info)
        {
            if (!displayData.hasEquipment)
                return;

            if (_lockedIconTexture)
            {
                info.IconOverride = _lockedIconTexture;
                info.IconRectOverride = new Rect(0f, 0f, 0.25f, 1f);
            }
            else
            {
                info.IconColorOverride = Color.red;
            }
        }
    }
}
