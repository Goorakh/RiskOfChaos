using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Methods;
using RiskOfChaos.EffectHandling.EffectComponents;
using RiskOfChaos.Utilities;
using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace RiskOfChaos.EffectDefinitions.World.Interactables
{
    [ChaosEffect("reposition_teleporter")]
    public sealed class RepositionTeleporter : NetworkBehaviour
    {
        [EffectCanActivate]
        static bool CanActivate()
        {
            return TeleporterInteraction.instance;
        }

        ChaosEffectComponent _effectComponent;

        [SyncVar(hook = nameof(setNewTeleporterPosition))]
        Vector3 _newTeleporterPosition;

        void Awake()
        {
            _effectComponent = GetComponent<ChaosEffectComponent>();
        }

        public override void OnStartServer()
        {
            base.OnStartServer();

            Xoroshiro128Plus rng = new Xoroshiro128Plus(_effectComponent.Rng.nextUlong);
            _newTeleporterPosition = SpawnUtils.GetBestValidRandomPlacementRule().EvaluateToPosition(rng);
        }

        void Start()
        {
            updateTeleporterPosition();
        }

        void setNewTeleporterPosition(Vector3 newTeleporterPosition)
        {
            _newTeleporterPosition = newTeleporterPosition;
            updateTeleporterPosition();
        }

        void updateTeleporterPosition()
        {
            if (TeleporterInteraction.instance)
            {
                TeleporterInteraction.instance.transform.position = _newTeleporterPosition;
            }
        }
    }
}
