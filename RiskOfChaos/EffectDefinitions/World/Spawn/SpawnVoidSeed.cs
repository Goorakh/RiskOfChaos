using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Methods;
using RiskOfChaos.Utilities;
using RoR2;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace RiskOfChaos.EffectDefinitions.World.Spawn
{
    [ChaosEffect("spawn_void_seed", DefaultSelectionWeight = 0.6f, EffectWeightReductionPercentagePerActivation = 30f)]
    public sealed class SpawnVoidSeed : BaseEffect
    {
        static InteractableSpawnCard _iscVoidCamp;

        [SystemInitializer]
        static void Init()
        {
            AsyncOperationHandle<InteractableSpawnCard> iscVoidCampLoadHandle = Addressables.LoadAssetAsync<InteractableSpawnCard>("RoR2/DLC1/VoidCamp/iscVoidCamp.asset");
            iscVoidCampLoadHandle.Completed += handle =>
            {
                _iscVoidCamp = handle.Result;
            };
        }

        [EffectCanActivate]
        static bool CanActivate()
        {
            return ExpansionUtils.DLC1Enabled && _iscVoidCamp && DirectorCore.instance;
        }

        public override void OnStart()
        {
            DirectorPlacementRule placementRule = SpawnUtils.GetBestValidRandomPlacementRule();

            DirectorSpawnRequest spawnRequest = new DirectorSpawnRequest(_iscVoidCamp, placementRule, RNG);

            DirectorCore.instance.TrySpawnObject(spawnRequest);
        }
    }
}
