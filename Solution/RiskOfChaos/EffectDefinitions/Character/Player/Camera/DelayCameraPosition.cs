using RiskOfChaos.Components;
using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.Utilities.Extensions;
using RoR2;
using System.Collections.Generic;
using UnityEngine.Networking;

namespace RiskOfChaos.EffectDefinitions.Character.Player.Camera
{
    [ChaosTimedEffect("delay_camera_position", 45f, AllowDuplicates = false)]
    public sealed class DelayCameraPosition : NetworkBehaviour
    {
        readonly Dictionary<CameraRigController, DelayedCameraPositionController> _createdDelayedPositionControllers = [];

        void Start()
        {
            _createdDelayedPositionControllers.EnsureCapacity(CameraRigController.readOnlyInstancesList.Count);
            CameraRigController.readOnlyInstancesList.TryDo(tryAddDelayCameraComponent);
            CameraRigController.onCameraEnableGlobal += tryAddDelayCameraComponent;
        }

        void OnDestroy()
        {
            CameraRigController.onCameraEnableGlobal -= tryAddDelayCameraComponent;

            foreach (DelayedCameraPositionController delayedPositionController in _createdDelayedPositionControllers.Values)
            {
                if (delayedPositionController)
                {
                    delayedPositionController.EaseOutAndDestroy(1f);
                }
            }

            _createdDelayedPositionControllers.Clear();
        }

        void tryAddDelayCameraComponent(CameraRigController cameraRigController)
        {
            if (_createdDelayedPositionControllers.ContainsKey(cameraRigController))
                return;

            DelayedCameraPositionController delayedPositionController = cameraRigController.gameObject.AddComponent<DelayedCameraPositionController>();
            delayedPositionController.SmoothTime = 0.25f;
            delayedPositionController.MaxSpeed = float.PositiveInfinity;
            _createdDelayedPositionControllers.Add(cameraRigController, delayedPositionController);
        }
    }
}
