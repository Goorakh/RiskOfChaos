using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.Patches;
using RoR2;
using UnityEngine;

namespace RiskOfChaos.EffectDefinitions.Character.Equipment
{
    [ChaosTimedEffect("disable_equipment_activation", 60f, AllowDuplicates = false)]
    public sealed class DisableEquipmentActivation : MonoBehaviour
    {
        void Start()
        {
            EquipmentDisabledHook.OverrideEquipmentDisabled += overrideEquipmentDisabled;
        }
        
        void OnDestroy()
        {
            EquipmentDisabledHook.OverrideEquipmentDisabled -= overrideEquipmentDisabled;
        }

        static void overrideEquipmentDisabled(Inventory inventory, ref bool isDisabled)
        {
            isDisabled = true;
        }
    }
}
