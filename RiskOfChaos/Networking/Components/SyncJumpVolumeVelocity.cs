using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace RiskOfChaos.Networking.Components
{
    public class SyncJumpVolumeVelocity : NetworkBehaviour
    {
        JumpVolume _jumpVolume;

        [SyncVar(hook = nameof(syncJumpVelocity))]
        Vector3 _jumpVelocity;

        void Awake()
        {
            _jumpVolume = GetComponentInChildren<JumpVolume>();
        }

        public override void OnStartServer()
        {
            base.OnStartServer();
            _jumpVelocity = _jumpVolume.jumpVelocity;
        }

        public override void OnStartClient()
        {
            base.OnStartClient();
            syncJumpVelocity(_jumpVelocity);
        }

        void FixedUpdate()
        {
            if (NetworkServer.active)
            {
                _jumpVelocity = _jumpVolume.jumpVelocity;
            }
        }

        void syncJumpVelocity(Vector3 newJumpVelocity)
        {
            _jumpVelocity = newJumpVelocity;

            if (_jumpVolume.jumpVelocity == newJumpVelocity)
                return;

#if DEBUG
            Log.Debug($"{name} ({netId}) Jump velocity changed: {_jumpVolume.jumpVelocity} -> {newJumpVelocity}");
#endif

            _jumpVolume.jumpVelocity = newJumpVelocity;
        }
    }
}
