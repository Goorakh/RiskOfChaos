using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.EffectHandling.EffectComponents;
using RiskOfChaos.Utilities;
using RiskOfChaos.Utilities.Extensions;
using RoR2;
using RoR2.Navigation;
using UnityEngine;
using UnityEngine.Networking;

namespace RiskOfChaos.EffectDefinitions.Character.Player
{
    [ChaosEffect("tp_random_location")]
    public sealed class TpRandomLocation : NetworkBehaviour
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
                DirectorPlacementRule positionSelectorPlacementRule = SpawnUtils.GetBestValidRandomPlacementRule();

                PlayerUtils.GetAllPlayerBodies(true).TryDo(playerBody =>
                {
                    teleportToRandomLocation(playerBody, positionSelectorPlacementRule);
                }, FormatUtils.GetBestBodyName);
            }
        }

        void teleportToRandomLocation(CharacterBody playerBody, DirectorPlacementRule positionSelectorPlacementRule)
        {
            teleportToPosition(playerBody, generateTargetPosition(playerBody, positionSelectorPlacementRule));
        }

        Vector3 generateTargetPosition(CharacterBody playerBody, DirectorPlacementRule positionSelectorPlacementRule)
        {
            WeightedSelection<MapNodeGroup.GraphType> graphTypeSelection = new WeightedSelection<MapNodeGroup.GraphType>();

            const float OPPOSITE_GRAPH_TYPE_WEIGHT = 0.5f;
            bool isFlying = playerBody.isFlying;
            graphTypeSelection.AddChoice(MapNodeGroup.GraphType.Air, isFlying ? 1f : OPPOSITE_GRAPH_TYPE_WEIGHT);
            graphTypeSelection.AddChoice(MapNodeGroup.GraphType.Ground, !isFlying ? 1f : OPPOSITE_GRAPH_TYPE_WEIGHT);

            return positionSelectorPlacementRule.EvaluateToPosition(_rng.Branch(), playerBody.hullClassification, graphTypeSelection.Evaluate(_rng.nextNormalizedFloat));
        }

        static void teleportToPosition(CharacterBody playerBody, Vector3 targetPosition)
        {
            if (playerBody.currentVehicle)
            {
                playerBody.currentVehicle.EjectPassenger();
            }

            TeleportUtils.TeleportBody(playerBody, targetPosition);
        }
    }
}
