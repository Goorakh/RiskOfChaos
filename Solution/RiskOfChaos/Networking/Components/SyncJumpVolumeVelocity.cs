using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace RiskOfChaos.Networking.Components
{
    public class SyncJumpVolumeVelocity : NetworkBehaviour
    {
        public JumpVolume JumpVolume;

        [SyncVar(hook = nameof(syncJumpVelocity))]
        Vector3 _jumpVelocity;

        void Awake()
        {
            if (!JumpVolume)
            {
                JumpVolume = GetComponentInChildren<JumpVolume>();
            }
        }

        public override void OnStartServer()
        {
            base.OnStartServer();

            if (JumpVolume)
            {
                _jumpVelocity = JumpVolume.jumpVelocity;
            }
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
                if (JumpVolume)
                {
                    _jumpVelocity = JumpVolume.jumpVelocity;
                }
            }
        }

        void syncJumpVelocity(Vector3 newJumpVelocity)
        {
            _jumpVelocity = newJumpVelocity;

            updateJumpVelocity();
        }

        void updateJumpVelocity()
        {
            if (!JumpVolume)
                return;

            if (JumpVolume.jumpVelocity == _jumpVelocity)
                return;

            Log.Debug($"{Util.GetGameObjectHierarchyName(gameObject)} ({netId}) Jump velocity changed: {JumpVolume.jumpVelocity} -> {_jumpVelocity}");

            JumpVolume.jumpVelocity = _jumpVelocity;
        }
    }
}
