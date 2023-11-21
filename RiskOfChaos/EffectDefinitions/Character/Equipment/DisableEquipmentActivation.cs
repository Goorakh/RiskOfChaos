using HarmonyLib;
using MonoMod.RuntimeDetour;
using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.Patches;
using RoR2;
using RoR2.Skills;
using RoR2.UI;
using System;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Networking;

namespace RiskOfChaos.EffectDefinitions.Character.Equipment
{
    [ChaosTimedEffect("disable_equipment_activation", 60f, AllowDuplicates = false, IsNetworked = true)]
    public sealed class DisableEquipmentActivation : TimedEffect
    {
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

        bool _addedServerHooks;

        public override void OnStart()
        {
            if (NetworkServer.active)
            {
                On.RoR2.EquipmentSlot.PerformEquipmentAction += EquipmentSlot_PerformEquipmentAction;
                _addedServerHooks = true;
            }

            OverrideEquipmentIconHook.OverrideEquipmentIcon += overrideEquipmentIcon;
        }

        public override void OnEnd()
        {
            if (_addedServerHooks)
            {
                On.RoR2.EquipmentSlot.PerformEquipmentAction -= EquipmentSlot_PerformEquipmentAction;
            }

            OverrideEquipmentIconHook.OverrideEquipmentIcon -= overrideEquipmentIcon;
        }

        static bool EquipmentSlot_PerformEquipmentAction(On.RoR2.EquipmentSlot.orig_PerformEquipmentAction orig, EquipmentSlot self, EquipmentDef equipmentDef)
        {
            return false;
        }

        static void overrideEquipmentIcon(in EquipmentIcon.DisplayData displayData, in OverrideEquipmentIconHook.IconOverrideInfo info)
        {
            if (!displayData.hasEquipment)
                return;

            if (info.IconImage)
            {
                if (_lockedIconTexture)
                {
                    info.IconImage.texture = _lockedIconTexture;
                    info.IconImage.color = Color.white;
                    info.IconImage.uvRect = new Rect(0f, 0f, 0.25f, 1f);
                }
                else
                {
                    info.IconImage.color = Color.red;
                }
            }
        }
    }
}
