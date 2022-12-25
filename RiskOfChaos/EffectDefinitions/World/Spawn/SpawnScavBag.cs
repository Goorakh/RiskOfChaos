using RiskOfChaos.EffectHandling;
using RiskOfChaos.Utility;
using RoR2;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
            DirectorPlacementRule placement = new DirectorPlacementRule
            {
                placementMode = DirectorPlacementRule.PlacementMode.NearestNode,
                position = SpawnUtils.GetRandomSpawnPosition(RNG, true) ?? Vector3.zero,
                minDistance = 50f,
                maxDistance = float.PositiveInfinity
            };

            DirectorCore.instance.TrySpawnObject(new DirectorSpawnRequest(_iscScavBackpack, placement, new Xoroshiro128Plus(RNG.nextUlong)));
        }
    }
}
