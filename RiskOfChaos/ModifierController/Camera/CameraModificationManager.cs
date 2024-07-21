using MonoMod.Cil;
using RiskOfChaos.Patches;
using RiskOfChaos.Utilities.Interpolation;
using RoR2;
using RoR2.CameraModes;
using UnityEngine;
using UnityEngine.Networking;

namespace RiskOfChaos.ModifierController.Camera
{
    [ValueModificationManager(typeof(SyncCameraModification))]
    public sealed class CameraModificationManager : ValueModificationManager<CameraModificationData>
    {
        static CameraModificationManager _instance;
        public static CameraModificationManager Instance => _instance;

        static bool _appliedPatches;
        static void tryApplyPatches()
        {
            if (_appliedPatches)
                return;

            IL.RoR2.CameraTargetParams.AddRecoil += il =>
            {
                ILCursor c = new ILCursor(il);

                if (c.TryGotoNext(MoveType.After, x => x.MatchNewobj<Vector2>()))
                {
                    c.EmitDelegate((Vector2 recoil) =>
                    {
                        if (_instance && _instance.AnyModificationActive)
                        {
                            return Vector2.Scale(recoil, _instance.RecoilMultiplier);
                        }
                        else
                        {
                            return recoil;
                        }
                    });
                }
                else
                {
                    Log.Error("Failed to find AddRecoil patch location");
                }
            };

            On.RoR2.CameraModes.CameraModeBase.Update += CameraModeBase_Update;

            On.RoR2.CameraModes.CameraModeBase.CollectLookInput += CameraModeBase_CollectLookInput;

            On.RoR2.CameraTargetParams.CalcParams += CameraTargetParams_CalcParams;

            PlayerInputHook.ModifyPlayerMoveInput += PlayerInputHook_ModifyPlayerMoveInput;

            _appliedPatches = true;
        }

        static void CameraModeBase_Update(On.RoR2.CameraModes.CameraModeBase.orig_Update orig, CameraModeBase self, ref CameraModeBase.CameraModeContext context, out CameraModeBase.UpdateResult result)
        {
            orig(self, ref context, out result);

            if (_instance && _instance.AnyModificationActive && context.targetInfo.target)
            {
                const float MIN_FOV = 10f;
                const float MAX_FOV = 170f;

                result.cameraState.fov = Mathf.Clamp(result.cameraState.fov * _instance.FovMultiplier, MIN_FOV, MAX_FOV);

                result.cameraState.rotation *= _instance.CameraRotationOffset;
            }
        }

        static void CameraModeBase_CollectLookInput(On.RoR2.CameraModes.CameraModeBase.orig_CollectLookInput orig, CameraModeBase self, ref CameraModeBase.CameraModeContext context, out CameraModeBase.CollectLookInputResult result)
        {
            orig(self, ref context, out result);

            if (_instance && _instance.AnyModificationActive && context.targetInfo.target)
            {
                Vector2 rotatedLookInput = _instance.CameraRotationOffset * result.lookInput;
                if (rotatedLookInput.sqrMagnitude > 0f)
                {
                    float lookInputMagnitude = result.lookInput.magnitude;
                    result.lookInput = rotatedLookInput.normalized * lookInputMagnitude;
                }
            }
        }

        static void CameraTargetParams_CalcParams(On.RoR2.CameraTargetParams.orig_CalcParams orig, CameraTargetParams self, out CharacterCameraParamsData dest)
        {
            orig(self, out dest);

            if (_instance && _instance.AnyModificationActive)
            {
                float distanceMultiplier = _instance.CameraDistanceMultiplier;

                dest.idealLocalCameraPos.value *= distanceMultiplier;
            }
        }

        static void PlayerInputHook_ModifyPlayerMoveInput(PlayerCharacterMasterController playerMasterController, ref Vector2 moveInput)
        {
            if (_instance && _instance.AnyModificationActive)
            {
                const float ROTATION_TO_CONSIDER_FLIPPED = 120f;

                float zOffset = _instance.CameraRotationOffset.eulerAngles.z;
                if (zOffset >= ROTATION_TO_CONSIDER_FLIPPED && zOffset <= 360f - ROTATION_TO_CONSIDER_FLIPPED)
                {
                    moveInput.x *= -1;
                }
            }
        }

        SyncCameraModification _clientSync;

        public Vector2 RecoilMultiplier
        {
            get
            {
                return _clientSync.RecoilMultiplier;
            }
            private set
            {
                _clientSync.RecoilMultiplier = value;
            }
        }

        public float FovMultiplier
        {
            get
            {
                return _clientSync.FovMultiplier;
            }
            private set
            {
                _clientSync.FovMultiplier = value;
            }
        }

        public Quaternion CameraRotationOffset
        {
            get
            {
                return _clientSync.RotationOffset;
            }
            private set
            {
                _clientSync.RotationOffset = value;
            }
        }

        public float CameraDistanceMultiplier
        {
            get
            {
                return _clientSync.DistanceMultiplier;
            }
            private set
            {
                _clientSync.DistanceMultiplier = value;
            }
        }

        protected override void Awake()
        {
            base.Awake();
            _clientSync = GetComponent<SyncCameraModification>();
        }

        protected override void OnEnable()
        {
            base.OnEnable();

            tryApplyPatches();
            SingletonHelper.Assign(ref _instance, this);
        }

        protected override void OnDisable()
        {
            base.OnDisable();

            SingletonHelper.Unassign(ref _instance, this);
        }

        public override CameraModificationData InterpolateValue(in CameraModificationData a, in CameraModificationData b, float t)
        {
            return CameraModificationData.Interpolate(a, b, t, ValueInterpolationFunctionType.Linear);
        }

        public override void UpdateValueModifications()
        {
            if (!NetworkServer.active)
            {
                Log.Warning("Called on client");
                return;
            }

            CameraModificationData modificationData = GetModifiedValue(new CameraModificationData());

            RecoilMultiplier = modificationData.RecoilMultiplier;
            FovMultiplier = modificationData.FOVMultiplier;
            CameraRotationOffset = modificationData.RotationOffset;
            CameraDistanceMultiplier = modificationData.CameraDistanceMultiplier;
        }
    }
}
