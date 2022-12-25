using RoR2;
using RoR2.Navigation;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace RiskOfChaos.Utilities
{
    public static class SpawnUtils
    {
        public static Vector3? GetRandomSpawnPosition(Xoroshiro128Plus rng, bool allowPlayerSpawn)
        {
            if (allowPlayerSpawn)
            {
                IEnumerable<CharacterBody> allPlayerBodies = PlayerUtils.GetAllPlayerBodies(true);
                if (allPlayerBodies.Any())
                {
                    return rng.NextElementUniform(allPlayerBodies.ToArray()).footPosition;
                }
            }

            SceneInfo sceneInfo = SceneInfo.instance;
            if (sceneInfo)
            {
                NodeGraph groundNodes = sceneInfo.groundNodes;
                if (groundNodes)
                {
                    List<NodeGraph.NodeIndex> allNodes = groundNodes.GetActiveNodesForHullMask(HullMask.Human);
                    NodeGraph.NodeIndex randomNodeIndex = rng.NextElementUniform(allNodes);

                    if (groundNodes.GetNodePosition(randomNodeIndex, out Vector3 position))
                    {
                        return position;
                    }
                }
            }

            return null;
        }
    }
}
