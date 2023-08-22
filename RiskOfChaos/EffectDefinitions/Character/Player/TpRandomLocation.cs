using RiskOfChaos.EffectHandling;
using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Methods;
using RiskOfChaos.Utilities;
using RiskOfChaos.Utilities.Extensions;
using RoR2;
using RoR2.Navigation;
using System.Linq;
using UnityEngine;

namespace RiskOfChaos.EffectDefinitions.Character.Player
{
    [ChaosEffect("tp_random_location")]
    public sealed class TpRandomLocation : BaseEffect
    {
        [EffectCanActivate]
        static bool CanSelect(in EffectCanActivateContext context)
        {
            return !context.IsNow || (DirectorCore.instance && PlayerUtils.GetAllPlayerBodies(true).Any());
        }

        public override void OnStart()
        {
            DirectorPlacementRule.PlacementMode placementMode = SpawnUtils.GetBestValidRandomPlacementType();

            PlayerUtils.GetAllPlayerBodies(true).TryDo(playerBody =>
            {
                teleportToRandomLocation(playerBody, placementMode);
            }, FormatUtils.GetBestBodyName);
        }

        void teleportToRandomLocation(CharacterBody playerBody, DirectorPlacementRule.PlacementMode targetPositionPlacementMode)
        {
            teleportToPosition(playerBody, generateTargetPosition(playerBody, targetPositionPlacementMode));
        }

        Vector3 generateTargetPosition(CharacterBody playerBody, DirectorPlacementRule.PlacementMode targetPositionPlacementMode)
        {
            DirectorPlacementRule positionSelectorPlacementRule = new DirectorPlacementRule
            {
                placementMode = targetPositionPlacementMode,
                position = playerBody.footPosition,
                minDistance = 50f
            };

            WeightedSelection<MapNodeGroup.GraphType> graphTypeSelection = new WeightedSelection<MapNodeGroup.GraphType>();

            const float OPPOSITE_GRAPH_TYPE_WEIGHT = 0.5f;
            bool isFlying = playerBody.isFlying;
            graphTypeSelection.AddChoice(MapNodeGroup.GraphType.Air, isFlying ? 1f : OPPOSITE_GRAPH_TYPE_WEIGHT);
            graphTypeSelection.AddChoice(MapNodeGroup.GraphType.Ground, !isFlying ? 1f : OPPOSITE_GRAPH_TYPE_WEIGHT);

            return positionSelectorPlacementRule.EvaluateToPosition(RNG, playerBody.hullClassification, graphTypeSelection.Evaluate(RNG.nextNormalizedFloat));
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
