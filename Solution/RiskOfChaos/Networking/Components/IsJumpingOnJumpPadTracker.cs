using RiskOfChaos.Patches;
using RiskOfChaos.Utilities;
using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace RiskOfChaos.Networking.Components
{
    [RequireComponent(typeof(CharacterMotor))]
    public sealed class IsJumpingOnJumpPadTracker : NetworkBehaviour
    {
        [SystemInitializer(typeof(BodyCatalog))]
        static void Init()
        {
            foreach (GameObject bodyPrefab in BodyCatalog.allBodyPrefabs)
            {
                if (bodyPrefab.GetComponent<CharacterMotor>())
                {
                    bodyPrefab.AddComponent<IsJumpingOnJumpPadTracker>();

#if DEBUG
                    Log.Debug($"Added tracker to prefab: {bodyPrefab.name}");
#endif
                }
            }
        }

        [SyncVar]
        public bool IsJumping;

        CharacterMotor _motor;

        void Awake()
        {
            _motor = GetComponent<CharacterMotor>();
        }

        void OnEnable()
        {
            JumpVolumeHooks.OnJumpVolumeJumpAuthority += onJumpVolumeJumpAuthority;
            _motor.onHitGroundAuthority += onHitGroundAuthority;
        }

        void OnDisable()
        {
            JumpVolumeHooks.OnJumpVolumeJumpAuthority -= onJumpVolumeJumpAuthority;
            _motor.onHitGroundAuthority -= onHitGroundAuthority;
        }

        void onJumpVolumeJumpAuthority(JumpVolume jumpVolume, CharacterMotor jumpingCharacterMotor)
        {
            if (!jumpingCharacterMotor || jumpingCharacterMotor != _motor)
                return;

#if DEBUG
            if (!IsJumping)
            {
                Log.Debug($"{Util.GetBestBodyName(gameObject)} started jumping on jump pad");
            }
#endif

            CmdSetIsJumping(true);
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
