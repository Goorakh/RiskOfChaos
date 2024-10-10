using RiskOfChaos.Utilities.Interpolation;
using UnityEngine;
using UnityEngine.Networking;

namespace RiskOfChaos.ModifierController.Camera
{
    [ValueModificationManager(typeof(SyncCameraModification))]
    public sealed class CameraModificationManager : ValueModificationManager<CameraModificationData>
    {
        static CameraModificationManager _instance;
        public static CameraModificationManager Instance => _instance;

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
