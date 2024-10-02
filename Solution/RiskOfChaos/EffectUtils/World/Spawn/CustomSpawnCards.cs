using RoR2;
using System.Collections;
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
            AsyncOperationHandle<InteractableSpawnCard> iscGeodeLoad = Addressables.LoadAssetAsync<InteractableSpawnCard>("RoR2/DLC2/iscGeode.asset");

            while (!iscGeodeLoad.IsDone)
            {
                yield return null;
            }

            iscGeodeFixed = ScriptableObject.Instantiate(iscGeodeLoad.Result);
            iscGeodeFixed.name = "iscGeodeFixed";
            iscGeodeFixed.orientToFloor = false; // causes it to have really strange rotation since the raycast to find the floor normal hits itself
            iscGeodeFixed.occupyPosition = true;

            yield break;
        }
    }
}
