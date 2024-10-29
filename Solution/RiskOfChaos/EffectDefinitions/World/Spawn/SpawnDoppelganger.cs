using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.EffectHandling.EffectComponents;
using RoR2.Artifacts;
using UnityEngine.Networking;

namespace RiskOfChaos.EffectDefinitions.World.Spawn
{
    [ChaosEffect("spawn_doppelganger", DefaultSelectionWeight = 0.8f)]
    public sealed class SpawnDoppelganger : NetworkBehaviour
    {
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
            if (NetworkServer.active)
            {
                DoppelgangerInvasionManager.PerformInvasion(_rng);
            }
        }
    }
}
