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
#if DEBUG
            if (IsJumping)
            {
                Log.Debug($"{FormatUtils.GetBestBodyName(_motor.body)} has landed from jump pad");
            }
#endif

            CmdSetIsJumping(false);
        }

        [Command]
        public void CmdSetIsJumping(bool isJumping)
        {
            IsJumping = isJumping;
        }
    }
}
