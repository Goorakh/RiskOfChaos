using RiskOfChaos.Utilities;
using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace RiskOfChaos.Networking.Components
{
    [RequireComponent(typeof(CharacterMotor))]
    public sealed class IsJumpingOnJumpPadTracker : NetworkBehaviour
    {
        [SyncVar]
        public bool IsJumping;

        CharacterMotor _motor;

        void Awake()
        {
            _motor = GetComponent<CharacterMotor>();
        }

        void OnEnable()
        {
            _motor.onHitGroundAuthority += onHitGroundAuthority;
        }

        void OnDisable()
        {
            _motor.onHitGroundAuthority -= onHitGroundAuthority;
        }

        void onHitGroundAuthority(ref CharacterMotor.HitGroundInfo hitGroundInfo)
        {
            if (!IsJumping)
                return;

            CmdSetIsJumping(false);

#if DEBUG
#pragma warning disable Publicizer001 // Accessing a member that was not originally public
            Log.Debug($"{FormatUtils.GetBestBodyName(_motor.body)} has landed from jump pad");
#pragma warning restore Publicizer001 // Accessing a member that was not originally public
#endif
        }

        [Command]
        public void CmdSetIsJumping(bool isJumping)
        {
            IsJumping = isJumping;
        }
    }
}
