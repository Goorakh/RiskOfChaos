using RiskOfChaos.Utilities;
using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace RiskOfChaos.Networking.Components
{
    [RequireComponent(typeof(CharacterMotor))]
    public sealed class IsJumpingOnJumpPadTracker : NetworkBehaviour
    {
        const uint IS_JUMPING_DIRTY_BIT = 1 << 0;

        bool _isJumping;
        public bool NetworkedIsJumping
        {
            get
            {
                return _isJumping;
            }
            set
            {
                SetSyncVar(value, ref _isJumping, IS_JUMPING_DIRTY_BIT);
            }
        }

        CharacterMotor _motor;

        void Awake()
        {
            _motor = GetComponent<CharacterMotor>();
        }

        void FixedUpdate()
        {
            if (_motor && _isJumping && _motor.hasEffectiveAuthority && _motor.isGrounded)
            {
                NetworkedIsJumping = false;

#if DEBUG
#pragma warning disable Publicizer001 // Accessing a member that was not originally public
                Log.Debug($"{FormatUtils.GetBestBodyName(_motor.body)} has landed from jump pad");
#pragma warning restore Publicizer001 // Accessing a member that was not originally public
#endif
            }
        }

        public override bool OnSerialize(NetworkWriter writer, bool initialState)
        {
            if (initialState)
            {
                writer.Write(_isJumping);
                return true;
            }

            uint dirtyBits = syncVarDirtyBits;
            writer.WritePackedUInt32(dirtyBits);

            bool anythingWritten = false;
            if ((dirtyBits & IS_JUMPING_DIRTY_BIT) != 0)
            {
                writer.Write(_isJumping);
                anythingWritten = true;
            }

            return anythingWritten;
        }

        public override void OnDeserialize(NetworkReader reader, bool initialState)
        {
            if (initialState)
            {
                _isJumping = reader.ReadBoolean();
                return;
            }

            uint dirtyBits = reader.ReadPackedUInt32();

            if ((dirtyBits & IS_JUMPING_DIRTY_BIT) != 0)
            {
                _isJumping = reader.ReadBoolean();
            }
        }
    }
}
