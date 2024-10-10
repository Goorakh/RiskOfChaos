using RiskOfChaos.EffectHandling;
using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Data;
using RiskOfChaos.Patches;
using RoR2;
using RoR2.Skills;
using RoR2.UI;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Networking;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace RiskOfChaos.EffectDefinitions.Character.Equipment
{
    [ChaosTimedEffect("disable_equipment_activation", 60f, AllowDuplicates = false)]
    public sealed class DisableEquipmentActivation : NetworkBehaviour
    {
        [InitEffectInfo]
        public static readonly TimedEffectInfo EffectInfo;

        static Texture _lockedIconTexture;

        [SystemInitializer]
        static void Init()
        {
            AsyncOperationHandle<CaptainSupplyDropSkillDef> prepSupplyDropLoad = Addressables.LoadAssetAsync<CaptainSupplyDropSkillDef>("RoR2/Base/Captain/PrepSupplyDrop.asset");
            prepSupplyDropLoad.Completed += handle =>
            {
                CaptainSupplyDropSkillDef supplyDropSkillDef = handle.Result;
                if (!supplyDropSkillDef)
                {
                    Log.Error("Failed to load captain supply drop SkillDef");
                    return;
                }

                Sprite exhaustedIcon = supplyDropSkillDef.exhaustedIcon;
                if (!exhaustedIcon)
                {
                    Log.Error("Missing exhausted icon");
                    return;
                }

                _lockedIconTexture = exhaustedIcon.texture;
            };
        }

        void Start()
        {
            OverrideEquipmentIconHook.OverrideEquipmentIcon += overrideEquipmentIcon;
        }

        void OnDestroy()
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
