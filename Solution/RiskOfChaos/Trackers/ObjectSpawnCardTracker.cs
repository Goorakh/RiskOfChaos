using HG;
using RoR2;
using UnityEngine;

namespace RiskOfChaos.Trackers
{
    public sealed class ObjectSpawnCardTracker : MonoBehaviour
    {
        [SystemInitializer]
        static void Init()
        {
            SpawnCard.onSpawnedServerGlobal += onSpawnedServer;
        }

        static void onSpawnedServer(SpawnCard.SpawnResult result)
        {
            if (result.success && result.spawnedInstance)
            {
                ObjectSpawnCardTracker tracker = result.spawnedInstance.EnsureComponent<ObjectSpawnCardTracker>();
                tracker.SpawnCard = result.spawnRequest.spawnCard;
            }
        }

        public SpawnCard SpawnCard { get; private set; }

        void OnEnable()
        {
            InstanceTracker.Add(this);
        }

        void OnDisable()
        {
            InstanceTracker.Remove(this);
        }
    }
}
