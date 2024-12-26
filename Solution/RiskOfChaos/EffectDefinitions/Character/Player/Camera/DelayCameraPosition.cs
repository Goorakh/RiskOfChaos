using HG;
using RiskOfChaos.Components;
using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.Utilities;
using RiskOfChaos.Utilities.Extensions;
using RoR2;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;

namespace RiskOfChaos.EffectDefinitions.Character.Player.Camera
{
    [ChaosTimedEffect("delay_camera_position", 45f, AllowDuplicates = false)]
    public sealed class DelayCameraPosition : MonoBehaviour
    {
        readonly Dictionary<UnityObjectWrapperKey<CameraRigController>, DelayedCameraPositionController> _delayedPositionControllers = [];

        readonly List<OnDestroyEvent> _destroyEvent = [];

        bool _trackedObjectDestroyed;

        void Start()
        {
            ReadOnlyCollection<CameraRigController> cameraRigInstances = CameraRigController.readOnlyInstancesList;

            _delayedPositionControllers.EnsureCapacity(cameraRigInstances.Count);
            _destroyEvent.EnsureCapacity(cameraRigInstances.Count);

            cameraRigInstances.TryDo(tryAddDelayCameraComponent);
            CameraRigController.onCameraEnableGlobal += tryAddDelayCameraComponent;
        }

        void FixedUpdate()
        {
            if (_trackedObjectDestroyed)
            {
                _trackedObjectDestroyed = false;

                UnityObjectUtils.RemoveAllDestroyed(_destroyEvent);

                int removedPositionControllers = UnityObjectUtils.RemoveAllDestroyed(_delayedPositionControllers);
                Log.Debug($"Cleared {removedPositionControllers} destroyed position controller(s)");
            }
        }

        void OnDestroy()
        {
            CameraRigController.onCameraEnableGlobal -= tryAddDelayCameraComponent;

            foreach (OnDestroyEvent destroyEvent in _destroyEvent)
            {
                if (destroyEvent)
                {
                    destroyEvent.OnDestroyed -= onCameraRigDestroyed;
                }
            }

            _destroyEvent.Clear();

            foreach (DelayedCameraPositionController delayedPositionController in _delayedPositionControllers.Values)
            {
                if (delayedPositionController)
                {
                    delayedPositionController.EaseOutAndDestroy(1f);
                }
            }

            _delayedPositionControllers.Clear();
        }

        void tryAddDelayCameraComponent(CameraRigController cameraRigController)
        {
            if (_delayedPositionControllers.ContainsKey(cameraRigController))
                return;

            DelayedCameraPositionController delayedPositionController = cameraRigController.gameObject.AddComponent<DelayedCameraPositionController>();
            delayedPositionController.SmoothTime = 0.25f;
            delayedPositionController.MaxSpeed = float.PositiveInfinity;

            _delayedPositionControllers.Add(cameraRigController, delayedPositionController);

            OnDestroyEvent destroyEvent = OnDestroyEvent.Add(cameraRigController.gameObject, onCameraRigDestroyed);

            _destroyEvent.Add(destroyEvent);
        }

        void onCameraRigDestroyed(GameObject obj)
        {
            _trackedObjectDestroyed = true;
        }
    }
}
