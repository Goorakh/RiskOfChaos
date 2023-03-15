using RiskOfChaos.EffectHandling;
using RiskOfChaos.Utilities;
using RoR2;
using System.Linq;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace RiskOfChaos.EffectDefinitions.World.Spawn
{
    [ChaosEffect("SpawnScavBag", DefaultSelectionWeight = 0.6f)]
    public class SpawnScavBag : BaseEffect
    {
        static readonly SpawnCard _iscScavBackpack = Addressables.LoadAssetAsync<InteractableSpawnCard>("RoR2/Base/Scav/iscScavBackpack.asset").WaitForCompletion();

        [EffectCanActivate]
        static bool CanActivate()
        {
            return _iscScavBackpack && DirectorCore.instance;
        }

        public override void OnStart()
        {
            DirectorSpawnRequest spawnRequest = new DirectorSpawnRequest(_iscScavBackpack, SpawnUtils.GetPlacementRule_AtRandomPlayerDirect(RNG), new Xoroshiro128Plus(RNG.nextUlong));

            if (!DirectorCore.instance.TrySpawnObject(spawnRequest))
            {
                spawnRequest.placementRule = new DirectorPlacementRule
                {
                    placementMode = SpawnUtils.GetBestValidRandomPlacementType()
                };

                DirectorCore.instance.TrySpawnObject(spawnRequest);
            }
        }
    }
}
