﻿using RiskOfChaos.EffectHandling;
using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Data;
using RiskOfChaos.Patches;
using RiskOfChaos.Utilities;
using RiskOfChaos.Utilities.Extensions;
using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace RiskOfChaos.EffectDefinitions.Character.Equipment
{
    [ChaosTimedEffect("no_equipment_cooldown", 60f, AllowDuplicates = false)]
    public sealed class NoEquipmentCooldown : MonoBehaviour
    {
        [InitEffectInfo]
        public static readonly TimedEffectInfo EffectInfo;

        void Start()
        {
            if (NetworkServer.active)
            {
                CharacterBody.readOnlyInstancesList.TryDo(skipActiveEquipmentCooldowns, FormatUtils.GetBestBodyName);
            }

            InventoryHooks.OverrideEquipmentCooldownScale += overrideEquipmentCooldownScale;
        }

        void OnDestroy()
        {
            InventoryHooks.OverrideEquipmentCooldownScale -= overrideEquipmentCooldownScale;
        }

        static void skipActiveEquipmentCooldowns(CharacterBody body)
        {
            Inventory inventory = body.inventory;
            if (!inventory)
                return;

            int equipmentSlotCount = inventory.GetEquipmentSlotCount();
            for (byte i = 0; i < equipmentSlotCount; i++)
            {
                EquipmentState equipmentState = inventory.GetEquipment(i);
                if (equipmentState.equipmentIndex != EquipmentIndex.None)
                {
                    inventory.RestockEquipmentCharges(i, int.MaxValue);
                }
            }
        }

        static void overrideEquipmentCooldownScale(Inventory inventory, ref float cooldownScale)
        {
            cooldownScale = 0f;
        }
    }
}
