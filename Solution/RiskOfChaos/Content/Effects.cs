﻿using RiskOfChaos.Components;
using RiskOfChaos.Content.AssetCollections;
using RiskOfChaos.Utilities.Extensions;
using RoR2;
using RoR2.Orbs;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
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
                List<AsyncOperationHandle> asyncOperations = [];

                // EquipmentTransferOrb
                {
                    AsyncOperationHandle<GameObject> transferOrbEffectLoad = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/Common/VFX/ItemTransferOrbEffect.prefab");
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

                    asyncOperations.Add(transferOrbEffectLoad);
                }

                yield return asyncOperations.WaitForAllLoaded();
            }
        }
    }
}
