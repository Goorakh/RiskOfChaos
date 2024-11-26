using RiskOfChaos.Content;
using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Methods;
using RiskOfChaos.EffectHandling.EffectComponents;
using RiskOfChaos.Utilities;
using RiskOfChaos.Utilities.Extensions;
using RoR2;
using RoR2.Navigation;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Networking;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace RiskOfChaos.EffectDefinitions.World.Spawn
{
    [ChaosEffect("spawn_sulfur_pods")]
    public sealed class SpawnSulfurPods : NetworkBehaviour
    {
        static readonly SpawnUtils.NodeSelectionRules _placementSelectionRules = new SpawnUtils.NodeSelectionRules(SpawnUtils.NodeGraphFlags.Ground, true, HullMask.Human | HullMask.Golem, NodeFlags.None, NodeFlags.NoCharacterSpawn);

        static GameObject _sulfurPodPrefab;

        [SystemInitializer]
        static void Init()
        {
            AsyncOperationHandle<GameObject> sulfurPodLoad = Addressables.LoadAssetAsync<GameObject>("RoR2/DLC1/SulfurPod/SulfurPodBody.prefab");
            sulfurPodLoad.OnSuccess(sulfurPodPrefab => _sulfurPodPrefab = sulfurPodPrefab);
        }

        [EffectCanActivate]
        static bool CanActivate()
        {
            return ExpansionUtils.DLC1Enabled && _sulfurPodPrefab && SpawnUtils.GetNodes(_placementSelectionRules).Count > 0;
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

            foreach (Vector3 position in SpawnUtils.GenerateDistributedSpawnPositions(_placementSelectionRules,
                                                                                      0.1f,
                                                                                      _rng))
            {
                Vector3 normal = SpawnUtils.GetEnvironmentNormalAtPoint(position);
                Vector3 up = VectorUtils.Spread(normal, 10f, _rng);

                Quaternion rotation = Quaternion.AngleAxis(_rng.RangeFloat(0f, 360f), normal)
                                    * QuaternionUtils.PointLocalDirectionAt(Vector3.up, up);

                if (RoCContent.NetworkedPrefabs.NetworkedSulfurPodBase)
                {
                    GameObject sulfurPodBase = Instantiate(RoCContent.NetworkedPrefabs.NetworkedSulfurPodBase, position, rotation);
                    NetworkServer.Spawn(sulfurPodBase);
                }

                GameObject sulfurPod = Instantiate(_sulfurPodPrefab, position, rotation * Quaternion.Euler(270f, 0f, 0f));
                NetworkServer.Spawn(sulfurPod);
            }
        }
    }
}
