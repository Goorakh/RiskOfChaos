using HarmonyLib;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using RiskOfChaos.EffectHandling;
using RiskOfChaos.EffectHandling.Controllers;
using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Data;
using RiskOfChaos.Patches;
using RoR2;
using RoR2.Skills;
using RoR2.UI;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Networking;

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

            IL.RoR2.Items.MultiShopCardUtils.OnPurchase += il =>
            {
                ILCursor c = new ILCursor(il);

                ILLabel afterCardLogicLabel = null;
                if (c.TryGotoNext(MoveType.After,
                                  x => x.MatchCallOrCallvirt(AccessTools.DeclaredPropertyGetter(typeof(EquipmentSlot), nameof(EquipmentSlot.stock))),
                                  x => x.MatchLdcI4(0),
                                  x => x.MatchBle(out afterCardLogicLabel)))
                {
                    c.EmitDelegate(() =>
                    {
                        return TimedChaosEffectHandler.Instance && TimedChaosEffectHandler.Instance.IsTimedEffectActive(EffectInfo);
                    });

                    c.Emit(OpCodes.Brtrue, afterCardLogicLabel);
                }
                else
                {
                    Log.Error("Failed to find card logic patch location");
                }
            };
        }

        static bool _appliedServerPatches;
        static void tryApplyServerPatches()
        {
            if (_appliedServerPatches)
                return;

            On.RoR2.EquipmentSlot.PerformEquipmentAction += (orig, self, equipmentDef) =>
            {
                if (TimedChaosEffectHandler.Instance && TimedChaosEffectHandler.Instance.IsTimedEffectActive(EffectInfo))
                    return false;

                return orig(self, equipmentDef);
            };

            _appliedServerPatches = true;
        }

        public override void OnStart()
        {
            if (NetworkServer.active)
            {
                tryApplyServerPatches();
            }

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
