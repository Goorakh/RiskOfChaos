using RoR2;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace RiskOfChaos.Networking.Components
{
    public class SyncJumpVolumeVelocity : NetworkBehaviour
    {
        JumpVolume _jumpVolume;

        const int JUMP_VELOCITY_DIRTY_BIT = 1 << 0;
        Vector3 _lastJumpVelocity;

        void Awake()
        {
            _jumpVolume = GetComponentInChildren<JumpVolume>();
        }

        public override void OnStartAuthority()
        {
            base.OnStartAuthority();
            _lastJumpVelocity = _jumpVolume.jumpVelocity;
        }

        void FixedUpdate()
        {
            if (_jumpVolume && _jumpVolume.jumpVelocity != _lastJumpVelocity)
            {
                if (hasAuthority)
                {
#if DEBUG
                    Log.Debug("Jump velocity changed as authority, setting dirty bit");
#endif

                    _lastJumpVelocity = _jumpVolume.jumpVelocity;
                    SetDirtyBit(JUMP_VELOCITY_DIRTY_BIT);
                }
                else
                {
#if DEBUG
                    Log.Debug("Jump velocity changed as non-authority, setting jumpVelocity");
#endif

                    _jumpVolume.jumpVelocity = _lastJumpVelocity;
                }
            }
        }

        public override bool OnSerialize(NetworkWriter writer, bool initialState)
        {
            if (initialState)
            {
                writer.Write(_lastJumpVelocity);
                return true;
            }

            uint dirtyBits = syncVarDirtyBits;

            bool anythingWritten = false;

            if ((dirtyBits & JUMP_VELOCITY_DIRTY_BIT) != 0)
            {
                writer.Write(_lastJumpVelocity);
                anythingWritten = true;
            }

            return anythingWritten;
        }

        public override void OnDeserialize(NetworkReader reader, bool initialState)
        {
            if (initialState)
            {
                _lastJumpVelocity = reader.ReadVector3();
                return;
            }

            uint dirtyBits = syncVarDirtyBits;
            if ((dirtyBits & JUMP_VELOCITY_DIRTY_BIT) != 0)
            {
                _lastJumpVelocity = reader.ReadVector3();
            }
        }
    }
}
