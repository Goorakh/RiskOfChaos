using RiskOfChaos.ConfigHandling;
using RiskOfChaos.ConfigHandling.AcceptableValues;
using RiskOfChaos.Content;
using RiskOfChaos.Content.AssetCollections;
using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Data;
using RiskOfChaos.EffectHandling.EffectComponents;
using RiskOfChaos.EffectUtils.World.Spawn;
using RiskOfChaos.SaveHandling;
using RiskOfChaos.Utilities;
using RiskOfOptions.OptionConfigs;
using RoR2;
using RoR2.Navigation;
using UnityEngine;
using UnityEngine.Networking;

#if DEBUG
using System.Linq;
#endif

namespace RiskOfChaos.EffectDefinitions.World.Spawn
{
    [ChaosTimedEffect("spawn_black_hole", 30f)]
    public sealed class SpawnBlackHole : NetworkBehaviour
    {
        static GameObject _blackHolePrefab;

        [ContentInitializer]
        static void LoadContent(NetworkedPrefabAssetCollection networkPrefabs)
        {
            // BlackHoleController
            {
                _blackHolePrefab = Prefabs.CreateNetworkedPrefab("BlackHoleController", [
                    typeof(BlackHoleController),
                    typeof(DestroyOnTimer)
                ]);

                DestroyOnTimer destroyOnTimer = _blackHolePrefab.GetComponent<DestroyOnTimer>();
                destroyOnTimer.duration = 30f;

                networkPrefabs.Add(_blackHolePrefab);
            }
        }

        [EffectConfig]
        static readonly ConfigHolder<float> _maxRadiusConfig =
            ConfigFactory<float>.CreateConfig("Size", 50f)
                                .Description("The size of the black hole")
                                .AcceptableValues(new AcceptableValueMin<float>(0f))
                                .OptionConfig(new FloatFieldConfig { Min = 0f })
                                .Build();

        ChaosEffectComponent _effectComponent;

        [SerializedMember("rng")]
        Xoroshiro128Plus _rng;

        float _blackHoleRespawnTimer;
        BlackHoleController _blackHoleInstanceServer;

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
                _maxRadiusConfig.SettingChanged += onMaxRadiusConfigChanged;
        }

        void FixedUpdate()
        {
            if (NetworkServer.active)
            {
                if (!_blackHoleInstanceServer)
                {
                    _blackHoleRespawnTimer -= Time.fixedDeltaTime;
                    if (_blackHoleRespawnTimer <= 0f && Stage.instance && Stage.instance.entryTime.timeSinceClamped > 1f)
                    {
                        _blackHoleInstanceServer = spawnBlackHole();
                    }
                }

                if (_blackHoleInstanceServer)
                {
                    _blackHoleRespawnTimer = 2.5f;
                }
            }
        }

        void OnDestroy()
        {
            _maxRadiusConfig.SettingChanged -= onMaxRadiusConfigChanged;

            if (_blackHoleInstanceServer)
            {
                Destroy(_blackHoleInstanceServer.gameObject);
            }
        }

        void onMaxRadiusConfigChanged(object sender, ConfigChangedArgs<float> e)
        {
            refreshBlackHoleMaxRadius(_blackHoleInstanceServer);
        }

        [Server]
        void refreshBlackHoleMaxRadius(BlackHoleController blackHoleController)
        {
            if (!blackHoleController)
                return;

            blackHoleController.MaxRadius = _maxRadiusConfig.Value;
        }

        [Server]
        BlackHoleController spawnBlackHole()
        {
            BlackHoleController blackHoleController = Instantiate(_blackHolePrefab, pickBlackHolePosition(), Quaternion.identity).GetComponent<BlackHoleController>();

            refreshBlackHoleMaxRadius(blackHoleController);

            NetworkServer.Spawn(blackHoleController.gameObject);

            return blackHoleController;
        }

        Vector3 pickBlackHolePosition()
        {
            float maxRadius = _maxRadiusConfig.Value;

            Vector3 spawnOffset = new Vector3(0f, maxRadius + 12.5f, 0f);

            DirectorPlacementRule placementRule = SpawnUtils.GetBestValidRandomPlacementRule();

            Collider[] spawnPositionOverlapsBuffer = new Collider[32];

            Vector3 bestEncounteredSpawnPosition = Vector3.zero;
            float bestSpawnPositionSqrFitRadius = 0f;

            const int MAX_SPAWN_POSITION_CANDIDATE_COUNT = 25;

            for (int i = 0; i < MAX_SPAWN_POSITION_CANDIDATE_COUNT; i++)
            {
                Vector3 groundPosition = placementRule.EvaluateToPosition(_rng, HullClassification.Golem, MapNodeGroup.GraphType.Ground);
                Vector3 spawnPosition = groundPosition + spawnOffset;

                int overlapCount = Physics.OverlapSphereNonAlloc(spawnPosition, maxRadius, spawnPositionOverlapsBuffer, LayerIndex.world.mask.value);

                // No overlap: It fits completely, no need to check any more positions
                if (overlapCount == 0)
                {
#if DEBUG
                    Log.Debug($"Candidate {i} overlaps 0 objects, selecting");
#endif

                    bestEncounteredSpawnPosition = spawnPosition;
                    bestSpawnPositionSqrFitRadius = maxRadius * maxRadius;
                    break;
                }

                float closestOverlapPointSqrDistance = float.PositiveInfinity;
                for (int j = 0; j < overlapCount; j++)
                {
                    Collider overlapCollider = spawnPositionOverlapsBuffer[j];

                    Vector3 closestPoint;
                    switch (overlapCollider)
                    {
                        case BoxCollider:
                        case SphereCollider:
                        case CapsuleCollider:
                        case MeshCollider meshCollider when meshCollider.convex:
                            closestPoint = overlapCollider.ClosestPoint(spawnPosition);
                            break;
                        default:
                            closestPoint = overlapCollider.ClosestPointOnBounds(spawnPosition);
                            break;
                    }

                    closestOverlapPointSqrDistance = Mathf.Min(closestOverlapPointSqrDistance, (closestPoint - spawnPosition).sqrMagnitude);
                }

#if DEBUG
                float fitRadius = Mathf.Sqrt(closestOverlapPointSqrDistance);

                Log.Debug($"Candidate {i} overlaps {overlapCount} object(s) ({maxRadius - fitRadius} units, {fitRadius / maxRadius:P} fit): [{string.Join<Collider>(", ", spawnPositionOverlapsBuffer.Take(overlapCount))}]");
#endif

                if (closestOverlapPointSqrDistance > bestSpawnPositionSqrFitRadius)
                {
                    bestSpawnPositionSqrFitRadius = closestOverlapPointSqrDistance;
                    bestEncounteredSpawnPosition = spawnPosition;
                }
            }

#if DEBUG
            float bestSpawnPositionFitRadius = Mathf.Sqrt(bestSpawnPositionSqrFitRadius);
            Log.Debug($"Selected spawn position with {maxRadius - bestSpawnPositionFitRadius} units overlap ({bestSpawnPositionFitRadius / maxRadius:P} fit)");
#endif

            return bestEncounteredSpawnPosition;
        }
    }
}