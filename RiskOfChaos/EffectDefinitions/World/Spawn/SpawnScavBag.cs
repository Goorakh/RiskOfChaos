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
            DirectorPlacementRule placementRule = new DirectorPlacementRule();

            CharacterBody[] playerBodies = PlayerUtils.GetAllPlayerBodies(true).ToArray();
            if (playerBodies.Length > 0)
            {
                CharacterBody selectedPlayer = RNG.NextElementUniform(playerBodies);

                placementRule.placementMode = DirectorPlacementRule.PlacementMode.NearestNode;
                placementRule.position = selectedPlayer.footPosition;
                placementRule.minDistance = 50f;
                placementRule.maxDistance = float.PositiveInfinity;
            }
            else
            {
                placementRule.placementMode = SpawnUtils.GetBestValidRandomPlacementType();
            }
            
            DirectorCore.instance.TrySpawnObject(new DirectorSpawnRequest(_iscScavBackpack, placementRule, new Xoroshiro128Plus(RNG.nextUlong)));
        }
    }
}
