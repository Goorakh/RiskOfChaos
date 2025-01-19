using RiskOfChaos.Content;
using RiskOfChaos.Utilities.Extensions;
using RoR2;
using RoR2.Navigation;
using System.Collections;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace RiskOfChaos.EffectUtils.World.Spawn
{
    public static class CustomSpawnCards
    {
        public static InteractableSpawnCard iscGeodeFixed { get; private set; }

        public static InteractableSpawnCard iscTimedChest { get; private set; }

        [SystemInitializer]
        static IEnumerator Init()
        {
            AsyncOperationHandle<InteractableSpawnCard> iscGeodeLoad = Addressables.LoadAssetAsync<InteractableSpawnCard>("RoR2/DLC2/iscGeode.asset");
            iscGeodeLoad.OnSuccess(iscGeode =>
            {
                iscGeodeFixed = ScriptableObject.Instantiate(iscGeode);
                iscGeodeFixed.name = "iscGeodeFixed";
                iscGeodeFixed.orientToFloor = false; // causes it to have really strange rotation since the raycast to find the floor normal hits itself
                iscGeodeFixed.occupyPosition = true;
            });

            iscTimedChest = ScriptableObject.CreateInstance<InteractableSpawnCard>();
            iscTimedChest.name = "iscTimedChest";
            iscTimedChest.prefab = RoCContent.NetworkedPrefabs.TimedChestFixedOrigin;
            iscTimedChest.sendOverNetwork = true;
            iscTimedChest.hullSize = HullClassification.Golem;
            iscTimedChest.nodeGraphType = MapNodeGroup.GraphType.Ground;
            iscTimedChest.requiredFlags = NodeFlags.None;
            iscTimedChest.forbiddenFlags = NodeFlags.NoChestSpawn;
            iscTimedChest.occupyPosition = true;
            iscTimedChest.orientToFloor = false;
            iscTimedChest.slightlyRandomizeOrientation = false;

            AsyncOperationHandle[] asyncOperations = [iscGeodeLoad];
            yield return asyncOperations.WaitForAllLoaded();
        }
    }
}
