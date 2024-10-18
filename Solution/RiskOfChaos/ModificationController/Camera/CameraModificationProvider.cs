using RiskOfChaos.Content;
using RiskOfChaos.Utilities;
using UnityEngine;
using UnityEngine.Networking;

namespace RiskOfChaos.ModificationController.Camera
{
    [RequiredComponents(typeof(ValueModificationController))]
    public sealed class CameraModificationProvider : NetworkBehaviour
    {
        ValueModificationController _modificationController;

        [SyncVar(hook = nameof(setRecoilMultiplier))]
        public Vector2 RecoilMultiplier = Vector2.one;

        [SyncVar(hook = nameof(setFovMultiplier))]
        float _fovMultiplier = 1f;
        public float FOVMultiplier
        {
            get
            {
                float fovMultiplier = _fovMultiplier;
                
                if (_modificationController && _modificationController.IsInterpolating)
                {
                    fovMultiplier = Mathf.Lerp(1f, fovMultiplier, Ease.InOutQuad(_modificationController.CurrentInterpolationFraction));
                }

                return fovMultiplier;
            }
            set
            {
                _fovMultiplier = value;
            }
        }

        [SyncVar(hook = nameof(setRotationOffset))]
        Quaternion _rotationOffset = Quaternion.identity;
        public Quaternion RotationOffset
        {
            get
            {
                Quaternion rotationOffset = _rotationOffset;
                
                if (_modificationController && _modificationController.IsInterpolating)
                {
                    rotationOffset = Quaternion.Slerp(Quaternion.identity, rotationOffset, Ease.InOutQuad(_modificationController.CurrentInterpolationFraction));
                }

                return rotationOffset;
            }
            set
            {
                _rotationOffset = value;
            }
        }

        public ValueModificationConfigBinding<float> DistanceMultiplierConfigBinding { get; private set; }

        [SyncVar(hook = nameof(setDistanceMultiplier))]
        float _distanceMultiplier = 1f;
        public float DistanceMultiplier
        {
            get
            {
                float distanceMultiplier = _distanceMultiplier;

                if (_modificationController && _modificationController.IsInterpolating)
                {
                    distanceMultiplier = Mathf.Lerp(1f, distanceMultiplier, _modificationController.CurrentInterpolationFraction);
                }

                return distanceMultiplier;
            }
            set
            {
                _distanceMultiplier = value;
            }
        }

        void Awake()
        {
            _modificationController = GetComponent<ValueModificationController>();
            _modificationController.OnRetire += onRetire;

            DistanceMultiplierConfigBinding = new ValueModificationConfigBinding<float>(setDistanceMultiplierFromConfig);
        }

        void OnDestroy()
        {
            _modificationController.OnRetire -= onRetire;
        }

        void onRetire()
        {
            DistanceMultiplierConfigBinding?.Dispose();
        }

        void onValueChanged()
        {
            if (_modificationController)
            {
                _modificationController.InvokeOnValuesDirty();
            }
        }

        void setRecoilMultiplier(Vector2 recoilMultiplier)
        {
            RecoilMultiplier = recoilMultiplier;
            onValueChanged();
        }

        void setFovMultiplier(float fovMultiplier)
        {
            FOVMultiplier = fovMultiplier;
            onValueChanged();
        }

        void setRotationOffset(Quaternion rotationOffset)
        {
            RotationOffset = rotationOffset;
            onValueChanged();
        }

        [Server]
        void setDistanceMultiplierFromConfig(float distanceMultiplier)
        {
            DistanceMultiplier = distanceMultiplier;
        }

        void setDistanceMultiplier(float distanceMultiplier)
        {
            DistanceMultiplier = distanceMultiplier;
            onValueChanged();
        }
    }
}
