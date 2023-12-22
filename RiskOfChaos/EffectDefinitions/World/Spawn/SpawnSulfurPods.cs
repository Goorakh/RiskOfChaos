using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Methods;
using RiskOfChaos.Utilities;
using RiskOfChaos.Utilities.Extensions;
using RoR2;
using RoR2.Navigation;
using System.Linq;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Networking;

namespace RiskOfChaos.EffectDefinitions.World.Spawn
{
    [ChaosEffect("spawn_sulfur_pods")]
    public sealed class SpawnSulfurPods : BaseEffect
    {
        static readonly SpawnUtils.NodeSelectionRules _placementSelectionRules = new SpawnUtils.NodeSelectionRules(SpawnUtils.NodeGraphFlags.Ground, true, HullMask.Human | HullMask.Golem, NodeFlags.None, NodeFlags.NoCharacterSpawn);

        static GameObject _sulfurPodPrefab;

        [SystemInitializer]
        static void Init()
        {
            _sulfurPodPrefab = Addressables.LoadAssetAsync<GameObject>("RoR2/DLC1/SulfurPod/SulfurPodBody.prefab").WaitForCompletion();
        }

        [EffectCanActivate]
        static bool CanActivate()
        {
            return ExpansionUtils.DLC1Enabled && _sulfurPodPrefab && SpawnUtils.GetNodes(_placementSelectionRules).Any();
        }

        public override void OnStart()
        {
            foreach (Vector3 position in SpawnUtils.GenerateDistributedSpawnPositions(_placementSelectionRules,
                                                                                      RNG.RangeFloat(0.15f, 0.3f),
                                                                                      RNG.Branch()))
            {
                Vector3 normal = SpawnUtils.GetEnvironmentNormalAtPoint(position);

                Quaternion rotation = Quaternion.AngleAxis(RNG.RangeFloat(0f, 360f), normal)
                                    * QuaternionUtils.RandomDeviation(15f, RNG.Branch())
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
