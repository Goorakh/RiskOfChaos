using HG;
using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RoR2;
using RoR2.CameraModes;
using System.Collections.Generic;
using UnityEngine;

namespace RiskOfChaos.EffectDefinitions.Character.Player.Camera
{
    [ChaosTimedEffect("delay_camera_position", 45f, AllowDuplicates = false, IsNetworked = true)]
    public sealed class DelayCameraPosition : TimedEffect
    {
        readonly Dictionary<UnityObjectWrapperKey<CameraRigController>, Vector3> _cameraMoveVelocities = [];

        public override void OnStart()
        {
            On.RoR2.CameraModes.CameraModeBase.Update += CameraModeBase_Update;
        }

        public override void OnEnd()
        {
            On.RoR2.CameraModes.CameraModeBase.Update -= CameraModeBase_Update;
            _cameraMoveVelocities.Clear();
        }

        void CameraModeBase_Update(On.RoR2.CameraModes.CameraModeBase.orig_Update orig, CameraModeBase self, ref CameraModeBase.CameraModeContext context, out CameraModeBase.UpdateResult result)
        {
            orig(self, ref context, out result);

            if (context.viewerInfo.localUser is null)
                return;

            CameraRigController cameraRig = context.cameraInfo.cameraRigController;
            if (!cameraRig || !context.targetInfo.target)
                return;

            if (!_cameraMoveVelocities.TryGetValue(cameraRig, out Vector3 velocity))
            {
                velocity = Vector3.zero;
            }

            result.cameraState.position = Vector3.SmoothDamp(context.cameraInfo.previousCameraState.position, result.cameraState.position, ref velocity, 0.25f, float.PositiveInfinity, Time.deltaTime);

            _cameraMoveVelocities[cameraRig] = velocity;
        }
    }
}
