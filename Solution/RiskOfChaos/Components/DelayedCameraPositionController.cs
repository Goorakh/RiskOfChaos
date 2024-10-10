using RiskOfChaos.Patches;
using RoR2;
using RoR2.CameraModes;
using UnityEngine;

namespace RiskOfChaos.Components
{
    public sealed class DelayedCameraPositionController : MonoBehaviour
    {
        CameraRigController _cameraRigController;

        Vector3 _velocity;

        public float SmoothTime = 1f;

        public float MaxSpeed = float.PositiveInfinity;

        bool _isInterpolatingOut;
        float _interpolationTimer;
        float _interpolationDuration;

        void Awake()
        {
            _cameraRigController = GetComponent<CameraRigController>();

            if (!_cameraRigController)
            {
                Log.Error($"Missing {nameof(CameraRigController)} component");
                enabled = false;
            }
        }

        void OnEnable()
        {
            _velocity = Vector3.zero;
            CameraModeHooks.OnBaseUpdatePostfix += onCameraModeBaseUpdatePostfix;
        }

        void Update()
        {
            if (_isInterpolatingOut)
            {
                _interpolationTimer += Time.deltaTime;
                if (_interpolationTimer >= _interpolationDuration)
                {
                    _isInterpolatingOut = false;
                    Destroy(this);
                }
            }
        }

        void OnDisable()
        {
            CameraModeHooks.OnBaseUpdatePostfix -= onCameraModeBaseUpdatePostfix;
        }

        void onCameraModeBaseUpdatePostfix(CameraModeBase cameraMode, ref CameraModeBase.CameraModeContext context, ref CameraModeBase.UpdateResult result)
        {
            if (context.viewerInfo.localUser is null)
                return;

            CameraRigController cameraRig = context.cameraInfo.cameraRigController;
            if (!cameraRig || !context.targetInfo.target || cameraRig != _cameraRigController)
                return;

            Vector3 delayedPosition = Vector3.SmoothDamp(context.cameraInfo.previousCameraState.position, result.cameraState.position, ref _velocity, SmoothTime, MaxSpeed, Time.deltaTime);

            if (_isInterpolatingOut)
            {
                delayedPosition = Vector3.Lerp(delayedPosition, result.cameraState.position, Mathf.Clamp01(_interpolationTimer / _interpolationDuration));
            }

            result.cameraState.position = delayedPosition;
        }

        public void EaseOutAndDestroy(float time)
        {
            if (!_isInterpolatingOut)
            {
                _isInterpolatingOut = true;
                _interpolationTimer = 0f;
            }

            _interpolationDuration = time;
        }
    }
}
