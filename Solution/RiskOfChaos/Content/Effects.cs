using RiskOfChaos.Components;
using RiskOfChaos.Content.AssetCollections;
using RiskOfChaos.Utilities;
using RiskOfChaos.Utilities.Extensions;
using RoR2;
using RoR2.Orbs;
using System.Collections;
using UnityEngine;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace RiskOfChaos.Content
{
    partial class RoCContent
    {
        partial class Effects
        {
            [ContentInitializer]
            static IEnumerator LoadContent(EffectDefAssetCollection effectDefs)
            {
                // EquipmentTransferOrb
                {
                    AsyncOperationHandle<GameObject> transferOrbEffectLoad = AddressableUtil.LoadTempAssetAsync<GameObject>(AddressableGuids.RoR2_Base_Common_VFX_ItemTransferOrbEffect_prefab);
                    transferOrbEffectLoad.OnSuccess(itemTransferOrbEffectPrefab =>
                    {
                        GameObject prefab = itemTransferOrbEffectPrefab.InstantiatePrefab(nameof(EquipmentTransferOrbEffect));

                        ItemTakenOrbEffect itemTakenEffect = prefab.GetComponent<ItemTakenOrbEffect>();

                        EquipmentTakenOrbEffect equipmentTakenEffect = prefab.AddComponent<EquipmentTakenOrbEffect>();

                        equipmentTakenEffect.TrailToColor = itemTakenEffect.trailToColor;
                        equipmentTakenEffect.ParticlesToColor = itemTakenEffect.particlesToColor;
                        equipmentTakenEffect.SpritesToColor = itemTakenEffect.spritesToColor;
                        equipmentTakenEffect.IconSpriteRenderer = itemTakenEffect.iconSpriteRenderer;

                        GameObject.Destroy(itemTakenEffect);

                        effectDefs.Add(new EffectDef(prefab));
                    });

                    return transferOrbEffectLoad;
                }
            }
        }
    }
}
