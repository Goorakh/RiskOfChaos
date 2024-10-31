using RiskOfChaos.Utilities.Extensions;
using RoR2;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace RiskOfChaos.EffectUtils.World.Spawn
{
    public static class CustomSpawnCards
    {
        public static InteractableSpawnCard iscGeodeFixed { get; private set; }

        [SystemInitializer]
        static IEnumerator Init()
        {
            List<AsyncOperationHandle> asyncOperations = new List<AsyncOperationHandle>(1);

            AsyncOperationHandle<InteractableSpawnCard> iscGeodeLoad = Addressables.LoadAssetAsync<InteractableSpawnCard>("RoR2/DLC2/iscGeode.asset");
            iscGeodeLoad.OnSuccess(iscGeode =>
            {
                iscGeodeFixed = ScriptableObject.Instantiate(iscGeode);
                iscGeodeFixed.name = "iscGeodeFixed";
                iscGeodeFixed.orientToFloor = false; // causes it to have really strange rotation since the raycast to find the floor normal hits itself
                iscGeodeFixed.occupyPosition = true;
            });

            asyncOperations.Add(iscGeodeLoad);

            yield return asyncOperations.WaitForAllLoaded();
        }
    }
}
