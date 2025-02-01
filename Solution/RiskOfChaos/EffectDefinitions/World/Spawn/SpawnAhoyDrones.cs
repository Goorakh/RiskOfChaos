using HG;
using RiskOfChaos.ConfigHandling;
using RiskOfChaos.ConfigHandling.AcceptableValues;
using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Data;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Methods;
using RiskOfChaos.EffectHandling.EffectComponents;
using RiskOfChaos.Utilities;
using RiskOfOptions.OptionConfigs;
using RoR2;
using RoR2.Navigation;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

namespace RiskOfChaos.EffectDefinitions.World.Spawn
{
    [ChaosEffect("spawn_ahoy_drones")]
    public sealed class SpawnAhoyDrones : NetworkBehaviour
    {
        static CharacterSpawnCard _equipmentDroneSpawnCard;

        [SystemInitializer(typeof(MasterCatalog))]
        static void Init()
        {
            GameObject equipmentDroneMasterPrefab = MasterCatalog.FindMasterPrefab("EquipmentDroneMaster");
            if (equipmentDroneMasterPrefab)
            {
                _equipmentDroneSpawnCard = ScriptableObject.CreateInstance<CharacterSpawnCard>();
                _equipmentDroneSpawnCard.name = "cscEquipmentDrone";
                _equipmentDroneSpawnCard.prefab = equipmentDroneMasterPrefab;
                _equipmentDroneSpawnCard.sendOverNetwork = true;
                _equipmentDroneSpawnCard.nodeGraphType = MapNodeGroup.GraphType.Air;
                _equipmentDroneSpawnCard.forbiddenFlags = NodeFlags.NoCharacterSpawn;

                _equipmentDroneSpawnCard.equipmentToGrant = [
                    DLC1Content.Equipment.BossHunterConsumed
                ];

                if (_equipmentDroneSpawnCard.prefab.TryGetComponent(out CharacterMaster masterPrefab) &&
                    masterPrefab.bodyPrefab &&
                    masterPrefab.bodyPrefab.TryGetComponent(out CharacterBody bodyPrefab))
                {
                    _equipmentDroneSpawnCard.hullSize = bodyPrefab.hullClassification;

                    Log.Debug($"Set SpawnCard hull size to: {_equipmentDroneSpawnCard.hullSize}");
                }
                else
                {
                    Log.Error("Failed to get equipment drone hull size");
                    _equipmentDroneSpawnCard.hullSize = HullClassification.Human;
                }
            }
            else
            {
                Log.Error("Failed to find equipment drone master prefab");
            }
        }

        [EffectConfig]
        static readonly ConfigHolder<int> _droneSpawnCount =
            ConfigFactory<int>.CreateConfig("Spawn Count", 3)
                              .Description("How many drones should be spawned")
                              .AcceptableValues(new AcceptableValueMin<int>(1))
                              .OptionConfig(new IntFieldConfig { Min = 1 })
                              .Build();

        [EffectCanActivate]
        static bool CanActivate()
        {
            return ExpansionUtils.DLC1Enabled && DirectorCore.instance && _equipmentDroneSpawnCard && _equipmentDroneSpawnCard.HasValidSpawnLocation();
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

            List<SpawnTargetInfo> spawnTargets = new List<SpawnTargetInfo>(PlayerCharacterMasterController.instances.Count);
            foreach (PlayerCharacterMasterController playerMaster in PlayerCharacterMasterController.instances)
            {
                if (!playerMaster.isConnected)
                    continue;

                CharacterMaster master = playerMaster.master;
                if (!master || master.IsDeadAndOutOfLivesServer() || !master.GetBody())
                    continue;

                spawnTargets.Add(new SpawnTargetInfo(master, new Xoroshiro128Plus(_rng.nextUlong), 0));
            }

            if (spawnTargets.Count <= 0)
            {
                Log.Error("No valid spawn targets found");
                return;
            }

            int totalSpawnCount = _droneSpawnCount.Value;
            int spawnsRemaining = totalSpawnCount;

            while (spawnsRemaining > 0)
            {
                Util.ShuffleList(spawnTargets, _rng);

                foreach (SpawnTargetInfo target in spawnTargets)
                {
                    target.SpawnCount++;

                    spawnsRemaining--;
                    if (spawnsRemaining <= 0)
                        break;
                }
            }

            NodeGraph spawnNodeGraph = null;
            if (SceneInfo.instance)
            {
                spawnNodeGraph = SceneInfo.instance.GetNodeGraph(_equipmentDroneSpawnCard.nodeGraphType);
                if (!spawnNodeGraph)
                {
                    spawnNodeGraph = SceneInfo.instance.airNodes;
                    
                    if (!spawnNodeGraph)
                    {
                        spawnNodeGraph = SceneInfo.instance.groundNodes;
                    }
                }
            }

            List<NodeGraph.NodeIndex> usedSpawnNodes = new List<NodeGraph.NodeIndex>(totalSpawnCount);

            foreach (SpawnTargetInfo target in spawnTargets)
            {
                if (target.SpawnCount <= 0)
                    continue;

                CharacterMaster targetMaster = target.TargetMaster;
                if (!targetMaster)
                    continue;

                CharacterBody targetBody = targetMaster.GetBody();
                if (!targetBody)
                    continue;

                Vector3 spawnOrigin = targetBody.corePosition;

                Vector3[] spawnPositions = new Vector3[target.SpawnCount];

                HullDef hullDef = HullDef.Find(_equipmentDroneSpawnCard.hullSize);

                float bestFitRadius = Mathf.Max(hullDef.height / 2f, hullDef.radius);

                if (_equipmentDroneSpawnCard.prefab && _equipmentDroneSpawnCard.prefab.TryGetComponent(out CharacterMaster masterPrefab) && masterPrefab.bodyPrefab)
                {
                    if (masterPrefab.bodyPrefab.TryGetComponent(out SphereCollider sphereCollider))
                    {
                        bestFitRadius = sphereCollider.radius;
                    }
                    else if (masterPrefab.bodyPrefab.TryGetComponent(out CapsuleCollider capsuleCollider))
                    {
                        bestFitRadius = Mathf.Max(capsuleCollider.height / 2f, capsuleCollider.radius);
                    }
                }

                for (int i = 0; i < spawnPositions.Length; i++)
                {
                    Vector3 spawnOffset = target.Rng.PointInUnitSphere() * 3.5f;
                    if (Physics.SphereCast(new Ray(spawnOrigin, spawnOffset), bestFitRadius, out RaycastHit hit, spawnOffset.magnitude, LayerIndex.world.mask))
                    {
                        spawnOffset = hit.point - spawnOrigin;
                    }

                    spawnPositions[i] = spawnOrigin + spawnOffset;
                }

                NodeGraph.NodeIndex startNode = spawnNodeGraph.FindClosestNode(spawnOrigin, _equipmentDroneSpawnCard.hullSize);
                if (spawnNodeGraph && startNode != NodeGraph.NodeIndex.invalid)
                {
                    NodeGraphSpider nodeGraphSpider = new NodeGraphSpider(spawnNodeGraph, (HullMask)(1 << (int)_equipmentDroneSpawnCard.hullSize));
                    nodeGraphSpider.AddNodeForNextStep(startNode);

                    int stepsRemaining = 16;
                    while (nodeGraphSpider.PerformStep())
                    {
                        List<NodeGraphSpider.StepInfo> collectedSteps = nodeGraphSpider.collectedSteps;
                        for (int i = collectedSteps.Count - 1; i >= 0; i--)
                        {
                            if (usedSpawnNodes.Contains(collectedSteps[i].node) ||
                                !spawnNodeGraph.GetNodeFlags(collectedSteps[i].node, out NodeFlags nodeFlags) ||
                                (nodeFlags & _equipmentDroneSpawnCard.requiredFlags) != _equipmentDroneSpawnCard.requiredFlags ||
                                (nodeFlags & _equipmentDroneSpawnCard.forbiddenFlags) != 0)
                            {
                                collectedSteps.RemoveAt(i);
                            }
                        }

                        if (nodeGraphSpider.collectedSteps.Count >= spawnPositions.Length)
                            break;

                        stepsRemaining--;
                        if (stepsRemaining <= 0)
                            break;
                    }

                    List<NodeGraphSpider.StepInfo> collectedSpawnPositionSteps = nodeGraphSpider.collectedSteps;
                    Util.ShuffleList(collectedSpawnPositionSteps, target.Rng);

                    int numAvailableSpawnPositions = Mathf.Min(spawnPositions.Length, collectedSpawnPositionSteps.Count);
                    for (int i = 0; i < numAvailableSpawnPositions; i++)
                    {
                        NodeGraphSpider.StepInfo stepInfo = collectedSpawnPositionSteps[i];

                        if (spawnNodeGraph.GetNodePosition(stepInfo.node, out Vector3 spawnPosition))
                        {
                            spawnPositions[i] = spawnPosition;
                            usedSpawnNodes.Add(stepInfo.node);
                        }
                    }
                }

                for (int i = 0; i < spawnPositions.Length; i++)
                {
                    Vector3 spawnPosition = spawnPositions[i];

                    MasterSummon droneSummon = new MasterSummon
                    {
                        masterPrefab = _equipmentDroneSpawnCard.prefab,
                        summonerBodyObject = targetBody.gameObject,
                        position = spawnPosition,
                        rotation = Quaternion.identity,
                        ignoreTeamMemberLimit = true,
                        useAmbientLevel = true,
                        inventorySetupCallback = _equipmentDroneSpawnCard,
                    };

                    droneSummon.Perform();
                }
            }
        }

        class SpawnTargetInfo
        {
            public readonly CharacterMaster TargetMaster;
            public readonly Xoroshiro128Plus Rng;
            public int SpawnCount;

            public SpawnTargetInfo(CharacterMaster targetMaster, Xoroshiro128Plus rng, int spawnCount)
            {
                TargetMaster = targetMaster;
                Rng = rng;
                SpawnCount = spawnCount;
            }
        }
    }
}
