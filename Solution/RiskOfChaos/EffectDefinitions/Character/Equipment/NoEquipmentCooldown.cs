using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.Utilities;
using RiskOfChaos.Utilities.Extensions;
using RoR2;

namespace RiskOfChaos.EffectDefinitions.Character.Equipment
{
    [ChaosTimedEffect("no_equipment_cooldown", 60f, AllowDuplicates = false)]
    public sealed class NoEquipmentCooldown : TimedEffect
    {
        public override void OnStart()
        {
            CharacterBody.readOnlyInstancesList.TryDo(body =>
            {
                Inventory inventory = body.inventory;
                if (!inventory)
                    return;

                int equipmentSlotCount = inventory.GetEquipmentSlotCount();
                for (uint i = 0; i < equipmentSlotCount; i++)
                {
                    EquipmentState equipmentState = inventory.GetEquipment(i);
                    if (equipmentState.equipmentIndex != EquipmentIndex.None)
                    {
                        inventory.SetEquipment(new EquipmentState(equipmentState.equipmentIndex, Run.FixedTimeStamp.now, equipmentState.charges), i);
                    }
                }
            }, FormatUtils.GetBestBodyName);

            On.RoR2.Inventory.CalculateEquipmentCooldownScale += Inventory_CalculateEquipmentCooldownScale;
        }

        public override void OnEnd()
        {
            On.RoR2.Inventory.CalculateEquipmentCooldownScale -= Inventory_CalculateEquipmentCooldownScale;
        }

        static float Inventory_CalculateEquipmentCooldownScale(On.RoR2.Inventory.orig_CalculateEquipmentCooldownScale orig, Inventory self)
        {
            orig(self);
            return 0f;
        }
    }
}
