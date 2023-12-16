using R2API;
using RiskOfChaos.Components;
using RoR2;
using RoR2.ContentManagement;
using RoR2.Orbs;
using System;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace RiskOfChaos.Content
{
    public static class Effects
    {
        public static readonly EffectDef EquipmentTransferOrbEffect;

        static Effects()
        {
            // EquipmentTransferOrb
            {
                GameObject prefab = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/Common/VFX/ItemTransferOrbEffect.prefab").WaitForCompletion().InstantiateClone(Main.PluginGUID + "_EquipmentTransferOrbEffect", false);

                ItemTakenOrbEffect itemTakenEffect = prefab.GetComponent<ItemTakenOrbEffect>();

                EquipmentTakenOrbEffect equipmentTakenEffect = prefab.AddComponent<EquipmentTakenOrbEffect>();

                equipmentTakenEffect.TrailToColor = itemTakenEffect.trailToColor;
                equipmentTakenEffect.ParticlesToColor = itemTakenEffect.particlesToColor;
                equipmentTakenEffect.SpritesToColor = itemTakenEffect.spritesToColor;
                equipmentTakenEffect.IconSpriteRenderer = itemTakenEffect.iconSpriteRenderer;

                GameObject.Destroy(itemTakenEffect);

                EquipmentTransferOrbEffect = new EffectDef(prefab);
            }
        }

        internal static void AddEffectDefsTo(NamedAssetCollection<EffectDef> effectDefs)
        {
            effectDefs.Add(new EffectDef[]
            {
                EquipmentTransferOrbEffect
            });
        }
    }
}
