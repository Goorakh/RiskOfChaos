using MonoMod.Cil;
using RiskOfChaos.Patches;
using RiskOfChaos.Utilities.Interpolation;
using RoR2.CameraModes;
using UnityEngine;
using UnityEngine.Networking;

namespace RiskOfChaos.ModifierController.Camera
{
    [ValueModificationManager]
    public sealed class CameraModificationManager : NetworkedValueModificationManager<CameraModificationData>
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
                            return Vector2.Scale(recoil, _instance.NetworkRecoilMultiplier);
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

                result.cameraState.fov = Mathf.Clamp(result.cameraState.fov * _instance.CurrentFOVMultiplier, MIN_FOV, MAX_FOV);

                result.cameraState.rotation *= _instance.CurrentCameraRotationOffset;
            }
        }

        static void CameraModeBase_CollectLookInput(On.RoR2.CameraModes.CameraModeBase.orig_CollectLookInput orig, CameraModeBase self, ref CameraModeBase.CameraModeContext context, out CameraModeBase.CollectLookInputResult result)
        {
            orig(self, ref context, out result);

            if (_instance && _instance.AnyModificationActive && context.targetInfo.target)
            {
                Vector2 rotatedLookInput = _instance.CurrentCameraRotationOffset * result.lookInput;
                if (rotatedLookInput.sqrMagnitude > 0f)
                {
                    float lookInputMagnitude = result.lookInput.magnitude;
                    result.lookInput = rotatedLookInput.normalized * lookInputMagnitude;
                }
            }
        }

        static void PlayerInputHook_ModifyPlayerMoveInput(RoR2.PlayerCharacterMasterController playerMasterController, ref Vector2 moveInput)
        {
            if (_instance && _instance.AnyModificationActive)
            {
                const float ROTATION_TO_CONSIDER_FLIPPED = 120f;

                float zOffset = _instance.CurrentCameraRotationOffset.eulerAngles.z;
                if (zOffset >= ROTATION_TO_CONSIDER_FLIPPED && zOffset <= 360f - ROTATION_TO_CONSIDER_FLIPPED)
                {
                    moveInput.x *= -1;
                }
            }
        }

        Vector2 _recoilMultiplier = Vector2.one;
        const uint RECOIL_MULTIPLIER_DIRTY_BIT = 1 << 1;
        public Vector2 NetworkRecoilMultiplier
        {
            get
            {
                return _recoilMultiplier;
            }
            set
            {
                SetSyncVar(value, ref _recoilMultiplier, RECOIL_MULTIPLIER_DIRTY_BIT);
            }
        }

        float _FOVMultiplier = 1f;
        const uint FOV_MULTIPLIER_DIRTY_BIT = 1 << 2;
        float networkFOVMultiplier
        {
            get
            {
                return _FOVMultiplier;
            }
            set
            {
                SetSyncVar(value, ref _FOVMultiplier, FOV_MULTIPLIER_DIRTY_BIT);
            }
        }

        float _clientFOVMultiplierVelocity;
        float _clientFOVMultiplier = 1f;
        public float CurrentFOVMultiplier => NetworkServer.active ? networkFOVMultiplier : _clientFOVMultiplier;

        Quaternion _cameraRotationOffset = Quaternion.identity;
        const uint CAMERA_ROTATION_OFFSET_DIRTY_BIT = 1 << 3;
        Quaternion networkCameraRotationOffset
        {
            get
            {
                return _cameraRotationOffset;
            }
            set
            {
                SetSyncVar(value, ref _cameraRotationOffset, CAMERA_ROTATION_OFFSET_DIRTY_BIT);
            }
        }

        Quaternion _clientCameraRotationOffset = Quaternion.identity;
        public Quaternion CurrentCameraRotationOffset => NetworkServer.active ? networkCameraRotationOffset : _clientCameraRotationOffset;

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

        public override void OnStartClient()
        {
            base.OnStartClient();

            _clientFOVMultiplier = networkFOVMultiplier;
            _clientCameraRotationOffset = networkCameraRotationOffset;
        }

        void Update()
        {
            if (!NetworkServer.active)
            {
                _clientFOVMultiplier = Mathf.SmoothDamp(_clientFOVMultiplier, networkFOVMultiplier, ref _clientFOVMultiplierVelocity, 0.75f, float.PositiveInfinity, Time.unscaledDeltaTime);
                _clientCameraRotationOffset = Quaternion.RotateTowards(_clientCameraRotationOffset, networkCameraRotationOffset, 180f * Time.unscaledDeltaTime);
            }
        }

        public override CameraModificationData InterpolateValue(in CameraModificationData a, in CameraModificationData b, float t)
        {
            return CameraModificationData.Interpolate(a, b, t, ValueInterpolationFunctionType.Linear);
        }

        public override void UpdateValueModifications()
        {
            CameraModificationData modificationData = GetModifiedValue(new CameraModificationData());

            NetworkRecoilMultiplier = modificationData.RecoilMultiplier;
            networkFOVMultiplier = modificationData.FOVMultiplier;
            networkCameraRotationOffset = modificationData.RotationOffset;
        }

        protected override bool serialize(NetworkWriter writer, bool initialState, uint dirtyBits)
        {
            bool baseValue = base.serialize(writer, initialState, dirtyBits);

            if (initialState)
            {
                writer.Write(_recoilMultiplier);
                writer.Write(_FOVMultiplier);
                writer.Write(_cameraRotationOffset);

                return baseValue;
            }

            bool anythingWritten = false;

            if ((dirtyBits & RECOIL_MULTIPLIER_DIRTY_BIT) != 0)
            {
                writer.Write(_recoilMultiplier);
                anythingWritten = true;
            }

            if ((dirtyBits & FOV_MULTIPLIER_DIRTY_BIT) != 0)
            {
                writer.Write(_FOVMultiplier);
                anythingWritten = true;
            }

            if ((dirtyBits & CAMERA_ROTATION_OFFSET_DIRTY_BIT) != 0)
            {
                writer.Write(_cameraRotationOffset);
                anythingWritten = true;
            }

            return baseValue || anythingWritten;
        }

        protected override void deserialize(NetworkReader reader, bool initialState, uint dirtyBits)
        {
            base.deserialize(reader, initialState, dirtyBits);

            if (initialState)
            {
                _recoilMultiplier = reader.ReadVector2();
                _FOVMultiplier = reader.ReadSingle();
                _cameraRotationOffset = reader.ReadQuaternion();
                return;
            }

            if ((dirtyBits & RECOIL_MULTIPLIER_DIRTY_BIT) != 0)
            {
                _recoilMultiplier = reader.ReadVector2();
            }

            if ((dirtyBits & FOV_MULTIPLIER_DIRTY_BIT) != 0)
            {
                _FOVMultiplier = reader.ReadSingle();
            }

            if ((dirtyBits & CAMERA_ROTATION_OFFSET_DIRTY_BIT) != 0)
            {
                _cameraRotationOffset = reader.ReadQuaternion();
            }
        }
    }
}
