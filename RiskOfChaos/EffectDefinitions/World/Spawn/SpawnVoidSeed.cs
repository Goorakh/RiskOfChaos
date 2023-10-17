using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Methods;
using RiskOfChaos.Patches;
using RiskOfChaos.Utilities;
using RoR2;
using RoR2.Navigation;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace RiskOfChaos.EffectDefinitions.World.Spawn
{
    [ChaosEffect("spawn_void_seed", DefaultSelectionWeight = 0.6f, EffectWeightReductionPercentagePerActivation = 30f)]
    public sealed class SpawnVoidSeed : BaseEffect
    {
        static InteractableSpawnCard _iscVoidCamp;

        [SystemInitializer]
        static void Init()
        {
            InteractableSpawnCard iscVoidCamp = Addressables.LoadAssetAsync<InteractableSpawnCard>("RoR2/DLC1/VoidCamp/iscVoidCamp.asset").WaitForCompletion();
            if (iscVoidCamp)
            {
                _iscVoidCamp = GameObject.Instantiate(iscVoidCamp);
                _iscVoidCamp.requiredFlags = NodeFlags.None;
                _iscVoidCamp.forbiddenFlags = NodeFlags.None;
                _iscVoidCamp.hullSize = HullClassification.Human;
            }
            else
            {
                Log.Error("Could not load iscVoidCamp asset");
            }
        }

        [EffectCanActivate]
        static bool CanActivate()
        {
            return ExpansionUtils.DLC1Enabled && _iscVoidCamp && DirectorCore.instance && SpawnUtils.HasValidSpawnLocation(_iscVoidCamp);
        }

        public override void OnStart()
        {
            DirectorPlacementRule placementRule = SpawnUtils.GetBestValidRandomPlacementRule();

            DirectorSpawnRequest spawnRequest = new DirectorSpawnRequest(_iscVoidCamp, placementRule, new Xoroshiro128Plus(RNG.nextUlong));

            GameObject voidSeedObj = DirectorCore.instance.TrySpawnObject(spawnRequest);
            if (voidSeedObj && Configs.General.SeededEffectSelection.Value)
            {
                VoidCampOverrideRNGSeedPatch.OverrideRNG(voidSeedObj, new Xoroshiro128Plus(RNG.nextUlong));
            }
        }
    }
}
