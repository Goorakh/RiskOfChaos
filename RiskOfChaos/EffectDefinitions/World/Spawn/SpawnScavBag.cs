using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Methods;
using RiskOfChaos.Utilities;
using RoR2;
using UnityEngine.AddressableAssets;

namespace RiskOfChaos.EffectDefinitions.World.Spawn
{
    [ChaosEffect("spawn_scav_bag", DefaultSelectionWeight = 0.6f)]
    public sealed class SpawnScavBag : BaseEffect
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
