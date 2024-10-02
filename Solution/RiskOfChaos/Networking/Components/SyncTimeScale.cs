using RiskOfChaos.Utilities;
using UnityEngine.Networking;

namespace RiskOfChaos.Networking.Components
{
    public class SyncTimeScale : NetworkBehaviour
    {
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

            syncTimeScale(_timeScale);
        }

        void syncTimeScale(float value)
        {
            _timeScale = value;

            TimeUtils.UnpausedTimeScale = value;
        }

        void FixedUpdate()
        {
            float timeScale = TimeUtils.UnpausedTimeScale;
            if (_timeScale != timeScale)
            {
                if (NetworkServer.active)
                {
                    _timeScale = timeScale;
                }
                else
                {
                    TimeUtils.UnpausedTimeScale = _timeScale;
                }
            }
        }

        public override float GetNetworkSendInterval()
        {
            return base.GetNetworkSendInterval() * TimeUtils.UnpausedTimeScale;
        }
    }
}
