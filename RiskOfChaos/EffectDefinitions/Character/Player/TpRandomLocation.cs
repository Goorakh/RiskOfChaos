using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Methods;
using RiskOfChaos.Utilities;
using RoR2;
using RoR2.Navigation;
using System.Linq;
using UnityEngine;

namespace RiskOfChaos.EffectDefinitions.Character.Player
{
    [ChaosEffect("tp_random_location")]
    public class TpRandomLocation : BaseEffect
    {
        static readonly SpawnCard _positionSelectorSpawnCard;

        static TpRandomLocation()
        {
            _positionSelectorSpawnCard = ScriptableObject.CreateInstance<SpawnCard>();

            const string HELPER_PREFAB_PATH = "SpawnCards/HelperPrefab";
            _positionSelectorSpawnCard.prefab = LegacyResourcesAPI.Load<GameObject>(HELPER_PREFAB_PATH);

            if (!_positionSelectorSpawnCard.prefab)
            {
                Log.Error($"{HELPER_PREFAB_PATH} is null");
            }
        }

        [EffectCanActivate]
        static bool CanSelect()
        {
            return DirectorCore.instance && _positionSelectorSpawnCard.prefab && PlayerUtils.GetAllPlayerBodies(true).Any();
        }

        public override void OnStart()
        {
            DirectorPlacementRule.PlacementMode placementMode = SpawnUtils.GetBestValidRandomPlacementType();

            foreach (CharacterBody playerBody in PlayerUtils.GetAllPlayerBodies(true))
            {
                teleportToRandomLocation(playerBody, placementMode);
            }
        }

        void teleportToRandomLocation(CharacterBody playerBody, DirectorPlacementRule.PlacementMode targetPositionPlacementMode)
        {
            if (tryGetTargetPosition(playerBody, targetPositionPlacementMode, out Vector3 targetPosition))
            {
                teleportToPosition(playerBody, targetPosition);
            }
        }

        bool tryGetTargetPosition(CharacterBody playerBody, DirectorPlacementRule.PlacementMode targetPositionPlacementMode, out Vector3 targetPosition)
        {
            DirectorPlacementRule positionSelectorPlacementRule = new DirectorPlacementRule
            {
                placementMode = targetPositionPlacementMode,
                position = playerBody.footPosition,
                minDistance = 50f
            };

            _positionSelectorSpawnCard.hullSize = playerBody.hullClassification;

            WeightedSelection<MapNodeGroup.GraphType> graphTypeSelection = new WeightedSelection<MapNodeGroup.GraphType>();

            const float OPPOSITE_GRAPH_TYPE_WEIGHT = 0.5f;
            bool isFlying = playerBody.isFlying;
            graphTypeSelection.AddChoice(MapNodeGroup.GraphType.Air, isFlying ? 1f : OPPOSITE_GRAPH_TYPE_WEIGHT);
            graphTypeSelection.AddChoice(MapNodeGroup.GraphType.Ground, !isFlying ? 1f : OPPOSITE_GRAPH_TYPE_WEIGHT);

            _positionSelectorSpawnCard.nodeGraphType = graphTypeSelection.Evaluate(RNG.nextNormalizedFloat);

            DirectorSpawnRequest spawnRequest = new DirectorSpawnRequest(_positionSelectorSpawnCard, positionSelectorPlacementRule, new Xoroshiro128Plus(RNG.nextUlong));

            GameObject targetPositionMarker = DirectorCore.instance.TrySpawnObject(spawnRequest);
            if (targetPositionMarker)
            {
                targetPosition = targetPositionMarker.transform.position;

                GameObject.Destroy(targetPositionMarker);

                return true;
            }
            else
            {
                Log.Warning($"Failed to get target position for {playerBody}: target position marker could not be spawned");

                targetPosition = Vector3.zero;
                return false;
            }
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
