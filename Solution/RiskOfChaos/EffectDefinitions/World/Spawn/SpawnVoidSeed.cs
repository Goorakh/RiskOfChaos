using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Methods;
using RiskOfChaos.EffectHandling.EffectComponents;
using RiskOfChaos.Patches;
using RiskOfChaos.Utilities;
using RiskOfChaos.Utilities.Extensions;
using RoR2;
using RoR2.Navigation;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Networking;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace RiskOfChaos.EffectDefinitions.World.Spawn
{
    [ChaosEffect("spawn_void_seed", DefaultSelectionWeight = 0.6f)]
    public sealed class SpawnVoidSeed : NetworkBehaviour
    {
        static InteractableSpawnCard _iscVoidCamp;

        [SystemInitializer]
        static void Init()
        {
            AsyncOperationHandle<InteractableSpawnCard> iscVoidCampLoad = Addressables.LoadAssetAsync<InteractableSpawnCard>("RoR2/DLC1/VoidCamp/iscVoidCamp.asset");
            iscVoidCampLoad.OnSuccess(iscVoidCamp =>
            {
                _iscVoidCamp = Instantiate(iscVoidCamp);
                _iscVoidCamp.requiredFlags = NodeFlags.None;
                _iscVoidCamp.forbiddenFlags = NodeFlags.None;
                _iscVoidCamp.hullSize = HullClassification.Human;
            });
        }

        [EffectCanActivate]
        static bool CanActivate()
        {
            return ExpansionUtils.DLC1Enabled && _iscVoidCamp && DirectorCore.instance && SpawnUtils.HasValidSpawnLocation(_iscVoidCamp);
        }

        ChaosEffectComponent _effectComponent;

        Xoroshiro128Plus _rng;

        void Awake()
        {
            _effectComponent = GetComponent<ChaosEffectComponent>();
        }

        public override void OnStartServer()
        {
            base.OnStartServer();

            _rng = new Xoroshiro128Plus(_effectComponent.Rng.nextUlong);
        }

        void Start()
        {
            if (!NetworkServer.active)
                return;

            DirectorPlacementRule placementRule = SpawnUtils.GetBestValidRandomPlacementRule();

            DirectorSpawnRequest spawnRequest = new DirectorSpawnRequest(_iscVoidCamp, placementRule, _rng);

            GameObject voidSeedObj = DirectorCore.instance.TrySpawnObject(spawnRequest);
            if (voidSeedObj && Configs.EffectSelection.SeededEffectSelection.Value)
            {
                RNGOverridePatch.OverrideRNG(voidSeedObj, new Xoroshiro128Plus(_rng.nextUlong));
            }
        }
    }
}
