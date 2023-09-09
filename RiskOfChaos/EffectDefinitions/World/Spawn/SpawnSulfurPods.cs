﻿using RiskOfChaos.ConfigHandling;
using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Data;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Methods;
using RiskOfChaos.Utilities;
using RiskOfChaos.Utilities.Extensions;
using RiskOfOptions.OptionConfigs;
using RoR2;
using RoR2.Navigation;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Networking;

namespace RiskOfChaos.EffectDefinitions.World.Spawn
{
    [ChaosEffect("spawn_sulfur_pods", EffectWeightReductionPercentagePerActivation = 20f)]
    public sealed class SpawnSulfurPods : BaseEffect
    {
        static GameObject _sulfurPodPrefab;

        [SystemInitializer]
        static void Init()
        {
            _sulfurPodPrefab = Addressables.LoadAssetAsync<GameObject>("RoR2/DLC1/SulfurPod/SulfurPodBody.prefab").WaitForCompletion();
        }

        static NodeGraph getSpawnNodeGraph()
        {
            SceneInfo sceneInfo = SceneInfo.instance;
            return sceneInfo ? sceneInfo.groundNodes : null;
        }

        static List<NodeGraph.NodeIndex> getValidSpawnNodes()
        {
            List<NodeGraph.NodeIndex> result = new List<NodeGraph.NodeIndex>();

            NodeGraph groundNodes = getSpawnNodeGraph();
            if (groundNodes)
            {
                groundNodes.GetActiveNodesForHullMaskWithFlagConditions(HullMask.Human | HullMask.Golem, NodeFlags.None, NodeFlags.NoCharacterSpawn, result);
            }

            return result;
        }

        [EffectCanActivate]
        static bool CanActivate()
        {
            return ExpansionUtils.DLC1Enabled && _sulfurPodPrefab && getValidSpawnNodes().Count > 0;
        }

        public override void OnStart()
        {
            NodeGraph spawnNodeGraph = getSpawnNodeGraph();
            List<NodeGraph.NodeIndex> validSpawnNodes = getValidSpawnNodes();

            int spawnCount = Mathf.Max(1, Mathf.FloorToInt(RNG.RangeFloat(0.15f, 0.3f) * validSpawnNodes.Count));
            for (int i = 0; i < spawnCount; i++)
            {
                if (spawnNodeGraph.GetNodePosition(validSpawnNodes.GetAndRemoveRandom(RNG), out Vector3 position))
                {
                    Vector3 normal = SpawnUtils.GetEnvironmentNormalAtPoint(position);

                    Quaternion rotation = Quaternion.AngleAxis(RNG.RangeFloat(0f, 360f), normal)
                                        * QuaternionUtils.RandomDeviation(15f, RNG)
                                        * QuaternionUtils.PointLocalDirectionAt(Vector3.up, normal);

                    if (NetPrefabs.SulfurPodBasePrefab)
                    {
                        GameObject sulfurPodBase = GameObject.Instantiate(NetPrefabs.SulfurPodBasePrefab, position, rotation);
                        NetworkServer.Spawn(sulfurPodBase);
                    }

                    GameObject sulfurPod = GameObject.Instantiate(_sulfurPodPrefab, position, rotation * Quaternion.Euler(270f, 0f, 0f));
                    NetworkServer.Spawn(sulfurPod);
                }
            }
        }
    }
}