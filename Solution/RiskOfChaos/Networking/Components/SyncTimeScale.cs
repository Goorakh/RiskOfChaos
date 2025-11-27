using RiskOfChaos.Components;
using RiskOfChaos.Content;
using RiskOfChaos.Utilities;
using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace RiskOfChaos.Networking.Components
{
    public sealed class SyncTimeScale : NetworkBehaviour
    {
        [ContentInitializer]
        static void LoadContent(ContentIntializerArgs args)
        {
            // TimeScaleNetworker
            {
                GameObject prefab = Prefabs.CreateNetworkedPrefab("TimeScaleNetworker", [
                    typeof(SetDontDestroyOnLoad),
                    typeof(DestroyOnRunEnd),
                    typeof(AutoCreateOnRunStart),
                    typeof(SyncTimeScale)
                ]);

                args.ContentPack.networkedObjectPrefabs.Add([prefab]);
            }
        }

        [SyncVar(hook = nameof(syncTimeScale))]
        float _timeScale = 1f;

        public override void OnStartServer()
        {
            base.OnStartServer();

            _timeScale = TimeUtils.UnpausedTimeScale;
        }

        public override void OnStartClient()
        {
            base.OnStartClient();

            setEngineTimeScale();
        }

        void syncTimeScale(float value)
        {
            _timeScale = value;
            setEngineTimeScale();
        }

        void setEngineTimeScale()
        {
            TimeUtils.UnpausedTimeScale = _timeScale;
        }

        void Update()
        {
            float timeScale = TimeUtils.UnpausedTimeScale;
            if (_timeScale != timeScale)
            {
                if (NetworkServer.active)
                {
                    _timeScale = timeScale;
                }

                setEngineTimeScale();
            }
        }

        public override float GetNetworkSendInterval()
        {
            return base.GetNetworkSendInterval() * TimeUtils.UnpausedTimeScale;
        }
    }
}
