using RiskOfChaos.EffectHandling;
using RoR2;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace RiskOfChaos.EffectDefinitions
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
            Vector3 spawnPosition;

            IEnumerable<CharacterBody> playerBodies = PlayerUtils.GetAllPlayerBodies(true);
            if (playerBodies.Any())
            {
                spawnPosition = RNG.NextElementUniform(playerBodies.ToArray()).footPosition;
            }
            else
            {
                spawnPosition = Vector3.zero;
            }

            DirectorPlacementRule placement = new DirectorPlacementRule
            {
                placementMode = DirectorPlacementRule.PlacementMode.NearestNode,
                position = spawnPosition,
                minDistance = 50f,
                maxDistance = float.PositiveInfinity
            };

            DirectorCore.instance.TrySpawnObject(new DirectorSpawnRequest(_iscScavBackpack, placement, new Xoroshiro128Plus(RNG.nextUlong)));
        }
    }
}
