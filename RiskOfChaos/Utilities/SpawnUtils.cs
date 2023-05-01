using RoR2;
using RoR2.Navigation;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace RiskOfChaos.Utilities
{
    public static class SpawnUtils
    {
        static readonly SpawnCard _positionHelperSpawnCard;

        static SpawnUtils()
        {
            _positionHelperSpawnCard = ScriptableObject.CreateInstance<SpawnCard>();

            const string HELPER_PREFAB_PATH = "SpawnCards/HelperPrefab";
            _positionHelperSpawnCard.prefab = LegacyResourcesAPI.Load<GameObject>(HELPER_PREFAB_PATH);
            _positionHelperSpawnCard.sendOverNetwork = false;
            _positionHelperSpawnCard.hullSize = HullClassification.Human;
            _positionHelperSpawnCard.nodeGraphType = MapNodeGroup.GraphType.Ground;

            if (!_positionHelperSpawnCard.prefab)
            {
                Log.Error($"{HELPER_PREFAB_PATH} is null");
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static DirectorPlacementRule.PlacementMode GetBestValidRandomPlacementType()
        {
            SceneInfo sceneInfo = SceneInfo.instance;
            return sceneInfo && sceneInfo.approximateMapBoundMesh != null ? DirectorPlacementRule.PlacementMode.RandomNormalized : DirectorPlacementRule.PlacementMode.Random;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static DirectorPlacementRule GetBestValidRandomPlacementRule()
        {
            return new DirectorPlacementRule
            {
                placementMode = GetBestValidRandomPlacementType()
            };
        }

        public static Vector3 EvaluateToPosition(this DirectorPlacementRule placementRule, Xoroshiro128Plus rng, HullClassification? overrideHullSize = null, MapNodeGroup.GraphType? overrideNodeGraphType = null)
        {
            if (!_positionHelperSpawnCard.prefab)
            {
                Log.Warning("Null position helper prefab");
                return Vector3.zero;
            }

            DirectorCore directorCore = DirectorCore.instance;
            if (!directorCore)
            {
                Log.Warning($"No {nameof(DirectorCore)} instance found");
                return Vector3.zero;
            }

            HullClassification originalHullSize = _positionHelperSpawnCard.hullSize;
            MapNodeGroup.GraphType originalNodeGraphType = _positionHelperSpawnCard.nodeGraphType;

            if (overrideHullSize.HasValue)
                _positionHelperSpawnCard.hullSize = overrideHullSize.Value;

            if (overrideNodeGraphType.HasValue)
                _positionHelperSpawnCard.nodeGraphType = overrideNodeGraphType.Value;

            GameObject positionMarkerObject = directorCore.TrySpawnObject(new DirectorSpawnRequest(_positionHelperSpawnCard, placementRule, rng));
            
            _positionHelperSpawnCard.hullSize = originalHullSize;
            _positionHelperSpawnCard.nodeGraphType = originalNodeGraphType;

            if (!positionMarkerObject)
            {
                Log.Warning("Unable to spawn position marker object");
                return placementRule.targetPosition;
            }

            Vector3 position = positionMarkerObject.transform.position;

            GameObject.Destroy(positionMarkerObject);

            return position;
        }

        public static DirectorPlacementRule GetPlacementRule_AtRandomPlayerDirect(Xoroshiro128Plus rng)
        {
            CharacterBody[] playerBodies = PlayerUtils.GetAllPlayerBodies(true).ToArray();
            if (playerBodies.Length > 0)
            {
                CharacterBody selectedPlayer = rng.NextElementUniform(playerBodies);

                return new DirectorPlacementRule
                {
                    placementMode = DirectorPlacementRule.PlacementMode.Direct,
                    position = selectedPlayer.footPosition
                };
            }
            else
            {
                return GetBestValidRandomPlacementRule();
            }
        }

        public static DirectorPlacementRule GetPlacementRule_AtRandomPlayerNearestNode(Xoroshiro128Plus rng)
        {
            CharacterBody[] playerBodies = PlayerUtils.GetAllPlayerBodies(true).ToArray();
            if (playerBodies.Length > 0)
            {
                CharacterBody selectedPlayer = rng.NextElementUniform(playerBodies);

                return new DirectorPlacementRule
                {
                    placementMode = DirectorPlacementRule.PlacementMode.NearestNode,
                    position = selectedPlayer.footPosition
                };
            }
            else
            {
                return GetBestValidRandomPlacementRule();
            }
        }

        public static DirectorPlacementRule GetPlacementRule_AtRandomPlayerApproximate(Xoroshiro128Plus rng, float minPlayerDistance, float maxPlayerDistance)
        {
            CharacterBody[] playerBodies = PlayerUtils.GetAllPlayerBodies(true).ToArray();
            if (playerBodies.Length > 0)
            {
                CharacterBody selectedPlayer = rng.NextElementUniform(playerBodies);

                return new DirectorPlacementRule
                {
                    placementMode = DirectorPlacementRule.PlacementMode.Approximate,
                    position = selectedPlayer.footPosition,
                    minDistance = minPlayerDistance,
                    maxDistance = maxPlayerDistance
                };
            }
            else
            {
                return GetBestValidRandomPlacementRule();
            }
        }

        public static bool HasValidSpawnLocation(this SpawnCard card)
        {
            if (!card || !card.prefab)
            {
#if DEBUG
                Log.Debug($"Unable to spawn null card {card}");
#endif
                return false;
            }

            SceneInfo sceneInfo = SceneInfo.instance;
            if (!sceneInfo)
            {
#if DEBUG
                Log.Debug($"Unable to spawn {card}: No SceneInfo instance");
#endif
                return false;
            }

            NodeGraph cardNodeGraph = sceneInfo.GetNodeGraph(card.nodeGraphType);
            if (!cardNodeGraph)
            {
#if DEBUG
                Log.Debug($"Unable to spawn {card}: Target node graph {card.nodeGraphType} does not exist");
#endif
                return false;
            }

            List<NodeGraph.NodeIndex> validNodes = cardNodeGraph.GetActiveNodesForHullMaskWithFlagConditions((HullMask)(1 << (int)card.hullSize), card.requiredFlags, card.forbiddenFlags);
            if (validNodes.Count == 0)
            {
#if DEBUG
                Log.Debug($"Unable to spawn {card}: No active nodes matches flags");
#endif
                return false;
            }

            return true;
        }
    }
}
